using System.Reflection;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class FieldMapping
    {
        public ColumnSchema Column { get; set; }
        public FieldInfo TargetField { get; set; }
        public bool IsAutoMapped { get; set; }
    }
}
