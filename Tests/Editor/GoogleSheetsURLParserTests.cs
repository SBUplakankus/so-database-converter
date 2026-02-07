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
            
            Assert.IsTrue(result.isValid);
            Assert.AreEqual("ABC123", result.spreadsheetId);
            Assert.AreEqual("0", result.gid);
        }

        [Test]
        public void TestParseShareURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit?usp=sharing");
            
            Assert.IsTrue(result.isValid);
            Assert.AreEqual("ABC123", result.spreadsheetId);
            Assert.AreEqual("0", result.gid);
        }

        [Test]
        public void TestParseShortURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/");
            
            Assert.IsTrue(result.isValid);
            Assert.AreEqual("ABC123", result.spreadsheetId);
            Assert.AreEqual("0", result.gid);
        }

        [Test]
        public void TestParseExportURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/export?format=csv&gid=12345");
            
            Assert.IsTrue(result.isValid);
            Assert.AreEqual("ABC123", result.spreadsheetId);
            Assert.AreEqual("12345", result.gid);
        }

        [Test]
        public void TestParseRawID()
        {
            var result = GoogleSheetsURLParser.Parse("ABC123DEF456GHI789JKL012");
            
            Assert.IsTrue(result.isValid);
            Assert.AreEqual("ABC123DEF456GHI789JKL012", result.spreadsheetId);
            Assert.AreEqual("0", result.gid);
        }

        [Test]
        public void TestParseGIDFromFragment()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit#gid=42");
            
            Assert.IsTrue(result.isValid);
            Assert.AreEqual("42", result.gid);
        }

        [Test]
        public void TestParseGIDFromQuery()
        {
            var result = GoogleSheetsURLParser.Parse("https://docs.google.com/spreadsheets/d/ABC123/edit?gid=42");
            
            Assert.IsTrue(result.isValid);
            Assert.AreEqual("42", result.gid);
        }

        [Test]
        public void TestInvalidURL()
        {
            var result = GoogleSheetsURLParser.Parse("https://example.com");
            
            Assert.IsFalse(result.isValid);
            Assert.IsNotNull(result.error);
        }

        [Test]
        public void TestEmptyString()
        {
            var result = GoogleSheetsURLParser.Parse("");
            
            Assert.IsFalse(result.isValid);
            Assert.AreEqual("URL is empty", result.error);
        }

        [Test]
        public void TestNullString()
        {
            var result = GoogleSheetsURLParser.Parse(null);
            
            Assert.IsFalse(result.isValid);
            Assert.AreEqual("URL is empty", result.error);
        }

        [Test]
        public void TestBuildExportURL()
        {
            string url = GoogleSheetsURLParser.BuildExportURL("ABC", "0");
            
            Assert.AreEqual("https://docs.google.com/spreadsheets/d/ABC/export?format=csv&gid=0", url);
        }
    }
}
