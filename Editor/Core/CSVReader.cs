using DataToScriptableObject;
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
        
        private static bool IsIgnorableLine(string line, string commentPrefix)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true;

            var trimmed = line.Trim();
            
            // Directives are not ignorable - they must be processed
            if (IsDirective(trimmed))
                return false;
            
            return trimmed.StartsWith(commentPrefix) || trimmed.StartsWith("//");
        }
        
        private static bool IsDelimiter(string line, int index, string delimiter)
        {
            if (index + delimiter.Length > line.Length)
                return false;

            return string.CompareOrdinal(
                line, index,
                delimiter, 0,
                delimiter.Length) == 0;
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

            if (csvText.StartsWith("\uFEFF"))
                csvText = csvText.Substring(1);

            csvText = csvText.Replace("\r\n", "\n").Replace("\r", "\n");

            var lines = new List<string>();
            var directives = new List<string>();
            using var reader = new StringReader(csvText);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();

                if (IsDirective(trimmed))
                {
                    directives.Add(line);
                }
                else if (!IsIgnorableLine(line, commentPrefix))
                {
                    lines.Add(line);
                    break;
                }
            }

            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            if (delimiter == "auto")
                delimiter = DetectDelimiter(lines, commentPrefix);

            var parsedLines = new List<string[]>();
            for (var i = 0; i < lines.Count; i++)
            {
                var currentLine = lines[i];

                if (IsIgnorableLine(currentLine, commentPrefix))
                    continue;

                var fields = ParseLine(currentLine, delimiter, lines, ref i);
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
                result.Flags = NormalizeRow(parsedLines[currentRow], columnCount);
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

        private static string DetectDelimiter(List<string> lines, string commentPrefix)
        {
            var candidates = new[] { ",", ";", "\t" };
            var linesToCheck = lines
                .Where(l => !string.IsNullOrWhiteSpace(l) && 
                           !l.Trim().StartsWith(commentPrefix) && 
                           !l.Trim().StartsWith("//"))
                .Take(5)
                .ToList();

            if (linesToCheck.Count == 0)
                return ",";

            var delimitersScores = new Dictionary<string, int>();
            foreach (var delim in candidates)
            {
                var counts = linesToCheck
                    .Select(l => CountOccurrences(l, delim))
                    .ToList();
                
                if (counts.Count > 0 && counts.All(c => c == counts[0]) && counts[0] > 1)
                    delimitersScores[delim] = counts[0];
            }

            return delimitersScores.Count == 0 ? "," : delimitersScores.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private static string[] ParseLine(string line, string delimiter, List<string> allLines, ref int lineIndex)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            var inQuotes = false;
            var i = 0;

            while (i < line.Length || inQuotes)
            {
                if (i >= line.Length && inQuotes)
                {
                    currentField.Append('\n');
                    lineIndex++;
                    if (lineIndex < allLines.Count)
                    {
                        line = allLines[lineIndex];
                        i = 0;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                var c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i += 2;
                        }
                        else
                        {
                            inQuotes = false;
                            i++;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                        i++;
                    }
                }
                else
                {
                    if (c == '"' && currentField.ToString().All(char.IsWhiteSpace))
                    {
                        currentField.Clear();
                        inQuotes = true;
                        i++;
                    }
                    else if (IsDelimiter(line, i, delimiter))
                    {
                        fields.Add(currentField.ToString());
                        currentField.Clear();
                        i += delimiter.Length;
                    }
                    else
                    {
                        currentField.Append(c);
                        i++;
                    }
                }
            }

            fields.Add(currentField.ToString());

            return fields.ToArray();
        }
        
        #endregion
    }
}
