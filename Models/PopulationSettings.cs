namespace DataToScriptableObject
{
    [System.Serializable]
    public class PopulationSettings
    {
        public string AssetOutputPath { get; set;  }
        public string AssetPrefix { get; set;  }
        public string AssetNamingColumn { get; set;  }
        public bool OverwriteExisting { get; set;  }
        public bool DeleteOrphaned { get; set;  }
    }
}
