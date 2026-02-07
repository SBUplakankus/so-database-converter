using System.Collections.Generic;

namespace DataToScriptableObject
{
    public class ImportResult
    {
        public int created;
        public int updated;
        public int skipped;
        public int deleted;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public List<string> createdAssetPaths = new List<string>();
        public string generatedScriptPath;
        public string databaseAssetPath;
    }
}
