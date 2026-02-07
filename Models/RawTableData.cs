namespace DataToScriptableObject
{
    [System.Serializable]
    public class RawTableData
    {
        public string[] directives;         // #class:, #database:, #namespace: lines
        public string[] headers;            // Row 1: column names
        public string[] typeHints;          // Row 2: type declarations (null if headerRowCount < 2)
        public string[] flags;              // Row 3: flags/constraints (null if headerRowCount < 3)
        public string[][] dataRows;         // All data rows after headers
    }
}
