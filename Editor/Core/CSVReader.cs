using DataToScriptableObject;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataToScriptableObject.Editor
{
    public static class CSVReader
    {

        #region Fields

        private static readonly string[] DirectivePrefixes =
        {
            "#class:",
            "#database:",
            "#namespace:"
        };

        #endregion
        
        #region Helper Methods
        private static RawTableData EmptyResult(string[] directives = null)
        {
            return new RawTableData
            {
                Directives = directives ?? Array.Empty<string>(),
                Headers = Array.Empty<string>(),
                TypeHints = null,
                Flags = null,
                DataRows = Array.Empty<string[]>()
            };
        }
        
        private static bool IsDirective(string trimmedLine)
        {
            foreach (var prefix in DirectivePrefixes)
                if (trimmedLine.StartsWith(prefix))
                    return true;
            
            return false;
        }
        
        private static string[] NormalizeRow(string[] row, int length)
        {
            if (row.Length == length)
                return row;

            var result = new string[length];
            Array.Copy(row, result, Math.Min(row.Length, length));
            return result;
        }
        
        #endregion

        #region Parse Methods
        
        public static RawTableData Parse(string csvText, string delimiter, string commentPrefix, int headerRowCount)
        {
            if (string.IsNullOrEmpty(csvText))
                return EmptyResult();

            // Remove BOM if present
            if (csvText.StartsWith("\uFEFF"))
                csvText = csvText.Substring(1);

            // Normalize line endings
            csvText = csvText.Replace("\r\n", "\n").Replace("\r", "\n");

            // Extract directives from the prelude (before CSV data)
            // Directives must appear at the start, before any non-comment, non-empty lines
            var directives = new List<string>();
            var csvDataLines = new List<string>();
            var inPrelude = true;
            
            using (var reader = new StringReader(csvText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    
                    if (inPrelude)
                    {
                        if (IsDirective(trimmed))
                        {
                            directives.Add(line);
                        }
                        else if (string.IsNullOrWhiteSpace(trimmed) || 
                                trimmed.StartsWith(commentPrefix) || 
                                trimmed.StartsWith("//"))
                        {
                            // Skip empty lines and comments in prelude
                        }
                        else
                        {
                            // First non-directive, non-comment, non-empty line marks end of prelude
                            inPrelude = false;
                            csvDataLines.Add(line);
                        }
                    }
                    else
                    {
                        // After prelude, collect all lines for TextFieldParser
                        csvDataLines.Add(line);
                    }
                }
            }

            if (csvDataLines.Count == 0)
                return EmptyResult(directives.ToArray());

            // Reconstruct CSV data without directives
            var csvData = string.Join("\n", csvDataLines);
            
            // Auto-detect delimiter if needed
            if (delimiter == "auto")
                delimiter = DetectDelimiterFromText(csvData, commentPrefix);
            
            // Parse CSV using TextFieldParser - let it handle all the complexity
            var parsedLines = new List<string[]>();
            
            using (var reader = new StringReader(csvData))
            using (var parser = new TextFieldParser(reader))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(delimiter);
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = false;
                
                // Since we've already extracted directives, we still need to skip 
                // comment lines in the data section (after headers)
                if (!string.IsNullOrEmpty(commentPrefix))
                {
                    parser.CommentTokens = new[] { commentPrefix, "//" };
                }
                else
                {
                    parser.CommentTokens = new[] { "//" };
                }

                while (!parser.EndOfData)
                {
                    try
                    {
                        string[] fields = parser.ReadFields();
                        if (fields != null)
                            parsedLines.Add(fields);
                    }
                    catch (MalformedLineException)
                    {
                        // Skip malformed lines - TextFieldParser will handle most edge cases
                        continue;
                    }
                }
            }

            if (parsedLines.Count == 0)
                return EmptyResult(directives.ToArray());

            var result = new RawTableData
            {
                Directives = directives.ToArray(),
                Headers = parsedLines[0]
            };

            var columnCount = result.Headers.Length;
            var currentRow = 1;
            
            result.Headers = NormalizeRow(result.Headers, columnCount);

            if (headerRowCount >= 2 && parsedLines.Count > currentRow)
            {
                result.TypeHints = NormalizeRow(parsedLines[currentRow], columnCount);
                currentRow++;
            }

            if (headerRowCount >= 3 && parsedLines.Count > currentRow)
            {
                // Don't normalize flags row - it may have extra columns for enum values
                result.Flags = parsedLines[currentRow];
                currentRow++;
            }

            var dataRows = new List<string[]>();
            for (int i = currentRow; i < parsedLines.Count; i++)
            {
                var row = parsedLines[i];
                if (row.Length < result.Headers.Length)
                {
                    var padded = new string[result.Headers.Length];
                    Array.Copy(row, padded, row.Length);
                    
                    for (var j = row.Length; j < padded.Length; j++)
                        padded[j] = "";
                    
                    row = padded;
                }
                else if (row.Length > result.Headers.Length)
                {
                    var truncated = new string[result.Headers.Length];
                    Array.Copy(row, truncated, result.Headers.Length);
                    row = truncated;
                }
                dataRows.Add(row);
            }

            result.DataRows = dataRows.ToArray();
            return result;
        }

        public static RawTableData ReadFromFile(string filePath, string delimiter, string commentPrefix, int headerRowCount)
        {
            var csvText = File.ReadAllText(filePath);
            return Parse(csvText, delimiter, commentPrefix, headerRowCount);
        }

        private static string DetectDelimiterFromText(string csvData, string commentPrefix)
        {
            var candidates = new[] { ",", ";", "\t" };
            var lines = new List<string>();
            
            using (var reader = new StringReader(csvData))
            {
                string line;
                while ((line = reader.ReadLine()) != null && lines.Count < 5)
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && 
                        !trimmed.StartsWith(commentPrefix) && 
                        !trimmed.StartsWith("//"))
                    {
                        lines.Add(line);
                    }
                }
            }

            if (lines.Count == 0)
                return ",";

            var delimiterScores = new Dictionary<string, int>();
            foreach (var delim in candidates)
            {
                var counts = lines
                    .Select(l => CountOccurrences(l, delim))
                    .ToList();
                
                if (counts.Count > 0 && counts.All(c => c == counts[0]) && counts[0] > 1)
                    delimiterScores[delim] = counts[0];
            }

            return delimiterScores.Count == 0 ? "," : delimiterScores.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private static int CountOccurrences(string line, string delim)
        {
            var count = 0;
            var index = 0;

            while ((index = line.IndexOf(delim, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += delim.Length;
            }

            return count + 1;
        }
        
        #endregion
    }
}
