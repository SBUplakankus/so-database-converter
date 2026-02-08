using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;
using DataToScriptableObject.Editor;
using DataToScriptableObject.Editor.Enums;

namespace DataToScriptableObject.Editor
{
    public class SQLiteReadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TableSchema[] Schemas { get; set; }
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
    }

    /// <summary>
    /// Reads SQLite databases and converts them to TableSchema objects for the import pipeline.
    /// </summary>
    public class SQLiteReader
    {
        public SQLiteReadResult Read(string dbPath, GenerationSettings settings)
        {
            var result = new SQLiteReadResult();
            SQLiteConnection connection = null;

            try
            {
                // Open connection
                connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);

                // Query all user tables
                var tables = connection.Query<TableInfo>(
                    "SELECT name, sql FROM sqlite_master " +
                    "WHERE type='table' " +
                    "AND name NOT LIKE 'sqlite_%' " +
                    "AND name NOT LIKE '\\_%' ESCAPE '\\' " +
                    "AND name != 'android_metadata' " +
                    "ORDER BY name");

                var schemas = new List<TableSchema>();

                foreach (var table in tables)
                {
                    var schema = ProcessTable(connection, table.name, table.sql, settings, result.Warnings);
                    schemas.Add(schema);
                }

                // Run dependency resolver
                var resolveResult = DependencyResolver.Resolve(schemas.ToArray());
                
                if (!resolveResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = resolveResult.ErrorMessage;
                    return result;
                }

                // Merge warnings from resolver
                if (resolveResult.Warnings != null)
                {
                    result.Warnings.AddRange(resolveResult.Warnings);
                }

                result.Success = true;
                result.Schemas = resolveResult.OrderedSchemas;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Cannot open database: {ex.Message}";
                return result;
            }
            finally
            {
                connection?.Close();
            }
        }

        private TableSchema ProcessTable(
    SQLiteConnection connection,
    string tableName,
    string ddl,
    GenerationSettings settings,
    List<ValidationWarning> warnings)
{
    // Read column info
    var columnInfos = connection.Query<ColumnInfo>($"PRAGMA table_info({tableName})");
    
    // Read foreign keys
    var foreignKeys = connection.Query<ForeignKeyInfo>($"PRAGMA foreign_key_list({tableName})");
    var fkMap = foreignKeys.ToDictionary(fk => fk.from);

    // Parse DDL constraints
    var constraints = SQLiteSchemaParser.ParseDDL(ddl);

    // Build column schemas
    var columns = new List<ColumnSchema>();

    foreach (var colInfo in columnInfos)
    {
        var column = new ColumnSchema
        {
            OriginalHeader = colInfo.name,
            FieldName = NameSanitizer.SanitizeFieldName(colInfo.name),
            IsOptional = colInfo.notnull == 0,
            DefaultValue = colInfo.dflt_value,
            IsKey = colInfo.pk == 1,
            Attributes = new Dictionary<string, string>()
        };

        // Map SQLite type to ResolvedType
        column.Type = SQLiteTypeMapper.Map(colInfo.type, out string typeWarning);
        if (typeWarning != null)
        {
            warnings.Add(new ValidationWarning
            {
                level = WarningLevel.Warning,
                table = tableName,
                column = colInfo.name,
                message = typeWarning
            });
        }

        // Check for foreign key
        if (fkMap.ContainsKey(colInfo.name))
        {
            var fk = fkMap[colInfo.name];
            column.IsForeignKey = true;
            column.ForeignKeyTable = fk.table;
            column.ForeignKeyColumn = fk.to;
            column.ForeignKeySoType = NameSanitizer.ConvertToCase(fk.table, NamingCase.PascalCase) + "Entry";
            column.Type = ResolvedType.Asset;
        }

        // Apply DDL constraints
        if (constraints.ContainsKey(colInfo.name))
        {
            var constraint = constraints[colInfo.name];

            if (constraint.RangeMin.HasValue && constraint.RangeMax.HasValue)
            {
                column.Attributes["range"] = $"{constraint.RangeMin.Value},{constraint.RangeMax.Value}";
            }

            if (constraint.CheckInValues != null && constraint.CheckInValues.Length > 0)
            {
                // Convert to enum if currently string or int
                if (column.Type == ResolvedType.String || column.Type == ResolvedType.Int)
                {
                    column.Type = ResolvedType.Enum;
                    column.EnumValues = constraint.CheckInValues;
                }
            }

            if (constraint.IsUnique)
            {
                warnings.Add(new ValidationWarning
                {
                    level = WarningLevel.Info,
                    table = tableName,
                    column = colInfo.name,
                    message = $"Column '{colInfo.name}' is UNIQUE — duplicate values will cause asset naming collisions."
                });
            }
        }

        columns.Add(column);
    }

    // Read all data rows using SQLite-net's Query method
    var rows = new List<Dictionary<string, string>>();
    var columnNames = columns.Select(c => c.OriginalHeader).ToList();
    
    // Query all rows - SQLite-net will return List<object[]>
    var query = $"SELECT * FROM {tableName}";
    
    // Use the lower-level Execute method to get raw row data
    var stmt = SQLite3.Prepare2(connection.Handle, query);
    while (SQLite3.Step(stmt) == SQLite3.Result.Row)
    {
        var rowDict = new Dictionary<string, string>();
            
        for (int i = 0; i < columns.Count; i++)
        {
            var columnName = columns[i].OriginalHeader;
            var column = columns[i];
                
            // Get the value based on column type
            object value = null;
            var colType = SQLite3.ColumnType(stmt, i);
                
            switch (colType)
            {
                case SQLite3.ColType.Integer:
                    value = SQLite3.ColumnInt64(stmt, i);
                    break;
                case SQLite3.ColType.Float:
                    value = SQLite3.ColumnDouble(stmt, i);
                    break;
                case SQLite3.ColType.Text:
                    value = SQLite3.ColumnString(stmt, i);
                    break;
                case SQLite3.ColType.Blob:
                    value = SQLite3.ColumnBlob(stmt, i);
                    break;
                case SQLite3.ColType.Null:
                    value = null;
                    break;
            }
                
            rowDict[columnName] = ConvertValue(value, column);
        }
            
        rows.Add(rowDict);
    }

    // Build schema
    var schema = new TableSchema
    {
        ClassName = NameSanitizer.ConvertToCase(tableName, NamingCase.PascalCase) + "Entry",
        DatabaseName = NameSanitizer.ConvertToCase(tableName, NamingCase.PascalCase) + "Database",
        NamespaceName = settings?.NamespaceName ?? "",
        SourceTableName = tableName,
        Columns = columns.ToArray(),
        Rows = rows,
        Warnings = new List<ValidationWarning>()
    };

    return schema;
}

        private string ConvertValue(object value, ColumnSchema column)
        {
            if (value == null)
            {
                return "";
            }

            // For boolean columns, convert 0/1 to false/true
            if (column != null && column.Type == ResolvedType.Bool)
            {
                if (value is long longVal)
                {
                    return longVal == 0 ? "false" : "true";
                }
                if (value is int intVal)
                {
                    return intVal == 0 ? "false" : "true";
                }
            }

            return value.ToString();
        }

        // Helper classes for SQLite query results
        private class TableInfo
        {
            public string name { get; set; }
            public string sql { get; set; }
        }

        private class ColumnInfo
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int notnull { get; set; }
            public string dflt_value { get; set; }
            public int pk { get; set; }
        }

        private class ForeignKeyInfo
        {
            public int id { get; set; }
            public int seq { get; set; }
            public string table { get; set; }
            public string from { get; set; }
            public string to { get; set; }
        }
    }
}
