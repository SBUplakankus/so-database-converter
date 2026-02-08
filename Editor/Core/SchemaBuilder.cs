using System;
using System.Collections.Generic;
using System.Linq;

namespace DataToScriptableObject.Editor
{
    public static class SchemaBuilder
    {
        #region Fields

        private static readonly Dictionary<string, ResolvedType> TypeHintMap = new Dictionary<string, ResolvedType>(StringComparer.OrdinalIgnoreCase)
        {
            { "int", ResolvedType.Int },
            { "float", ResolvedType.Float },
            { "double", ResolvedType.Double },
            { "long", ResolvedType.Long },
            { "bool", ResolvedType.Bool },
            { "string", ResolvedType.String },
            { "enum", ResolvedType.Enum },
            { "vector2", ResolvedType.Vector2 },
            { "vector3", ResolvedType.Vector3 },
            { "color", ResolvedType.Color },
            { "sprite", ResolvedType.Sprite },
            { "prefab", ResolvedType.Prefab },
            { "asset", ResolvedType.Asset },
            { "int[]", ResolvedType.IntArray },
            { "float[]", ResolvedType.FloatArray },
            { "string[]", ResolvedType.StringArray },
            { "bool[]", ResolvedType.BoolArray }
        };

        #endregion
        
        #region Helper Methods

        private static bool TryParseDirective(string directive, string prefix, out string value)
        {
            if (directive.StartsWith(prefix))
            {
                value = directive.Substring(prefix.Length).Trim();
                return !string.IsNullOrEmpty(value);
            }
            value = null;
            return false;
        }
        
        private static bool TryParseAttributeFlag(string flag, string name, out string value)
        {
            var prefix = $"{name}(";
            var lowerFlag = flag.ToLowerInvariant();
    
            if (lowerFlag.StartsWith(prefix) && lowerFlag.EndsWith(")"))
            {
                value = flag.Substring(prefix.Length, flag.Length - prefix.Length - 1);
                return true;
            }
    
            value = null;
            return false;
        }
        
        private static List<Dictionary<string, string>> BuildRowDictionaries(RawTableData rawData, ColumnSchema[] columns)
        {
            var rows = new List<Dictionary<string, string>>();
    
            foreach (var dataRow in rawData.DataRows)
            {
                var rowDict = new Dictionary<string, string>();
        
                for (var i = 0; i < columns.Length; i++)
                {
                    // Use the FieldName from the column schema for dictionary keys
                    var key = columns[i].FieldName;
                    var value = i < dataRow.Length ? dataRow[i] : "";
                    rowDict[key] = value;
                }
        
                rows.Add(rowDict);
            }
    
            return rows;
        }
        
        private static string NormalizeHeader(string header, int columnIndex, Dictionary<string, int> headerMap, TableSchema schema)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                header = $"column_{columnIndex}";
                schema.Warnings.Add(new ValidationWarning
                {
                    level = WarningLevel.Warning,
                    message = $"Empty header at column {columnIndex}, using '{header}'"
                });
                return header;
            }

            if (!headerMap.ContainsKey(header))
                return header;

            var count = 2;
            string newHeader;
            do
            {
                newHeader = $"{header}_{count++}";
            }
            while (headerMap.ContainsKey(newHeader));

            schema.Warnings.Add(new ValidationWarning
            {
                level = WarningLevel.Warning,
                message = $"Duplicate header '{header}', renamed to '{newHeader}'"
            });

