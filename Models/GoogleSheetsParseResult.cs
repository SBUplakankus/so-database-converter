namespace DataToScriptableObject
{
    [System.Serializable]
    public class GoogleSheetsParseResult
    {
        public bool IsValid { get; set; }
        public string SpreadsheetId { get; set; }
        public string Gid { get; set; }
        public string Error { get; set; }
    }
}
