using DataToScriptableObject;
using System.Text.RegularExpressions;

namespace DataToScriptableObject.Editor
{
    public static class GoogleSheetsURLParser
    {
        #region Helper Methods

        private static readonly Regex SpreadsheetUrlRegex =
            new Regex(@"docs\.google\.com/spreadsheets/d/([a-zA-Z0-9_-]+)", RegexOptions.Compiled);

        private static readonly Regex GidRegex =
            new Regex(@"[?&#]gid=(\d+)", RegexOptions.Compiled);

        private static readonly Regex RawIdRegex =
            new Regex(@"^[a-zA-Z0-9_-]{20,}$", RegexOptions.Compiled);

        public static string BuildExportURL(string spreadsheetId, string gid)
        {
            return
                $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export" +
                $"?format=csv&gid={gid}";
        }
        #endregion

        #region Parse Methods

        public static GoogleSheetsParseResult Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new GoogleSheetsParseResult
                {
                    IsValid = false,
                    Error = "URL is empty"
                };
            }

            input = input.Trim();

            string spreadsheetId = null;
            var gid = "0";

            var spreadsheetIdMatch = SpreadsheetUrlRegex.Match(input);
            if (spreadsheetIdMatch.Success)
            {
                spreadsheetId = spreadsheetIdMatch.Groups[1].Value;

                var gidMatch = GidRegex.Match(input);
                if (gidMatch.Success)
                {
                    gid = gidMatch.Groups[1].Value;
                }
            }
            else if (RawIdRegex.IsMatch(input))
            {
                spreadsheetId = input;
                gid = "0";
            }

            if (string.IsNullOrEmpty(spreadsheetId))
            {
                return new GoogleSheetsParseResult
                {
                    IsValid = false,
                    Error =
                        "Not a valid Google Sheets URL. Expected format: " +
                        "https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit"
                };
            }

            return new GoogleSheetsParseResult
            {
                IsValid = true,
                SpreadsheetId = spreadsheetId,
                Gid = gid
            };
        }

        #endregion
        
        
    }
}
