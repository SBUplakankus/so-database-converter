using System;
using System.IO;

namespace DataToScriptableObject.Editor
{
    /// <summary>
    /// Validates CSV files and implements ISourceReader for the import pipeline.
    /// </summary>
    public class CSVValidator : ISourceReader
    {
        private readonly string _filePath;
        private readonly GenerationSettings _settings;
        private TableSchema[] _cachedSchemas;

        public CSVValidator(string filePath, GenerationSettings settings)
        {
            _filePath = filePath;
            _settings = settings;
        }

        public SourceValidationResult ValidateQuick()
        {
            var result = new SourceValidationResult();

            // Check file exists
            if (!File.Exists(_filePath))
            {
                return CreateResult(SourceStatus.InvalidSource, $"File not found: {_filePath}");
            }

            // Check extension
            var ext = Path.GetExtension(_filePath).ToLowerInvariant();
            if (ext != ".csv" && ext != ".txt")
            {
                var warning = new ValidationWarning
                {
                    level = WarningLevel.Warning,
                    message = $"Unexpected file extension '{ext}'. Expected .csv or .txt. Attempting to read as CSV anyway."
                };
                result.Warnings.Add(warning);
            }

            // Check file can be opened
            try
            {
                if (_filePath != null)
                {
                    using var stream = File.OpenRead(_filePath);
                    if (stream.Length == 0)
                    {
                        return CreateResult(SourceStatus.InvalidContent, "File is empty.");
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
            result.Status = SourceStatus.Valid;
            result.IsValid = true;
            return result;
        }

        public void ValidateFull(Action<SourceValidationResult> onComplete)
        {
            try
            {
                // Try parsing the CSV file
                var csvText = File.ReadAllText(_filePath);
                var rawData = CSVReader.Parse(
                    csvText,
                    ",",  // Default delimiter - could be made configurable
                    _settings.CommentPrefix,
                    _settings.HeaderRowCount
                );

                if (rawData == null)
                {
                    onComplete(CreateResult(SourceStatus.InvalidFormat, 
                        "Failed to parse CSV file. The file may be corrupted or have an invalid format."));
                    return;
                }

                if (rawData.Headers == null || rawData.Headers.Length == 0)
                {
                    onComplete(CreateResult(SourceStatus.InvalidContent, 
                        "CSV file has no headers. Ensure the file has at least a header row."));
                    return;
                }

                if (rawData.DataRows == null || rawData.DataRows.Length == 0)
                {
                    var result = new SourceValidationResult
                    {
                        Status = SourceStatus.Valid,
                        IsValid = true,
                        TableCount = 1,
                        TotalRowCount = 0,
                        TotalColumnCount = rawData.Headers.Length
                    };
                    result.Warnings.Add(new ValidationWarning
                    {
                        level = WarningLevel.Info,
                        message = "CSV file has no data rows. No assets will be generated."
                    });
                    onComplete(result);
                    return;
                }

                // Build schema to validate data
                TableSchema schema;
                try
                {
                    schema = SchemaBuilder.Build(rawData, _settings);
                }
                catch (Exception ex)
                {
                    onComplete(CreateResult(SourceStatus.InvalidSchema, 
                        $"Failed to build schema from CSV: {ex.Message}"));
                    return;
                }

                _cachedSchemas = new TableSchema[] { schema };

                var finalResult = new SourceValidationResult
                {
                    Status = SourceStatus.Valid,
                    IsValid = true,
                    TableCount = 1,
                    TotalRowCount = rawData.DataRows.Length,
                    TotalColumnCount = rawData.Headers.Length,
                    TableNames = new[] { schema.ClassName }
                };

                onComplete(finalResult);
            }
            catch (IOException ex)
            {
                onComplete(CreateResult(SourceStatus.InvalidSource, 
                    $"Cannot read file: {ex.Message}"));
            }
            catch (Exception ex)
            {
                onComplete(CreateResult(SourceStatus.InvalidFormat, 
                    $"Error validating CSV file: {ex.Message}"));
            }
        }

        public TableSchema[] ExtractSchemas()
        {
            if (_cachedSchemas != null)
            {
                return _cachedSchemas;
            }

            try
            {
                var csvText = File.ReadAllText(_filePath);
                var rawData = CSVReader.Parse(
                    csvText,
                    ",",
                    _settings.CommentPrefix,
                    _settings.HeaderRowCount
                );

                if (rawData == null)
                {
                    return new TableSchema[0];
                }

                var schema = SchemaBuilder.Build(rawData, _settings);
                _cachedSchemas = new TableSchema[] { schema };
                return _cachedSchemas;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to extract schemas from CSV file '{_filePath}': {ex.Message}");
                return Array.Empty<TableSchema>();
            }
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
    }
}
