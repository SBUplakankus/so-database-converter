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
        
        private static bool IsComment(string trimmedLine, string commentPrefix)
        {
            if (trimmedLine.StartsWith("//"))
                return true;
            if (!string.IsNullOrEmpty(commentPrefix) && trimmedLine.StartsWith(commentPrefix) && !IsDirective(trimmedLine))
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

        /// <summary>
        /// RFC 4180 compliant CSV line parser. Reads one logical record from csvData
        /// starting at position pos. Handles quoted fields with embedded delimiters,
        /// newlines, and escaped double-quotes.
        /// Returns null when there is no more data to read.
        /// </summary>
        private static string[] ParseRecord(string csvData, ref int pos, string delimiter)
        {
            if (pos >= csvData.Length)
                return null;

            var fields = new List<string>();
            var delimLen = delimiter.Length;

            while (true)
            {
                if (pos >= csvData.Length)
                {
                    // End of data after a delimiter — trailing empty field
                    fields.Add("");
                    break;
                }

                if (csvData[pos] == '"')
                {
                    // Quoted field
                    pos++; // skip opening quote
                    var sb = new StringBuilder();
                    while (pos < csvData.Length)
                    {
                        if (csvData[pos] == '"')
                        {
                            if (pos + 1 < csvData.Length && csvData[pos + 1] == '"')
                            {
                                // Escaped double-quote
                                sb.Append('"');
                                pos += 2;
                            }
                            else
                            {
                                // Closing quote
                                pos++; // skip closing quote
                                break;
                            }
                        }
                        else
                        {
                            sb.Append(csvData[pos]);
                            pos++;
                        }
                    }
                    fields.Add(sb.ToString());

                    // After closing quote, expect delimiter or end of line/data
                    if (pos < csvData.Length && csvData[pos] == '\n')
                    {
                        pos++; // consume newline
                        break;
                    }
                    if (pos + delimLen <= csvData.Length && csvData.Substring(pos, delimLen) == delimiter)
                    {
                        pos += delimLen; // consume delimiter
                        continue;
                    }
                    // Skip any trailing characters until delimiter or newline
                    while (pos < csvData.Length && csvData[pos] != '\n' &&
                           !(pos + delimLen <= csvData.Length && csvData.Substring(pos, delimLen) == delimiter))
                    {
                        pos++;
                    }
                    if (pos < csvData.Length && csvData[pos] == '\n')
                    {
                        pos++;
                        break;
                    }
                    if (pos + delimLen <= csvData.Length && csvData.Substring(pos, delimLen) == delimiter)
                    {
                        pos += delimLen;
                        continue;
                    }
                    break; // end of data
                }
                else
                {
                    // Unquoted field — read until delimiter or newline
                    var start = pos;
                    while (pos < csvData.Length && csvData[pos] != '\n' &&
                           !(pos + delimLen <= csvData.Length && csvData.Substring(pos, delimLen) == delimiter))
                    {
                        pos++;
                    }
                    fields.Add(csvData.Substring(start, pos - start));

                    if (pos < csvData.Length && csvData[pos] == '\n')
                    {
                        pos++; // consume newline
                        break;
                    }
                    if (pos + delimLen <= csvData.Length && csvData.Substring(pos, delimLen) == delimiter)
                    {
                        pos += delimLen; // consume delimiter
                        continue;
                    }
                    break; // end of data
                }
            }

            return fields.ToArray();
        }
        
        #endregion

        #region Parse Methods
        
        public static RawTableData Parse(string csvText, string delimiter, string commentPrefix, int headerRowCount)
        {
            if (string.IsNullOrEmpty(csvText))
                return EmptyResult();

            // Remove BOM if present (must use Ordinal comparison — culture-sensitive
            // StartsWith treats the zero-width BOM as matching any string)
            if (csvText.Length > 0 && csvText[0] == '\uFEFF')
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
                        else if (string.IsNullOrWhiteSpace(trimmed) || IsComment(trimmed, commentPrefix))
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
            
            // Parse CSV records using custom RFC 4180 parser
            var parsedLines = new List<string[]>();
            int pos = 0;
            
            while (pos < csvData.Length)
            {
                // Check if current line is a comment (only for unquoted lines)
                // Peek at the line start to see if it's a comment
                var peekEnd = csvData.IndexOf('\n', pos);
                if (peekEnd < 0) peekEnd = csvData.Length;
                var peekLine = csvData.Substring(pos, peekEnd - pos).Trim();
                
                if (string.IsNullOrWhiteSpace(peekLine))
                {
                    pos = peekEnd < csvData.Length ? peekEnd + 1 : csvData.Length;
                    continue;
                }

                if (IsComment(peekLine, commentPrefix))
                {
                    pos = peekEnd < csvData.Length ? peekEnd + 1 : csvData.Length;
                    continue;
                }
                
                var fields = ParseRecord(csvData, ref pos, delimiter);
                if (fields != null && fields.Length > 0)
                    parsedLines.Add(fields);
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
                    if (!string.IsNullOrWhiteSpace(trimmed) && !IsComment(trimmed, commentPrefix))
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
