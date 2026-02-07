using System.Collections.Generic;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class ColumnSchema
    {
        public string FieldName { get; set; }
        public string OriginalHeader { get; set; }
        public ResolvedType Type { get; set; }
        public bool IsKey { get; set; }
        public bool IsName { get; set; }
        public bool IsOptional { get; set; }
        public bool IsSkipped { get; set; }
        public bool IsList { get; set; }
        public string[] EnumValues { get; set; }
        public string DefaultValue { get; }
        public Dictionary<string, string> Attributes { get; set; }

        // Reserved for v2 SQLite support — leave these fields in place
        public bool IsForeignKey { get; }
        public string ForeignKeyTable { get; }
        public string ForeignKeyColumn { get; }
        public string ForeignKeySoType { get; }
    }
}
