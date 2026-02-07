namespace DataToScriptableObject
{
    [System.Serializable]
    public class RawTableData
    {
        public string[] Directives { get; set; }
        public string[] Headers { get; set; }
        public string[] TypeHints { get; set; }
        public string[] Flags { get; set; }
        public string[][] DataRows { get; set; }
    }
}
