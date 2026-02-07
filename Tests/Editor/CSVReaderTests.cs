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
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual("a", result.Headers[0]);
            Assert.AreEqual("b", result.Headers[1]);
            Assert.AreEqual("c", result.Headers[2]);
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
            Assert.AreEqual("2", result.DataRows[0][1]);
            Assert.AreEqual("3", result.DataRows[0][2]);
        }

        [Test]
        public void TestParseSemicolonDelimiter()
        {
            string csv = "a;b;c\n1;2;3";
            var result = CSVReader.Parse(csv, ";", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
        }

        [Test]
        public void TestParseTabDelimiter()
        {
            string csv = "a\tb\tc\n1\t2\t3";
            var result = CSVReader.Parse(csv, "\t", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
        }

        [Test]
        public void TestAutoDetectComma()
        {
            string csv = "a,b,c\n1,2,3";
            var result = CSVReader.Parse(csv, "auto", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
        }

        [Test]
        public void TestAutoDetectSemicolon()
        {
            string csv = "a;b;c\n1;2;3";
            var result = CSVReader.Parse(csv, "auto", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
        }

        [Test]
        public void TestQuotedFieldWithComma()
        {
            string csv = "a,b,c\n1,\"hello, world\",3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("hello, world", result.DataRows[0][1]);
        }

        [Test]
        public void TestQuotedFieldWithNewline()
        {
            string csv = "a,b,c\n1,\"hello\nworld\",3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("hello\nworld", result.DataRows[0][1]);
        }

        [Test]
        public void TestEscapedDoubleQuotes()
        {
            string csv = "a\n\"he said \"\"hi\"\"\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("he said \"hi\"", result.DataRows[0][0]);
        }

        [Test]
        public void TestBOMStripped()
        {
            string csv = "\uFEFFa,b,c\n1,2,3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual("a", result.Headers[0]);
        }

        [Test]
        public void TestCommentLinesSkipped()
        {
            string csv = "a,b,c\n1,2,3\n# comment\n4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
            Assert.AreEqual("4", result.DataRows[1][0]);
        }

        [Test]
        public void TestDirectivesParsed()
        {
            string csv = "#class:MyClass\n#namespace:Game\na,b\n1,2";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Directives.Length);
            Assert.IsTrue(result.Directives[0].Contains("MyClass"));
            Assert.IsTrue(result.Directives[1].Contains("Game"));
        }

        [Test]
        public void TestEmptyFile()
        {
            string csv = "";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(0, result.Headers.Length);
            Assert.AreEqual(0, result.DataRows.Length);
        }

        [Test]
        public void TestMismatchedColumns()
        {
            string csv = "a,b,c\n1,2\n4,5,6,7";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(2, result.DataRows.Length);
        }

        [Test]
        public void TestWindowsLineEndings()
        {
            string csv = "a,b,c\r\n1,2,3\r\n4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
        }

        [Test]
        public void TestMacLineEndings()
        {
            string csv = "a,b,c\r1,2,3\r4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
        }

        [Test]
        public void TestEmptyRowsSkipped()
        {
            string csv = "a,b,c\n1,2,3\n\n4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
            Assert.AreEqual("4", result.DataRows[1][0]);
        }

        // ==================== DIAGNOSTIC TESTS ====================
        // These tests help diagnose the root causes of failures

        #region Header Parsing Diagnostics

        [Test]
        public void TestHeaderCount()
        {
            // Diagnostic: Verify headers are parsed correctly
            string csv = "a,b,c\n1,2,3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length, "Should have 3 headers");
        }

        [Test]
        public void TestHeaderFirstValue()
        {
            // Diagnostic: Specifically test first header value
            string csv = "first,second,third\n1,2,3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual("first", result.Headers[0], "First header should be 'first'");
        }

        [Test]
        public void TestSingleColumnCSV()
        {
            // Edge case: Single column with no delimiter
            string csv = "header\nvalue1\nvalue2";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.Headers.Length);
            Assert.AreEqual("header", result.Headers[0]);
            Assert.AreEqual(2, result.DataRows.Length);
        }

        [Test]
        public void TestTwoRowHeaderMode()
        {
            // Test two-row header mode separately
            string csv = "name,value\nstring,int\nItem,100";
            var result = CSVReader.Parse(csv, ",", "#", 2);
            
            Assert.AreEqual(2, result.Headers.Length);
            Assert.AreEqual("name", result.Headers[0]);
            Assert.AreEqual("value", result.Headers[1]);
            Assert.IsNotNull(result.TypeHints);
            Assert.AreEqual("string", result.TypeHints[0]);
            Assert.AreEqual("int", result.TypeHints[1]);
        }

        [Test]
        public void TestThreeRowHeaderMode()
        {
            // Test three-row header mode with flags
            string csv = "id,name\nint,string\nkey,name\n1,Item";
            var result = CSVReader.Parse(csv, ",", "#", 3);
            
            Assert.AreEqual(2, result.Headers.Length);
            Assert.IsNotNull(result.TypeHints);
            Assert.IsNotNull(result.Flags);
            Assert.AreEqual("key", result.Flags[0]);
            Assert.AreEqual("name", result.Flags[1]);
        }

        #endregion

        #region Quote Handling Diagnostics

        [Test]
        public void TestSimpleQuotedField()
        {
            // Simplest quoted field case
            string csv = "a\n\"quoted\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length, "Should have 1 data row");
            Assert.AreEqual("quoted", result.DataRows[0][0], "Should parse quoted field");
        }

        [Test]
        public void TestQuotedFieldPreservesWhitespace()
        {
            string csv = "a\n\"  spaces  \"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("  spaces  ", result.DataRows[0][0]);
        }

        [Test]
        public void TestDoubleQuoteEscapeSimple()
        {
            // Minimal double quote escape case
            string csv = "a\n\"\"\"x\"\"\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            // Expected: "x" (the outer quotes are field delimiters, inner "" becomes ")
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("\"x\"", result.DataRows[0][0]);
        }

        [Test]
        public void TestMixedQuotedAndUnquoted()
        {
            string csv = "a,b,c\nunquoted,\"quoted\",123";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("unquoted", result.DataRows[0][0]);
            Assert.AreEqual("quoted", result.DataRows[0][1]);
            Assert.AreEqual("123", result.DataRows[0][2]);
        }

        [Test]
        public void TestEmptyQuotedField()
        {
            string csv = "a,b\n\"\",value";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("", result.DataRows[0][0]);
            Assert.AreEqual("value", result.DataRows[0][1]);
        }

        [Test]
        public void TestMultilineQuotedField()
        {
            // Field with newline inside quotes
            string csv = "a,b\n\"line1\nline2\",value";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("line1\nline2", result.DataRows[0][0]);
        }

        #endregion

        #region Directive Parsing Diagnostics

        [Test]
        public void TestSingleDirective()
        {
            // Minimal directive case
            string csv = "#class:Test\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.Directives.Length, "Should have 1 directive");
            Assert.IsTrue(result.Directives[0].Contains("Test"), "Directive should contain 'Test'");
        }

        [Test]
        public void TestDirectivePreservation()
        {
            // Verify directive is preserved exactly
            string csv = "#class:MyClassName\na,b\n1,2";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.Directives.Length);
            Assert.AreEqual("#class:MyClassName", result.Directives[0]);
        }

        [Test]
        public void TestMultipleDirectives()
        {
            string csv = "#class:Test\n#namespace:MyNS\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Directives.Length);
        }

        [Test]
        public void TestDirectiveVsComment()
        {
            // Comments without colon should NOT be directives
            string csv = "# This is a comment\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(0, result.Directives.Length, "Plain comments should not be directives");
        }

        [Test]
        public void TestDatabaseDirective()
        {
            string csv = "#database:ItemDB\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.Directives.Length);
            Assert.IsTrue(result.Directives[0].Contains("ItemDB"));
        }

        [Test]
        public void TestNamespaceDirective()
        {
            string csv = "#namespace:Game.Data\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.Directives.Length);
            Assert.IsTrue(result.Directives[0].Contains("Game.Data"));
        }

        [Test]
        public void TestDirectivesWithData()
        {
            // Verify directives don't break data parsing
            string csv = "#class:Test\na,b,c\n1,2,3\n4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(2, result.DataRows.Length);
        }

        #endregion

        #region Delimiter Detection Diagnostics

        [Test]
        public void TestAutoDetectTab()
        {
            string csv = "a\tb\tc\n1\t2\t3";
            var result = CSVReader.Parse(csv, "auto", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
        }

        [Test]
        public void TestExplicitTabDelimiter()
        {
            string csv = "a\tb\tc\n1\t2\t3";
            var result = CSVReader.Parse(csv, "\t", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual("a", result.Headers[0]);
            Assert.AreEqual("b", result.Headers[1]);
            Assert.AreEqual("c", result.Headers[2]);
        }

        [Test]
        public void TestPipeDelimiter()
        {
            string csv = "a|b|c\n1|2|3";
            var result = CSVReader.Parse(csv, "|", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
        }

        #endregion

        #region Edge Cases and Error Conditions

        [Test]
        public void TestNullInput()
        {
            var result = CSVReader.Parse(null, ",", "#", 1);
            
            Assert.AreEqual(0, result.Headers.Length);
            Assert.AreEqual(0, result.DataRows.Length);
        }

        [Test]
        public void TestWhitespaceOnlyInput()
        {
            var result = CSVReader.Parse("   \n   \n   ", ",", "#", 1);
            
            Assert.AreEqual(0, result.Headers.Length);
        }

        [Test]
        public void TestOnlyHeaderNoData()
        {
            string csv = "a,b,c";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(0, result.DataRows.Length);
        }

        [Test]
        public void TestExtraColumnsInData()
        {
            // Data has more columns than header
            string csv = "a,b\n1,2,3,4";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual(2, result.DataRows[0].Length, "Row should be truncated to header length");
        }

        [Test]
        public void TestFewerColumnsInData()
        {
            // Data has fewer columns than header
            string csv = "a,b,c,d\n1,2";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(4, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual(4, result.DataRows[0].Length, "Row should be padded to header length");
        }

        [Test]
        public void TestConsecutiveDelimiters()
        {
            // Empty fields between delimiters
            string csv = "a,b,c\n,,";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("", result.DataRows[0][0]);
            Assert.AreEqual("", result.DataRows[0][1]);
            Assert.AreEqual("", result.DataRows[0][2]);
        }

        [Test]
        public void TestTrailingComma()
        {
            string csv = "a,b,c,\n1,2,3,";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(4, result.Headers.Length);
            Assert.AreEqual("", result.Headers[3], "Trailing comma creates empty header");
        }

        [Test]
        public void TestLeadingWhitespaceInCells()
        {
            string csv = "a, b , c\n 1 , 2 , 3 ";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            // Whitespace in unquoted fields is typically preserved
            Assert.AreEqual(3, result.Headers.Length);
        }

        [Test]
        public void TestMixedLineEndings()
        {
            // Mix of \r\n and \n
            string csv = "a,b\r\n1,2\n3,4";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
        }

        [Test]
        public void TestOnlyComments()
        {
            string csv = "# comment 1\n# comment 2\n# comment 3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(0, result.Headers.Length);
            Assert.AreEqual(0, result.DataRows.Length);
        }

        [Test]
        public void TestCommentsInterleaved()
        {
            string csv = "a,b\n1,2\n# comment\n3,4";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
            Assert.AreEqual("3", result.DataRows[1][0]);
        }

        [Test]
        public void TestDoubleSlashComment()
        {
            // // style comments should also be skipped
            string csv = "a,b\n1,2\n// comment\n3,4";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
        }

        #endregion

        #region Unicode and Special Characters

        [Test]
        public void TestUnicodeInData()
        {
            string csv = "name,greeting\nJohn,こんにちは\nMaria,Здравствуй";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("こんにちは", result.DataRows[0][1]);
            Assert.AreEqual("Здравствуй", result.DataRows[1][1]);
        }

        [Test]
        public void TestEmojisInData()
        {
            string csv = "name,emoji\nsmile,😀\nheart,❤️";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("😀", result.DataRows[0][1]);
        }

        [Test]
        public void TestSpecialCharactersUnquoted()
        {
            string csv = "a,b\n!@#$%,^&*()";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            // # in middle of field should be fine (not comment)
            Assert.AreEqual(1, result.DataRows.Length);
        }

        #endregion
    }
}
