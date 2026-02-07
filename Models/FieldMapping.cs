using System.Reflection;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class FieldMapping
    {
        public ColumnSchema column;
        public FieldInfo targetField;       // null if column is skipped or unmapped
        public bool isAutoMapped;           // true if matched by name
    }
}
