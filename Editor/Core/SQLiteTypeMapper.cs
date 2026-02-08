namespace DataToScriptableObject.Editor
{
    /// <summary>
    /// Maps SQLite declared column type strings to ResolvedType enum.
    /// Uses CONTAINS logic to handle parameterized types like VARCHAR(255) or NUMERIC(10,2).
    /// </summary>
    public static class SQLiteTypeMapper
    {
        public static ResolvedType Map(string sqliteType, out string warning)
        {
            warning = null;

            if (string.IsNullOrEmpty(sqliteType))
            {
                return ResolvedType.String;
            }

            string normalized = sqliteType.Trim().ToUpperInvariant();

            // Priority order matters — check specific types before generic ones
            if (normalized.Contains("BIGINT"))
                return ResolvedType.Long;

            if (normalized.Contains("INT") && !normalized.Contains("POINT"))
                return ResolvedType.Int;

            if (normalized.Contains("DOUBLE"))
                return ResolvedType.Double;

            if (normalized.Contains("FLOAT"))
                return ResolvedType.Float;

            if (normalized.Contains("REAL"))
                return ResolvedType.Float;

            if (normalized.Contains("NUMERIC"))
                return ResolvedType.Float;

            if (normalized.Contains("BOOL"))
                return ResolvedType.Bool;

            if (normalized.Contains("TEXT"))
                return ResolvedType.String;

            if (normalized.Contains("VARCHAR"))
                return ResolvedType.String;

            if (normalized.Contains("CHAR") && !normalized.Contains("VARCHAR"))
                return ResolvedType.String;

            if (normalized.Contains("CLOB"))
                return ResolvedType.String;

            if (normalized.Contains("NCHAR"))
                return ResolvedType.String;

            if (normalized.Contains("NVARCHAR"))
                return ResolvedType.String;

            if (normalized.Contains("BLOB"))
            {
                warning = "BLOB columns are imported as string. Binary data is not supported.";
                return ResolvedType.String;
            }

            // No match — default to string with warning
            warning = $"Unrecognized SQLite type '{sqliteType}', defaulting to string.";
            return ResolvedType.String;
        }
    }
}
