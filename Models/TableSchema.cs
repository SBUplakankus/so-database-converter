using System.Collections.Generic;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class TableSchema
    {
        public string className;
        public string databaseName;
        public string namespaceName;
        public string sourceTableName;      // original filename or table name
        public ColumnSchema[] columns;
        public List<Dictionary<string, string>> rows; // each row is { originalHeader → rawValue }
        public List<ValidationWarning> warnings;
    }
}
