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

            // Extract directives from the prelude (before any data)
            var directives = new List<string>();
            var csvDataStart = 0;
            
            using (var reader = new StringReader(csvText))
            {
                string line;
                var inPrelude = true;
                
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    
                    if (inPrelude)
                    {
                        if (IsDirective(trimmed))
                        {
                            directives.Add(line);
                            csvDataStart += line.Length + 1; // +1 for newline
                        }
                        else if (string.IsNullOrWhiteSpace(trimmed) || 
                                trimmed.StartsWith(commentPrefix) || 
                                trimmed.StartsWith("//"))
                        {
                            csvDataStart += line.Length + 1;
                        }
                        else
                        {
                            // First non-directive, non-comment, non-empty line = end of prelude
                            inPrelude = false;
                            break;
                        }
                    }
                }
            }

            // Extract the CSV data portion (after directives)
            var csvData = csvDataStart < csvText.Length ? csvText.Substring(csvDataStart) : csvText;
            
            // Auto-detect delimiter if needed
            if (delimiter == "auto")
                delimiter = DetectDelimiterFromText(csvData, commentPrefix);
            
            // Convert delimiter to TextFieldParser format
            var delimiterChar = ConvertDelimiter(delimiter);
            
            // Parse CSV using TextFieldParser
            var parsedLines = new List<string[]>();
            
            using (var reader = new StringReader(csvData))
            using (var parser = new TextFieldParser(reader))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(delimiterChar);
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = false;
                
                // Set comment tokens
                var commentTokens = new List<string>();
                if (!string.IsNullOrEmpty(commentPrefix))
                    commentTokens.Add(commentPrefix);
                commentTokens.Add("//");
                parser.CommentTokens = commentTokens.ToArray();

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
                        // Skip malformed lines
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

        private static string ConvertDelimiter(string delimiter)
        {
            // TextFieldParser expects a string array of delimiters
            // but we return a single delimiter string for SetDelimiters()
            return delimiter;
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
