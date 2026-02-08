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
        private readonly string _filePath;
        private readonly GenerationSettings _settings;
        private TableSchema[] _cachedSchemas;

        public SQLiteValidator(string filePath, GenerationSettings settings)
        {
            _filePath = filePath;
            _settings = settings;
        }

        public SourceValidationResult ValidateQuick()
        {
            var result = new SourceValidationResult();

            if (!File.Exists(_filePath))
            {
                return CreateResult(SourceStatus.InvalidSource, $"File not found: {_filePath}");
            }

            var ext = Path.GetExtension(_filePath).ToLowerInvariant();
            var acceptedExtensions = new[] { ".db", ".sqlite", ".sqlite3", ".db3" };
            if (!acceptedExtensions.Contains(ext))
            {
                var warning = new ValidationWarning
                {
                    level = WarningLevel.Warning,
                    message = $"Unexpected file extension '{ext}'. Attempting to read as SQLite anyway."
                };
                result.Warnings.Add(warning);
            }

            try
            {
                if (_filePath != null)
                {
                    using var stream = File.OpenRead(_filePath);
                    var header = new byte[16];
                    var bytesRead = stream.Read(header, 0, 16);

                    if (bytesRead < 16)
                    {
                        return CreateResult(SourceStatus.InvalidFormat,
                            "File is too small to be a valid SQLite database.");
                    }

                    var headerString = System.Text.Encoding.ASCII.GetString(header);
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

            result.Status = SourceStatus.Valid;
            result.IsValid = true;
            return result;
        }

        public void ValidateFull(Action<SourceValidationResult> onComplete)
        {
            SQLiteConnection connection = null;
            
            try
            {
                // Try opening database
                connection = new SQLiteConnection(_filePath, SQLiteOpenFlags.ReadOnly);

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
                var totalRows = 0;
                var totalColumns = 0;
                var tableNames = new List<string>();
                var allEmpty = true;

                foreach (var table in tables)
                {
                    tableNames.Add(table.Name);

                    var countCmd = connection.CreateCommand($"SELECT COUNT(*) FROM {table.Name}");
                    var rowCount = countCmd.ExecuteScalar<int>();
                    totalRows += rowCount;

                    if (rowCount == 0)
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            level = WarningLevel.Info,
                            table = table.Name,
                            message = $"Table '{table.Name}' has 0 rows and will produce no assets."
                        });
                    }
                    else
                    {
                        allEmpty = false;
                    }

                    var columns = connection.Query<ColumnInfo>($"PRAGMA table_info({table.Name})");
                    if (columns.Count == 0)
                    {
                        onComplete(CreateResult(SourceStatus.InvalidSchema,
                            $"Cannot read schema for table '{table.Name}'. The table may be corrupted."));
                        return;
                    }
                    totalColumns += columns.Count;

                    // Validate foreign keys
                    var foreignKeys = connection.Query<ForeignKeyInfo>($"PRAGMA foreign_key_list({table.Name})");
                    foreach (var fk in from fk in foreignKeys let targetExists = tables.Any(t => t.Name == fk.Table) where !targetExists select fk)
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            level = WarningLevel.Warning,
                            table = table.Name,
                            column = fk.From,
                            message = $"Table '{table.Name}' column '{fk.From}' references table '{fk.Table}' " +
                                      $"which does not exist in this database. The column will be imported as a plain integer."
                        });
                    }
                }

                if (allEmpty)
                {
                    onComplete(CreateResult(SourceStatus.InvalidContent, "All tables in the database are empty."));
                    return;
                }

                var reader = new SQLiteReader();
                var readResult = reader.Read(_filePath, _settings);
                
                if (!readResult.Success)
                {
                    onComplete(CreateResult(SourceStatus.InvalidSchema, readResult.ErrorMessage));
                    return;
                }

                if (readResult.Warnings != null)
                {
                    result.Warnings.AddRange(readResult.Warnings);
                }

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
            if (_cachedSchemas != null)
            {
                return _cachedSchemas;
            }

            var reader = new SQLiteReader();
            var result = reader.Read(_filePath, _settings);

            if (!result.Success) return Array.Empty<TableSchema>();
            _cachedSchemas = result.Schemas;
            return _cachedSchemas;

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
            public string Name { get; set; }
            public string Sql { get; set; }
        }

        private class ColumnInfo
        {
            public int Cid { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public int Notnull { get; set; }
            public string DefaultValue { get; set; }
            public int Pk { get; set; }
        }

        private class ForeignKeyInfo
        {
            public int ID { get; set; }
            public int Seq { get; set; }
            public string Table { get; set; }
            public string From { get; set; }
            public string To { get; set; }
        }
    }
}
