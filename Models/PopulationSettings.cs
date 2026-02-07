namespace DataToScriptableObject
{
    [System.Serializable]
    public class PopulationSettings
    {
        public string assetOutputPath;
        public string assetPrefix;
        public string assetNamingColumn;
        public bool overwriteExisting;
        public bool deleteOrphaned;
    }
}