            return newHeader;
        }

        private static ColumnSchema CreateColumn(RawTableData rawData, TableSchema schema, GenerationSettings settings, int columnIndex, string normalizedHeader, string originalHeader)
        {
            var column = new ColumnSchema
            {
                OriginalHeader = normalizedHeader,
                FieldName = settings.SanitizeFieldNames 
                    ? NameSanitizer.SanitizeFieldName(normalizedHeader) 
                    : normalizedHeader,
                Attributes = new Dictionary<string, string>()
            };

            if (rawData.TypeHints != null && columnIndex < rawData.TypeHints.Length)
            {
                column.Type = ParseTypeHint(rawData.TypeHints[columnIndex], schema);
            }
            else
            {
                var columnValues = rawData.DataRows
                    .Select(row => columnIndex < row.Length ? row[columnIndex] : "")
                    .ToArray();
                column.Type = TypeInference.Infer(columnValues);
            }

            if (rawData.Flags != null && columnIndex < rawData.Flags.Length)
            {
                ParseFlags(rawData.Flags, columnIndex, column, schema);
            }

            return column;
        }
        
        #endregion
        
        #region Build Methods
        
        public static TableSchema Build(RawTableData rawData, GenerationSettings settings)
        {
            var schema = new TableSchema
            {
                Warnings = new List<ValidationWarning>()
            };

            ExtractDirectives(rawData, schema, settings);

            if (rawData.Headers == null || rawData.Headers.Length == 0)
            {
                schema.Columns = Array.Empty<ColumnSchema>();
                schema.Rows = new List<Dictionary<string, string>>();
                return schema;
            }

            schema.Columns = BuildColumns(rawData, schema, settings);
            schema.Rows = BuildRowDictionaries(rawData, schema.Columns);

            return schema;
        }

        private static ColumnSchema[] BuildColumns(RawTableData rawData, TableSchema schema, GenerationSettings settings)
        {
            var columns = new List<ColumnSchema>();
            var headerMap = new Dictionary<string, int>();

            for (var i = 0; i < rawData.Headers.Length; i++)
            {
                var originalHeader = rawData.Headers[i];
                var normalizedHeader = NormalizeHeader(originalHeader, i, headerMap, schema);
                headerMap[normalizedHeader] = i;

                var column = CreateColumn(rawData, schema, settings, i, normalizedHeader, originalHeader);
                columns.Add(column);
            }

            return columns.ToArray();
        }

        private static void ExtractDirectives(RawTableData rawData, TableSchema schema, GenerationSettings settings)
        {
            schema.ClassName = settings.ClassName;
            schema.DatabaseName = settings.DatabaseName;
            schema.NamespaceName = settings.NamespaceName;

            if (rawData.Directives != null)
            {
                foreach (var directive in rawData.Directives)
                {
                    if (TryParseDirective(directive, "#class:", out var className))
                        schema.ClassName = className;
                    else if (TryParseDirective(directive, "#database:", out var databaseName))
                        schema.DatabaseName = databaseName;
                    else if (TryParseDirective(directive, "#namespace:", out var namespaceName))
                        schema.NamespaceName = namespaceName;
                }
            }

            if (string.IsNullOrEmpty(schema.ClassName))
                schema.ClassName = "GeneratedEntry";
            if (string.IsNullOrEmpty(schema.DatabaseName))
                schema.DatabaseName = schema.ClassName + "Database";
        }

        private static ResolvedType ParseTypeHint(string typeHint, TableSchema schema)
        {
            if (string.IsNullOrWhiteSpace(typeHint))
                return ResolvedType.String;

            var hint = typeHint.Trim();
    
            if (TypeHintMap.TryGetValue(hint, out var type))
                return type;

            schema.Warnings.Add(new ValidationWarning
            {
                level = WarningLevel.Warning,
                message = $"Unrecognized type hint '{typeHint}', defaulting to String"
            });
            return ResolvedType.String;
        }

        private static void ParseFlags(string[] flagsRow, int columnIndex, ColumnSchema column, TableSchema schema)
        {
            var flagsCell = flagsRow[columnIndex];
            
            if (string.IsNullOrWhiteSpace(flagsCell))
                return;

            if (column.Type == ResolvedType.Enum)
            {
                // For enums, collect all non-empty values from this column onwards in the flags row
                var enumValues = flagsRow
                    .Skip(columnIndex)
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToArray();
                column.EnumValues = enumValues;
                return;
            }

            var flags = flagsCell.Split('|').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f));

            foreach (var flag in flags)
            {
                var lower = flag.ToLowerInvariant();

                switch (lower)
                {
                    case "key":
                        column.IsKey = true;
                        break;
                    case "name":
                        column.IsName = true;
                        break;
                    case "optional":
                        column.IsOptional = true;
                        break;
                    case "skip":
                        column.IsSkipped = true;
                        break;
                    case "list":
                        column.IsList = true;
                        break;
                    case "hide":
                        column.Attributes["hide"] = "";
                        break;
                    default:
                    {
                        if (TryParseAttributeFlag(flag, "header", out var headerValue))
                            column.Attributes["header"] = headerValue;
                        else if (TryParseAttributeFlag(flag, "tooltip", out var tooltipValue))
                            column.Attributes["tooltip"] = tooltipValue;
                        else if (TryParseAttributeFlag(flag, "range", out var rangeValue))
                        {
                            var parts = rangeValue.Split(';');
                            if (parts.Length == 2)
                                column.Attributes["range"] = $"{parts[0].Trim()},{parts[1].Trim()}";
                        }
                        else if (TryParseAttributeFlag(flag, "multiline", out var multilineValue))
                            column.Attributes["multiline"] = multilineValue;

                        break;
                    }
                }
            }
        }
        
        #endregion
    }
}
