using System.Collections.Generic;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class TableSchema
    {
        public string ClassName { get; set; }
        public string DatabaseName { get; set; }
        public string NamespaceName { get; set; }
        public string SourceTableName { get; set; }
        public ColumnSchema[] Columns { get; set; }
        public List<Dictionary<string, string>> Rows { get; set; }
        public List<ValidationWarning> Warnings { get; set; }
    }
}
