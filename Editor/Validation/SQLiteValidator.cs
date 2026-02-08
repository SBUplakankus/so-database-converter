using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SQLite;

namespace DataToScriptableObject.Editor
{
    /// <summary>
    /// Validates SQLite database files and implements ISourceReader for the import pipeline.
    /// </summary>
    public class SQLiteValidator : ISourceReader
    {
        private readonly string filePath;
        private readonly GenerationSettings settings;
        private TableSchema[] cachedSchemas;

        public SQLiteValidator(string filePath, GenerationSettings settings)
        {
            this.filePath = filePath;
            this.settings = settings;
        }

        public SourceValidationResult ValidateQuick()
        {
            var result = new SourceValidationResult();

            // Check file exists
            if (!File.Exists(filePath))
            {
                return CreateResult(SourceStatus.InvalidSource, $"File not found: {filePath}");
            }

            // Check extension (informational only)
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            var acceptedExtensions = new[] { ".db", ".sqlite", ".sqlite3", ".db3" };
            if (!acceptedExtensions.Contains(ext))
            {
                var warning = new ValidationWarning
                {
                    level = WarningLevel.Info,
                    message = $"Unexpected file extension '{ext}'. Attempting to read as SQLite anyway."
                };
                result.Warnings.Add(warning);
            }

            // Read first 16 bytes to check SQLite magic header
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] header = new byte[16];
                    int bytesRead = stream.Read(header, 0, 16);
                    
                    if (bytesRead < 16)
                    {
                        return CreateResult(SourceStatus.InvalidFormat, 
                            "File is too small to be a valid SQLite database.");
                    }

                    string headerString = System.Text.Encoding.ASCII.GetString(header);
                    if (!headerString.StartsWith("SQLite format 3\0"))
                    {
                        return CreateResult(SourceStatus.InvalidFormat,
                            "File is not a valid SQLite database. Expected 'SQLite format 3' header. " +
                            "The file may be corrupted, encrypted, or a different database format " +
                            "(such as Microsoft Access). This tool requires standard SQLite 3.x files.");
                    }
                }
            }
            catch (IOException ex)
            {
                return CreateResult(SourceStatus.InvalidSource, 
                    $"Cannot read file. It may be locked by another application. {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateResult(SourceStatus.AccessDenied, 
                    $"Permission denied when reading file. {ex.Message}");
            }

            // Basic checks passed
            return CreateResult(SourceStatus.Valid, null);
        }

        public void ValidateFull(Action<SourceValidationResult> onComplete)
        {
            SQLiteConnection connection = null;
            
            try
            {
                // Try opening database
                connection = new SQLiteConnection(filePath, SQLiteOpenFlags.ReadOnly);

                // Query user tables
                var tables = connection.Query<TableInfo>(
                    "SELECT name, sql FROM sqlite_master " +
                    "WHERE type='table' " +
                    "AND name NOT LIKE 'sqlite_%' " +
                    "AND name NOT LIKE '\\_%' ESCAPE '\\' " +
                    "AND name != 'android_metadata' " +
                    "ORDER BY name");

                if (tables.Count == 0)
                {
                    onComplete(CreateResult(SourceStatus.InvalidContent,
                        "Database contains no user tables. Only SQLite system tables were found. " +
                        "Ensure the database has at least one table with data."));
                    return;
                }

                var result = new SourceValidationResult();
                int totalRows = 0;
                int totalColumns = 0;
                var tableNames = new List<string>();
                bool allEmpty = true;

                foreach (var table in tables)
                {
                    tableNames.Add(table.name);

                    // Get row count
                    var countCmd = connection.CreateCommand($"SELECT COUNT(*) FROM {table.name}");
                    int rowCount = countCmd.ExecuteScalar<int>();
                    totalRows += rowCount;

                    if (rowCount == 0)
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            level = WarningLevel.Info,
                            table = table.name,
                            message = $"Table '{table.name}' has 0 rows and will produce no assets."
                        });
                    }
                    else
                    {
                        allEmpty = false;
                    }

                    // Get column count
                    var columns = connection.Query<ColumnInfo>($"PRAGMA table_info({table.name})");
                    if (columns.Count == 0)
                    {
                        onComplete(CreateResult(SourceStatus.InvalidSchema,
                            $"Cannot read schema for table '{table.name}'. The table may be corrupted."));
                        return;
                    }
                    totalColumns += columns.Count;

                    // Validate foreign keys
                    var foreignKeys = connection.Query<ForeignKeyInfo>($"PRAGMA foreign_key_list({table.name})");
                    foreach (var fk in foreignKeys)
                    {
                        // Check if target table exists
                        bool targetExists = tables.Any(t => t.name == fk.table);
                        if (!targetExists)
                        {
                            result.Warnings.Add(new ValidationWarning
                            {
                                level = WarningLevel.Warning,
                                table = table.name,
                                column = fk.from,
                                message = $"Table '{table.name}' column '{fk.from}' references table '{fk.table}' " +
                                         $"which does not exist in this database. The column will be imported as a plain integer."
                            });
                        }
                    }
                }

                if (allEmpty)
                {
                    onComplete(CreateResult(SourceStatus.InvalidContent, "All tables in the database are empty."));
                    return;
                }

                // Try to build schemas and check for dependency cycles
                var reader = new SQLiteReader();
                var readResult = reader.Read(filePath, settings);
                
                if (!readResult.Success)
                {
                    onComplete(CreateResult(SourceStatus.InvalidSchema, readResult.ErrorMessage));
                    return;
                }

                // Merge warnings
                if (readResult.Warnings != null)
                {
                    result.Warnings.AddRange(readResult.Warnings);
                }

                // Success
                var finalResult = new SourceValidationResult
                {
                    Status = SourceStatus.Valid,
                    IsValid = true,
                    TableCount = tables.Count,
                    TotalRowCount = totalRows,
                    TotalColumnCount = totalColumns,
                    TableNames = tableNames.ToArray(),
                    Warnings = result.Warnings
                };

                onComplete(finalResult);
            }
            catch (SQLiteException ex)
            {
                onComplete(CreateResult(SourceStatus.InvalidFormat,
                    $"Cannot open database: {ex.Message}. The file may be corrupted, encrypted with a tool like SQLCipher, " +
                    $"or created with an incompatible SQLite version."));
            }
            catch (Exception ex)
            {
                onComplete(CreateResult(SourceStatus.InvalidFormat,
                    $"Cannot open database: {ex.Message}"));
            }
            finally
            {
                connection?.Close();
            }
        }

        public TableSchema[] ExtractSchemas()
        {
            if (cachedSchemas != null)
            {
                return cachedSchemas;
            }

            var reader = new SQLiteReader();
            var result = reader.Read(filePath, settings);
            
            if (result.Success)
            {
                cachedSchemas = result.Schemas;
                return cachedSchemas;
            }

            return new TableSchema[0];
        }

        private SourceValidationResult CreateResult(SourceStatus status, string errorMessage)
        {
            return new SourceValidationResult
            {
                Status = status,
                IsValid = status == SourceStatus.Valid,
                ErrorMessage = errorMessage
            };
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
