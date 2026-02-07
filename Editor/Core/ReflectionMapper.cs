using DataToScriptableObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataToScriptableObject.Editor
{
    public static class ReflectionMapper
    {
        public static FieldMapping[] Map(TableSchema schema, Type targetType)
        {
            var publicFields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var privateFields = targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<UnityEngine.SerializeField>() != null);
            
            var allFields = publicFields.Concat(privateFields).ToList();
            var mappings = new List<FieldMapping>();
            var mappedFields = new HashSet<string>();

            foreach (var column in schema.Columns.Where(c => !c.IsSkipped))
            {
                FieldInfo matchedField = null;

                matchedField = allFields.FirstOrDefault(f => f.Name == column.FieldName);

                if (matchedField == null)
                {
                    matchedField = allFields.FirstOrDefault(f => 
                        string.Equals(f.Name, column.FieldName, StringComparison.OrdinalIgnoreCase));
                }

                if (matchedField == null)
                {
                    string normalizedColumn = NormalizeFieldName(column.FieldName);
                    matchedField = allFields.FirstOrDefault(f => 
                        NormalizeFieldName(f.Name) == normalizedColumn);
                }

                if (matchedField != null)
                {
                    mappedFields.Add(matchedField.Name);
                    mappings.Add(new FieldMapping
                    {
                        Column = column,
                        TargetField = matchedField,
                        IsAutoMapped = true
                    });
                }
                else
                {
                    mappings.Add(new FieldMapping
                    {
                        Column = column,
                        TargetField = null,
                        IsAutoMapped = false
                    });

                    schema.Warnings.Add(new ValidationWarning
                    {
                        level = WarningLevel.Warning,
                        message = $"Column '{column.OriginalHeader}' has no matching field on type '{targetType.Name}'"
                    });
                }
            }

            foreach (var field in allFields)
            {
                if (!mappedFields.Contains(field.Name))
                {
                    schema.Warnings.Add(new ValidationWarning
                    {
                        level = WarningLevel.Info,
                        message = $"Field '{field.Name}' on type '{targetType.Name}' has no matching column and will keep its default value"
                    });
                }
            }

            return mappings.ToArray();
        }

        private static string NormalizeFieldName(string name)
        {
            return name.Replace("_", "").ToLowerInvariant();
        }
    }
}
