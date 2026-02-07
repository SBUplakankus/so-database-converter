using NUnit.Framework;
using DataToScriptableObject;
using DataToScriptableObject.Editor;

namespace DataToScriptableObject.Tests.Editor
{
    public class CSVReaderTests
    {
        [Test]
        public void TestParseBasicCSV()
        {
            string csv = "a,b,c\n1,2,3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.headers.Length);
            Assert.AreEqual("a", result.headers[0]);
            Assert.AreEqual("b", result.headers[1]);
            Assert.AreEqual("c", result.headers[2]);
            Assert.AreEqual(1, result.dataRows.Length);
            Assert.AreEqual("1", result.dataRows[0][0]);
            Assert.AreEqual("2", result.dataRows[0][1]);
            Assert.AreEqual("3", result.dataRows[0][2]);
        }

        [Test]
        public void TestParseSemicolonDelimiter()
        {
            string csv = "a;b;c\n1;2;3";
            var result = CSVReader.Parse(csv, ";", "#", 1);
            
            Assert.AreEqual(3, result.headers.Length);
            Assert.AreEqual(1, result.dataRows.Length);
            Assert.AreEqual("1", result.dataRows[0][0]);
        }

        [Test]
        public void TestParseTabDelimiter()
        {
            string csv = "a\tb\tc\n1\t2\t3";
            var result = CSVReader.Parse(csv, "\t", "#", 1);
            
            Assert.AreEqual(3, result.headers.Length);
            Assert.AreEqual(1, result.dataRows.Length);
        }

        [Test]
        public void TestAutoDetectComma()
        {
            string csv = "a,b,c\n1,2,3";
            var result = CSVReader.Parse(csv, "auto", "#", 1);
            
            Assert.AreEqual(3, result.headers.Length);
            Assert.AreEqual(1, result.dataRows.Length);
        }

        [Test]
        public void TestAutoDetectSemicolon()
        {
            string csv = "a;b;c\n1;2;3";
            var result = CSVReader.Parse(csv, "auto", "#", 1);
            
            Assert.AreEqual(3, result.headers.Length);
            Assert.AreEqual(1, result.dataRows.Length);
        }

        [Test]
        public void TestQuotedFieldWithComma()
        {
            string csv = "a,b,c\n1,\"hello, world\",3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.dataRows.Length);
            Assert.AreEqual("hello, world", result.dataRows[0][1]);
        }

        [Test]
        public void TestQuotedFieldWithNewline()
        {
            string csv = "a,b,c\n1,\"hello\nworld\",3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.dataRows.Length);
            Assert.AreEqual("hello\nworld", result.dataRows[0][1]);
        }

        [Test]
        public void TestEscapedDoubleQuotes()
        {
            string csv = "a\n\"he said \"\"hi\"\"\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.dataRows.Length);
            Assert.AreEqual("he said \"hi\"", result.dataRows[0][0]);
        }

        [Test]
        public void TestBOMStripped()
        {
            string csv = "\uFEFFa,b,c\n1,2,3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual("a", result.headers[0]);
        }

        [Test]
        public void TestCommentLinesSkipped()
        {
            string csv = "a,b,c\n1,2,3\n# comment\n4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.dataRows.Length);
            Assert.AreEqual("1", result.dataRows[0][0]);
            Assert.AreEqual("4", result.dataRows[1][0]);
        }

        [Test]
        public void TestDirectivesParsed()
        {
            string csv = "#class:MyClass\n#namespace:Game\na,b\n1,2";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.directives.Length);
            Assert.IsTrue(result.directives[0].Contains("MyClass"));
            Assert.IsTrue(result.directives[1].Contains("Game"));
        }

        [Test]
        public void TestEmptyFile()
        {
            string csv = "";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(0, result.headers.Length);
            Assert.AreEqual(0, result.dataRows.Length);
        }

        [Test]
        public void TestMismatchedColumns()
        {
            string csv = "a,b,c\n1,2\n4,5,6,7";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.headers.Length);
            Assert.AreEqual(2, result.dataRows.Length);
        }

        [Test]
        public void TestWindowsLineEndings()
        {
            string csv = "a,b,c\r\n1,2,3\r\n4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.dataRows.Length);
        }

        [Test]
        public void TestMacLineEndings()
        {
            string csv = "a,b,c\r1,2,3\r4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.dataRows.Length);
        }

        [Test]
        public void TestEmptyRowsSkipped()
        {
            string csv = "a,b,c\n1,2,3\n\n4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.dataRows.Length);
            Assert.AreEqual("1", result.dataRows[0][0]);
            Assert.AreEqual("4", result.dataRows[1][0]);
        }
    }
}
