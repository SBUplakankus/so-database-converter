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
        public string DefaultValue { get; set; }
        public Dictionary<string, string> Attributes { get; set; }

        // V2 SQLite support — foreign key fields
        public bool IsForeignKey { get; set; }
        public string ForeignKeyTable { get; set; }
        public string ForeignKeyColumn { get; set; }
        public string ForeignKeySoType { get; set; }
    }
}
