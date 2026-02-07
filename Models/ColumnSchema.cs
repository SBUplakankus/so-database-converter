using System.Collections.Generic;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class ColumnSchema
    {
        public string fieldName;            // sanitized C# field name
        public string originalHeader;       // raw CSV header name
        public ResolvedType resolvedType;
        public bool isKey;
        public bool isName;
        public bool isOptional;
        public bool isSkipped;
        public bool isList;                 // List<T> instead of T[]
        public string[] enumValues;         // populated if resolvedType == Enum
        public string defaultValue;
        public Dictionary<string, string> attributes; // "range" => "0,100", "tooltip" => "Base damage"

        // Reserved for v2 SQLite support — leave these fields in place
        public bool isForeignKey;
        public string foreignKeyTable;
        public string foreignKeyColumn;
        public string foreignKeySOType;
    }
}
