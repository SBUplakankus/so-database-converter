using DataToScriptableObject;
using System.Collections.Generic;
using System.Linq;

namespace DataToScriptableObject.Editor
{
    public static class SchemaBuilder
    {
        public static TableSchema Build(RawTableData rawData, GenerationSettings settings)
        {
            var schema = new TableSchema
            {
                warnings = new List<ValidationWarning>()
            };

            ExtractDirectives(rawData, schema, settings);

            if (rawData.headers == null || rawData.headers.Length == 0)
            {
                schema.columns = new ColumnSchema[0];
                schema.rows = new List<Dictionary<string, string>>();
                return schema;
            }

            var columns = new List<ColumnSchema>();
            var headerMap = new Dictionary<string, int>();

            for (int i = 0; i < rawData.headers.Length; i++)
            {
                string header = rawData.headers[i];
                string originalHeader = header;

                if (string.IsNullOrWhiteSpace(header))
                {
                    header = $"column_{i}";
                    schema.warnings.Add(new ValidationWarning
                    {
                        level = WarningLevel.Warning,
                        message = $"Empty header at column {i}, using '{header}'"
                    });
                }

                if (headerMap.ContainsKey(header))
                {
                    int count = 2;
                    string newHeader = $"{header}_{count}";
                    while (headerMap.ContainsKey(newHeader))
                    {
                        count++;
                        newHeader = $"{header}_{count}";
                    }
                    schema.warnings.Add(new ValidationWarning
                    {
                        level = WarningLevel.Warning,
                        message = $"Duplicate header '{header}', renamed to '{newHeader}'"
                    });
                    header = newHeader;
                }

                headerMap[header] = i;

                var column = new ColumnSchema
                {
                    originalHeader = string.IsNullOrWhiteSpace(originalHeader) ? header : originalHeader,
                    fieldName = settings.sanitizeFieldNames ? NameSanitizer.SanitizeFieldName(header) : header,
                    attributes = new Dictionary<string, string>()
                };

                if (rawData.typeHints != null && i < rawData.typeHints.Length)
                {
                    column.resolvedType = ParseTypeHint(rawData.typeHints[i], schema);
                }
                else
                {
                    var columnValues = rawData.dataRows.Select(row => i < row.Length ? row[i] : "").ToArray();
                    column.resolvedType = TypeInference.Infer(columnValues);
                }

                if (rawData.flags != null && i < rawData.flags.Length)
                {
                    ParseFlags(rawData.flags[i], column, schema);
                }

                columns.Add(column);
            }

            schema.columns = columns.ToArray();

            schema.rows = new List<Dictionary<string, string>>();
            foreach (var dataRow in rawData.dataRows)
            {
                var rowDict = new Dictionary<string, string>();
                for (int i = 0; i < rawData.headers.Length; i++)
                {
                    string header = rawData.headers[i];
                    if (string.IsNullOrWhiteSpace(header))
                        header = $"column_{i}";
                    
                    string value = i < dataRow.Length ? dataRow[i] : "";
                    rowDict[header] = value;
                }
                schema.rows.Add(rowDict);
            }

            return schema;
        }

        private static void ExtractDirectives(RawTableData rawData, TableSchema schema, GenerationSettings settings)
        {
            schema.className = settings.className;
            schema.databaseName = settings.databaseName;
            schema.namespaceName = settings.namespaceName;

            if (rawData.directives != null)
            {
                foreach (var directive in rawData.directives)
                {
                    if (directive.StartsWith("#class:"))
                    {
                        string value = directive.Substring("#class:".Length).Trim();
                        if (!string.IsNullOrEmpty(value))
                            schema.className = value;
                    }
                    else if (directive.StartsWith("#database:"))
                    {
                        string value = directive.Substring("#database:".Length).Trim();
                        if (!string.IsNullOrEmpty(value))
                            schema.databaseName = value;
                    }
                    else if (directive.StartsWith("#namespace:"))
                    {
                        string value = directive.Substring("#namespace:".Length).Trim();
                        if (!string.IsNullOrEmpty(value))
                            schema.namespaceName = value;
                    }
                }
            }

            if (string.IsNullOrEmpty(schema.className))
                schema.className = "GeneratedEntry";
            if (string.IsNullOrEmpty(schema.databaseName))
                schema.databaseName = schema.className + "Database";
        }

        private static ResolvedType ParseTypeHint(string typeHint, TableSchema schema)
        {
            if (string.IsNullOrWhiteSpace(typeHint))
                return ResolvedType.String;

            string hint = typeHint.Trim().ToLowerInvariant();

            switch (hint)
            {
                case "int": return ResolvedType.Int;
                case "float": return ResolvedType.Float;
                case "double": return ResolvedType.Double;
                case "long": return ResolvedType.Long;
                case "bool": return ResolvedType.Bool;
                case "string": return ResolvedType.String;
                case "enum": return ResolvedType.Enum;
                case "vector2": return ResolvedType.Vector2;
                case "vector3": return ResolvedType.Vector3;
                case "color": return ResolvedType.Color;
                case "sprite": return ResolvedType.Sprite;
                case "prefab": return ResolvedType.Prefab;
                case "asset": return ResolvedType.Asset;
                case "int[]": return ResolvedType.IntArray;
                case "float[]": return ResolvedType.FloatArray;
                case "string[]": return ResolvedType.StringArray;
                case "bool[]": return ResolvedType.BoolArray;
                default:
                    schema.warnings.Add(new ValidationWarning
                    {
                        level = WarningLevel.Warning,
                        message = $"Unrecognized type hint '{typeHint}', defaulting to String"
                    });
                    return ResolvedType.String;
            }
        }

        private static void ParseFlags(string flagsCell, ColumnSchema column, TableSchema schema)
        {
            if (string.IsNullOrWhiteSpace(flagsCell))
                return;

            if (column.resolvedType == ResolvedType.Enum)
            {
                var enumValues = flagsCell.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)).ToArray();
                column.enumValues = enumValues;
                return;
            }

            var flags = flagsCell.Split('|').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f));

            foreach (var flag in flags)
            {
                string lower = flag.ToLowerInvariant();

                if (lower == "key")
                    column.isKey = true;
                else if (lower == "name")
                    column.isName = true;
                else if (lower == "optional")
                    column.isOptional = true;
                else if (lower == "skip")
                    column.isSkipped = true;
                else if (lower == "list")
                    column.isList = true;
                else if (lower == "hide")
                    column.attributes["hide"] = "";
                else if (lower.StartsWith("header(") && lower.EndsWith(")"))
                {
                    string value = flag.Substring(7, flag.Length - 8);
                    column.attributes["header"] = value;
                }
                else if (lower.StartsWith("tooltip(") && lower.EndsWith(")"))
                {
                    string value = flag.Substring(8, flag.Length - 9);
                    column.attributes["tooltip"] = value;
                }
                else if (lower.StartsWith("range(") && lower.EndsWith(")"))
                {
                    string value = flag.Substring(6, flag.Length - 7);
                    var parts = value.Split(';');
                    if (parts.Length == 2)
                    {
                        column.attributes["range"] = $"{parts[0].Trim()},{parts[1].Trim()}";
                    }
                }
            }
        }
    }
}
