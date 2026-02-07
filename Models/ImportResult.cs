using System.Collections.Generic;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class ImportResult
    {
        public int Created { get; set; }
        public int Updated { get; set;  }
        public int Skipped { get; set;  }
        public int Deleted { get; set;  }
        public List<string> Errors { get; set;  } = new List<string>();
        public List<string> Warnings { get; set;  } = new List<string>();
        public List<string> CreatedAssetPaths { get; set;  } = new List<string>();
        public string GeneratedScriptPath { get; set;  }
        public string DatabaseAssetPath { get; set;  }
    }
}
