using System.IO;
using System.Linq;
using DataToScriptableObject.Editor.Builders;
using UnityEditor;

namespace DataToScriptableObject.Editor
{
    public class CodeGenerationResult
    {
        public string EntryClassCode;
        public string DatabaseClassCode;
        public string EntryClassPath;
        public string DatabaseClassPath;
    }
    
    public static class CodeGenerator
    {
        #region Generation Methods
        
        public static CodeGenerationResult Generate(TableSchema schema, GenerationSettings settings)
        {
            var result = new CodeGenerationResult();

            Directory.CreateDirectory(settings.ScriptOutputPath);

            var sanitizedClassName =  NameSanitizer.SanitizeClassName(schema.ClassName);
            var sanitizedDatabaseName = NameSanitizer.SanitizeClassName(schema.DatabaseName);
            
            result.EntryClassPath = Path.Combine(settings.ScriptOutputPath, $"{sanitizedClassName}.cs");
            result.DatabaseClassPath = Path.Combine(settings.ScriptOutputPath, $"{sanitizedDatabaseName}.cs");

            if (!settings.SerializableClassMode)
            {
                result.EntryClassCode = GenerateScriptableObject(schema, settings);
                result.DatabaseClassCode = GenerateScriptableObject(schema, settings);
                
                File.WriteAllText(result.EntryClassPath, result.EntryClassCode);
                File.WriteAllText(result.DatabaseClassPath, result.DatabaseClassCode);
            }
            else
            {
                result.DatabaseClassCode = GenerateCombinedClass(schema, settings);
                File.WriteAllText(result.DatabaseClassCode, result.DatabaseClassCode);
                result.EntryClassPath  = result.DatabaseClassPath;
            }
            
            AssetDatabase.Refresh();
            return result;
        }
        
        public static string GenerateScriptableObject(TableSchema schema, GenerationSettings settings)
        {
            var sanitizedClassName = NameSanitizer.SanitizeClassName(schema.ClassName);

            var builder = new CodeFileBuilder(schema.NamespaceName)
                .WithHeader(settings)
                .BeginNamespace();
            
            // Generate enum declarations for any enum columns
            foreach (var column in schema.Columns.Where(c => !c.IsSkipped && c.Type == ResolvedType.Enum))
            {
                var enumName = ToPascalCase(column.FieldName);
                if (column.EnumValues != null && column.EnumValues.Length > 0)
                {
                    builder.AppendLine($"public enum {enumName}");
                    builder.AppendLine("{");
                    for (int i = 0; i < column.EnumValues.Length; i++)
                    {
                        var enumValue = NameSanitizer.SanitizeClassName(column.EnumValues[i]);
                        var suffix = i < column.EnumValues.Length - 1 ? "," : "";
                        builder.AppendLine($"    {enumValue}{suffix}");
                    }
                    builder.AppendLine("}");
                    builder.AppendLine();
                }
            }
            
            if(settings.SerializableClassMode)
                builder.BeginSerializableClass(sanitizedClassName);
            else 
                builder.BeginScriptableObjectClass(sanitizedClassName, settings);
            
            foreach(var column in schema.Columns.Where(c => !c.IsSkipped))
                AppendFieldDeclaration(builder, column, settings);

            return builder
                .EndClass()
                .EndNamespace()
                .Build();
        }

        public static string GenerateDatabaseClass(TableSchema schema, GenerationSettings settings)
        {
            var sanitizedClassName = NameSanitizer.SanitizeClassName(schema.ClassName);
            var sanitizedDatabaseName = NameSanitizer.SanitizeClassName(schema.DatabaseName);

            return new CodeFileBuilder(schema.NamespaceName)
                .WithHeader(settings)
                .BeginNamespace()
                .BeginScriptableObjectClass(sanitizedDatabaseName, settings)
                .AppendLine($"public List<{sanitizedClassName}> entries;")
                .EndClass()
                .EndNamespace()
                .Build();
        }

        private static string GenerateCombinedClass(TableSchema schema, GenerationSettings settings)
        {
            var sanitizedClassName =  NameSanitizer.SanitizeClassName(schema.ClassName);
            var sanitizedDatabaseName =  NameSanitizer.SanitizeClassName(schema.DatabaseName);
            
            var builder = new CodeFileBuilder(schema.NamespaceName)
                .WithHeader(settings)
                .BeginNamespace()
                .BeginSerializableClass(sanitizedClassName);

            foreach (var column in schema.Columns.Where(c => !c.IsSkipped))
            {
                AppendFieldDeclaration(builder, column, settings);
            }

            return builder
                .EndClass()
                .AppendLine()
                .BeginScriptableObjectClass(sanitizedDatabaseName, settings)
                .AppendLine($"public List<{sanitizedClassName}> entries;")
                .EndClass()
                .EndNamespace()
                .Build();
        }
        
        #endregion

        #region Helper Methods

        private static string ToPascalCase(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return fieldName;
            return char.ToUpperInvariant(fieldName[0]) + (fieldName.Length > 1 ? fieldName[1..] : "");
        }
        
        private static void AppendFieldDeclaration(CodeFileBuilder builder, ColumnSchema column, GenerationSettings settings)
        {
            if (settings.GenerateTooltips && column.Attributes.TryGetValue("tooltip", out var columnAttribute))
                builder.AppendLine($"[Tooltip(\"{columnAttribute}\")]");

            if (column.Attributes.ContainsKey("range"))
            {
                var rangeParts = column.Attributes["range"].Split(',');
                if (rangeParts.Length == 2)
                    builder.AppendLine($"[Range({rangeParts[0]}f, {rangeParts[1]}f)]");
            }

            if (column.Attributes.ContainsKey("hide"))
                builder.AppendLine("[HideInInspector]");

            if (column.Attributes.TryGetValue("header", out var attribute))
                builder.AppendLine($"[Header(\"{attribute}\")]");

            var typeString = GetTypeString(column, settings);

            if (settings.UseSerializeField)
            {
                builder.AppendLine($"[SerializeField] private {typeString} {column.FieldName};");

                if (!settings.GeneratePropertyAccessors) return;
                var pascalName = ToPascalCase(column.FieldName);
                builder.AppendLine($"public {typeString} {pascalName} => {column.FieldName};");
            }
            else
            {
                builder.AppendLine($"public {typeString} {column.FieldName};");
            }
        }

        private static string GetTypeString(ColumnSchema column, GenerationSettings settings)
        {
            var useList = column.IsList || settings.UseListInsteadOfArray;

            return column.Type switch
            {
                ResolvedType.Int => "int",
                ResolvedType.Float => "float",
                ResolvedType.Double => "double",
                ResolvedType.Long => "long",
                ResolvedType.Bool => "bool",
                ResolvedType.String => "string",
                ResolvedType.Vector2 => "Vector2",
                ResolvedType.Vector3 => "Vector3",
                ResolvedType.Color => "Color",
                ResolvedType.Sprite => "Sprite",
                ResolvedType.Prefab => "GameObject",
                ResolvedType.Asset => "ScriptableObject",
                ResolvedType.Enum => ToPascalCase(column.FieldName),
                ResolvedType.IntArray => useList ? "List<int>" : "int[]",
                ResolvedType.FloatArray => useList ? "List<float>" : "float[]",
                ResolvedType.StringArray => useList ? "List<string>" : "string[]",
                ResolvedType.BoolArray => useList ? "List<bool>" : "bool[]",
                _ => "string"
            };
        }

        #endregion
        
        
    }
}
