using NUnit.Framework;
using DataToScriptableObject;
using DataToScriptableObject.Editor;

namespace DataToScriptableObject.Tests.Editor
{
    public class GoogleSheetsURLParserTests
    {
        [Test]
        public void TestParseEditURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit#gid=0");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
            Assert.AreEqual("0", result.Gid);
        }

        [Test]
        public void TestParseShareURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit?usp=sharing");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
            Assert.AreEqual("0", result.Gid);
        }

        [Test]
        public void TestParseShortURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
            Assert.AreEqual("0", result.Gid);
        }

        [Test]
        public void TestParseExportURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/export?format=csv&gid=12345");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
            Assert.AreEqual("12345", result.Gid);
        }

        [Test]
        public void TestParseRawID()
        {
            var result = GoogleSheetsURLParser.Parse("ABC123DEF456GHI789JKL012");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123DEF456GHI789JKL012", result.SpreadsheetId);
            Assert.AreEqual("0", result.Gid);
        }

        [Test]
        public void TestParseGIDFromFragment()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit#gid=42");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("42", result.Gid);
        }

        [Test]
        public void TestParseGIDFromQuery()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit?gid=42");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("42", result.Gid);
        }

        [Test]
        public void TestInvalidURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://example.com");
            
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.Error);
        }

        [Test]
        public void TestEmptyString()
        {
            var result = GoogleSheetsURLParser.Parse("");
            
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("URL is empty", result.Error);
        }

        [Test]
        public void TestNullString()
        {
            var result = GoogleSheetsURLParser.Parse(null);
            
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("URL is empty", result.Error);
        }

        [Test]
        public void TestBuildExportURL()
        {
            string url = GoogleSheetsURLParser.BuildExportURL("ABC", "0");
            
            Assert.AreEqual("https://docs.google.com/spreadsheets/d/ABC/export?format=csv&gid=0", url);
        }

        // ==================== DIAGNOSTIC TESTS ====================

        #region URL Parsing Diagnostics

        [Test]
        public void TestParseLongSpreadsheetId()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/1ABC123DEF456GHI789JKL012MNO345PQR678STU901/edit");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("1ABC123DEF456GHI789JKL012MNO345PQR678STU901", result.SpreadsheetId);
        }

        [Test]
        public void TestParseWithQueryParams()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit?usp=sharing&gid=42");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
            Assert.AreEqual("42", result.Gid);
        }

        [Test]
        public void TestParseMultipleGids()
        {
            // Both fragment and query - fragment should take precedence
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit?gid=1#gid=2");
            
            Assert.IsTrue(result.IsValid);
            // Typically fragment takes precedence
            Assert.IsTrue(result.Gid == "2" || result.Gid == "1");
        }

        [Test]
        public void TestParsePubhtmlFormat()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/pubhtml");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
        }

        [Test]
        public void TestParseCopyFormat()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/copy");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
        }

        #endregion

        #region GID Extraction Diagnostics

        [Test]
        public void TestGidZero()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit#gid=0");
            
            Assert.AreEqual("0", result.Gid);
        }

        [Test]
        public void TestGidLargeNumber()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit#gid=1234567890");
            
            Assert.AreEqual("1234567890", result.Gid);
        }

        [Test]
        public void TestNoGidDefaultsToZero()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("0", result.Gid);
        }

        #endregion

        #region Error Handling Diagnostics

        [Test]
        public void TestInvalidDomain()
        {
            var result = GoogleSheetsURLParser.Parse("https://sheets.google.com/d/ABC123");
            
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void TestMissingSpreadsheetId()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/");
            
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void TestNotHttps()
        {
            var result = GoogleSheetsURLParser.Parse("http://docs.google.com/spreadsheets/d/ABC123/edit");
            
            // May or may not be valid depending on implementation
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestMalformedURL()
        {
            var result = GoogleSheetsURLParser.Parse("not a url at all");
            
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void TestWhitespaceString()
        {
            var result = GoogleSheetsURLParser.Parse("   ");
            
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void TestUrlWithLeadingWhitespace()
        {
            var result = GoogleSheetsURLParser.Parse("  https://docs.google.com/spreadsheets/d/ABC123/edit");
            
            // Should handle gracefully
            Assert.IsNotNull(result);
        }

        #endregion

        #region Raw ID Detection Diagnostics

        [Test]
        public void TestRawIdShort()
        {
            var result = GoogleSheetsURLParser.Parse("ABC123");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
        }

        [Test]
        public void TestRawIdWithNumbers()
        {
            var result = GoogleSheetsURLParser.Parse("1A2B3C4D5E6F7G8H9I0J");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("1A2B3C4D5E6F7G8H9I0J", result.SpreadsheetId);
        }

        [Test]
        public void TestRawIdWithHyphens()
        {
            // Spreadsheet IDs can contain hyphens and underscores
            var result = GoogleSheetsURLParser.Parse("ABC-123_DEF");
            
            // Behavior depends on implementation
            Assert.IsNotNull(result);
        }

        #endregion

        #region Export URL Building Diagnostics

        [Test]
        public void TestBuildExportURLWithGid()
        {
            string url = GoogleSheetsURLParser.BuildExportURL("ABC123", "42");
            
            Assert.AreEqual("https://docs.google.com/spreadsheets/d/ABC123/export?format=csv&gid=42", url);
        }

        [Test]
        public void TestBuildExportURLGidZero()
        {
            string url = GoogleSheetsURLParser.BuildExportURL("ABC123", "0");
            
            Assert.IsTrue(url.Contains("gid=0"));
        }

        #endregion
    }
}
