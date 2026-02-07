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
        public static RawTableData Parse(string csvText, string delimiter, string commentPrefix, int headerRowCount)
        {
            if (string.IsNullOrEmpty(csvText))
            {
                return new RawTableData
                {
                    directives = new string[0],
                    headers = new string[0],
                    typeHints = null,
                    flags = null,
                    dataRows = new string[0][]
                };
            }

            if (csvText.StartsWith("\uFEFF"))
                csvText = csvText.Substring(1);

            csvText = csvText.Replace("\r\n", "\n").Replace("\r", "\n");

            var lines = new List<string>();
            var directives = new List<string>();
            var reader = new StringReader(csvText);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                
                if (trimmed.StartsWith("#class:") || trimmed.StartsWith("#database:") || trimmed.StartsWith("#namespace:"))
                {
                    directives.Add(line);
                }
                else if (!string.IsNullOrWhiteSpace(trimmed) && 
                         !trimmed.StartsWith(commentPrefix) && 
                         !trimmed.StartsWith("//"))
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
            for (int i = 0; i < lines.Count; i++)
            {
                string currentLine = lines[i];
                string trimmed = currentLine.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || 
                    trimmed.StartsWith(commentPrefix) || 
                    trimmed.StartsWith("//"))
                    continue;

                var fields = ParseLine(currentLine, delimiter, lines, ref i);
                parsedLines.Add(fields);
            }

            if (parsedLines.Count == 0)
            {
                return new RawTableData
                {
                    directives = directives.ToArray(),
                    headers = new string[0],
                    typeHints = null,
                    flags = null,
                    dataRows = new string[0][]
                };
            }

            var result = new RawTableData
            {
                directives = directives.ToArray(),
                headers = parsedLines[0]
            };

            int currentRow = 1;

            if (headerRowCount >= 2 && parsedLines.Count > currentRow)
            {
                result.typeHints = parsedLines[currentRow];
                currentRow++;
            }

            if (headerRowCount >= 3 && parsedLines.Count > currentRow)
            {
                result.flags = parsedLines[currentRow];
                currentRow++;
            }

            var dataRows = new List<string[]>();
            for (int i = currentRow; i < parsedLines.Count; i++)
            {
                var row = parsedLines[i];
                if (row.Length < result.headers.Length)
                {
                    var padded = new string[result.headers.Length];
                    Array.Copy(row, padded, row.Length);
                    for (int j = row.Length; j < padded.Length; j++)
                        padded[j] = "";
                    row = padded;
                }
                else if (row.Length > result.headers.Length)
                {
                    var truncated = new string[result.headers.Length];
                    Array.Copy(row, truncated, result.headers.Length);
                    row = truncated;
                }
                dataRows.Add(row);
            }

            result.dataRows = dataRows.ToArray();
            return result;
        }

        public static RawTableData ReadFromFile(string filePath, string delimiter, string commentPrefix, int headerRowCount)
        {
            string csvText = File.ReadAllText(filePath);
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
                var counts = linesToCheck.Select(l => l.Split(new[] { delim }, StringSplitOptions.None).Length).ToList();
                if (counts.Count > 0 && counts.All(c => c == counts[0]) && counts[0] > 1)
                    delimitersScores[delim] = counts[0];
            }

            if (delimitersScores.Count == 0)
                return ",";

            return delimitersScores.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private static string[] ParseLine(string line, string delimiter, List<string> allLines, ref int lineIndex)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;
            int i = 0;

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

                char c = line[i];

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
                    if (c == '"' && currentField.Length == 0)
                    {
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

        private static bool IsDelimiter(string line, int index, string delimiter)
        {
            if (index + delimiter.Length > line.Length)
                return false;

            return line.Substring(index, delimiter.Length) == delimiter;
        }
    }
}
