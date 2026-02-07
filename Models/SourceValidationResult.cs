using System.Collections.Generic;

namespace DataToScriptableObject
{
    public enum SourceStatus
    {
        NotLoaded,
        Validating,
        Valid,
        InvalidSource,
        InvalidFormat,
        InvalidContent,
        InvalidSchema,
        NetworkError,
        AccessDenied,
        NotFound
    }

    public enum WarningLevel { Info, Warning, Error }

    [System.Serializable]
    public class ValidationWarning
    {
        public WarningLevel level;
        public string message;
        public string table;
        public int row;             // -1 if not row-specific
        public string column;

        public ValidationWarning()
        {
            row = -1;
        }
    }

    [System.Serializable]
    public class SourceValidationResult
    {
        public bool IsValid { get; }
        public SourceStatus Status { get; }
        public string ErrorMessage { get; }
        public List<ValidationWarning> Warnings { get; } = new List<ValidationWarning>();
        public int TableCount { get; }
        public int TotalRowCount { get; }
        public int TotalColumnCount { get; }
        public string[] TableNames { get; }
    }
}
