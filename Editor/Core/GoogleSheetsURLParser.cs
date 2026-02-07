using DataToScriptableObject;
using System.Text.RegularExpressions;

namespace DataToScriptableObject.Editor
{
    public static class GoogleSheetsURLParser
    {
        public static GoogleSheetsParseResult Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new GoogleSheetsParseResult
                {
                    isValid = false,
                    error = "URL is empty"
                };
            }

            input = input.Trim();

            var spreadsheetIdMatch = Regex.Match(input, @"docs\.google\.com/spreadsheets/d/([a-zA-Z0-9_-]+)");
            string spreadsheetId = null;
            string gid = "0";

            if (spreadsheetIdMatch.Success)
            {
                spreadsheetId = spreadsheetIdMatch.Groups[1].Value;

                var gidMatch = Regex.Match(input, @"[?&#]gid=(\d+)");
                if (gidMatch.Success)
                {
                    gid = gidMatch.Groups[1].Value;
                }
            }
            else
            {
                var rawIdMatch = Regex.Match(input, @"^[a-zA-Z0-9_-]{20,}$");
                if (rawIdMatch.Success)
                {
                    spreadsheetId = input;
                    gid = "0";
                }
            }

            if (string.IsNullOrEmpty(spreadsheetId))
            {
                return new GoogleSheetsParseResult
                {
                    isValid = false,
                    error = "Not a valid Google Sheets URL. Expected format: https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit"
                };
            }

            return new GoogleSheetsParseResult
            {
                isValid = true,
                spreadsheetId = spreadsheetId,
                gid = gid
            };
        }

        public static string BuildExportURL(string spreadsheetId, string gid)
        {
            return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";
        }
    }
}
