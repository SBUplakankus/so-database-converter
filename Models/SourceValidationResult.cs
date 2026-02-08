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
        public bool IsValid { get; set; }
        public SourceStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
        public int TableCount { get; set; }
        public int TotalRowCount { get; set; }
        public int TotalColumnCount { get; set; }
        public string[] TableNames { get; set; }
    }
}
