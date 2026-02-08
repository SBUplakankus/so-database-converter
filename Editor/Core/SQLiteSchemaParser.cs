using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DataToScriptableObject.Editor
{
    public class ColumnConstraints
    {
        public float? RangeMin { get; set; }
        public float? RangeMax { get; set; }
        public string[] CheckInValues { get; set; }
        public bool IsUnique { get; set; }
    }

    /// <summary>
    /// Parses raw CREATE TABLE SQL statements to extract constraints that PRAGMA table_info doesn't provide.
    /// Uses regex-based best-effort parsing - never throws on unparseable input.
    /// </summary>
    public static class SQLiteSchemaParser
    {
        /// <summary>
        /// Parse DDL to extract column constraints.
        /// Returns a dictionary mapping column name → extracted constraints.
        /// </summary>
        public static Dictionary<string, ColumnConstraints> ParseDDL(string createTableSQL)
        {
            var result = new Dictionary<string, ColumnConstraints>();

            if (string.IsNullOrEmpty(createTableSQL))
            {
                return result;
            }

            try
            {
                // Parse range CHECK constraints
                // Pattern: CHECK(col >= min AND col <= max) or CHECK(col <= max AND col >= min)
                var rangePattern1 = @"CHECK\s*\(\s*[""']?(\w+)[""']?\s*>=\s*(-?\d+\.?\d*)\s+AND\s+[""']?(\w+)[""']?\s*<=\s*(-?\d+\.?\d*)\s*\)";
                var rangePattern2 = @"CHECK\s*\(\s*[""']?(\w+)[""']?\s*<=\s*(-?\d+\.?\d*)\s+AND\s+[""']?(\w+)[""']?\s*>=\s*(-?\d+\.?\d*)\s*\)";
                
                foreach (Match match in Regex.Matches(createTableSQL, rangePattern1, RegexOptions.IgnoreCase))
                {
                    var col1 = match.Groups[1].Value;
                    var col2 = match.Groups[3].Value;
                    
                    if (col1 == col2)
                    {
                        var min = float.Parse(match.Groups[2].Value);
                        var max = float.Parse(match.Groups[4].Value);
                        
                        if (!result.ContainsKey(col1))
                            result[col1] = new ColumnConstraints();
                        
                        result[col1].RangeMin = min;
                        result[col1].RangeMax = max;
                    }
                }
                
                foreach (Match match in Regex.Matches(createTableSQL, rangePattern2, RegexOptions.IgnoreCase))
                {
                    var col1 = match.Groups[1].Value;
                    var col2 = match.Groups[3].Value;
                    
                    if (col1 == col2)
                    {
                        var max = float.Parse(match.Groups[2].Value);
                        var min = float.Parse(match.Groups[4].Value);
                        
                        if (!result.ContainsKey(col1))
                            result[col1] = new ColumnConstraints();
                        
                        result[col1].RangeMin = min;
                        result[col1].RangeMax = max;
                    }
                }

                // Parse IN CHECK constraints (enum-like)
                // Pattern: CHECK(col IN ('val1', 'val2', 'val3'))
                var inPattern = @"CHECK\s*\(\s*[""']?(\w+)[""']?\s+IN\s*\(\s*(.+?)\s*\)\s*\)";
                foreach (Match match in Regex.Matches(createTableSQL, inPattern, RegexOptions.IgnoreCase))
                {
                    var columnName = match.Groups[1].Value;
                    var valuesList = match.Groups[2].Value;
                    
                    // Parse individual values
                    var values = new List<string>();
                    var valuePattern = @"[""']([^""']+)[""']";
                    foreach (Match valueMatch in Regex.Matches(valuesList, valuePattern))
                    {
                        values.Add(valueMatch.Groups[1].Value.Trim());
                    }
                    
                    if (values.Count > 0)
                    {
                        if (!result.ContainsKey(columnName))
                            result[columnName] = new ColumnConstraints();
                        
                        result[columnName].CheckInValues = values.ToArray();
                    }
                }

                // Parse UNIQUE constraints
                // Column-level: look for UNIQUE keyword after type
                var columnUniquePattern = @"(\w+)\s+\w+.*?\bUNIQUE\b";
                foreach (Match match in Regex.Matches(createTableSQL, columnUniquePattern, RegexOptions.IgnoreCase))
                {
                    var columnName = match.Groups[1].Value;
                    
                    if (!result.ContainsKey(columnName))
                        result[columnName] = new ColumnConstraints();
                    
                    result[columnName].IsUnique = true;
                }
                
                // Table-level: UNIQUE(column_name)
                var tableUniquePattern = @"UNIQUE\s*\(\s*[""']?(\w+)[""']?\s*\)";
                foreach (Match match in Regex.Matches(createTableSQL, tableUniquePattern, RegexOptions.IgnoreCase))
                {
                    var columnName = match.Groups[1].Value;
                    
                    if (!result.ContainsKey(columnName))
                        result[columnName] = new ColumnConstraints();
                    
                    result[columnName].IsUnique = true;
                }
            }
            catch
            {
                // Best-effort parsing - return what we have so far
            }

            return result;
        }
    }
}
