using NUnit.Framework;
using DataToScriptableObject;
using DataToScriptableObject.Editor;

namespace DataToScriptableObject.Tests.Editor
{
    /// <summary>
    /// Comprehensive edge case tests for CSV parsing with TextFieldParser.
    /// These tests cover complex real-world scenarios that users might encounter
    /// when creating ScriptableObject databases.
    /// </summary>
    public class CSVReaderEdgeCaseTests
    {
        #region Complex Real-World Data Scenarios

        [Test]
        public void TestURLsInData()
        {
            // URLs with query parameters and special characters
            string csv = "name,website\nGoogle,https://google.com/search?q=test&lang=en\nGitHub,https://github.com/user/repo?tab=readme";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("https://google.com/search?q=test&lang=en", result.DataRows[0][1]);
            Assert.AreEqual("https://github.com/user/repo?tab=readme", result.DataRows[1][1]);
        }

        [Test]
        public void TestJSONLikeDataInQuotes()
        {
            // JSON-like strings that might be stored in database
            string csv = "id,metadata\n1,\"{\"\"type\"\":\"\"weapon\"\",\"\"damage\"\":10}\"\n2,\"{\"\"type\"\":\"\"armor\"\",\"\"defense\"\":5}\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            // The JSON string should be preserved with escaped quotes
            Assert.IsTrue(result.DataRows[0][1].Contains("type"));
            Assert.IsTrue(result.DataRows[0][1].Contains("weapon"));
        }

        [Test]
        public void TestFilePathsWindows()
        {
            // Windows file paths with backslashes
            string csv = "name,path\nTexture,C:\\\\Users\\\\Player\\\\textures\\\\sword.png\nModel,D:\\\\Assets\\\\Models\\\\character.fbx";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][1].Contains("\\"));
        }

        [Test]
        public void TestFilePathsUnix()
        {
            // Unix file paths
            string csv = "name,path\nTexture,/home/user/assets/textures/sword.png\nModel,/var/data/models/character.fbx";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("/home/user/assets/textures/sword.png", result.DataRows[0][1]);
        }

        [Test]
        public void TestSQLStringsInQuotes()
        {
            // SQL-like strings that might be stored
            string csv = "name,query\nGetUser,\"SELECT * FROM users WHERE id = 1\"\nGetItems,\"SELECT name, price FROM items WHERE category = 'weapon'\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][1].Contains("SELECT"));
            Assert.IsTrue(result.DataRows[1][1].Contains("'weapon'"));
        }

        [Test]
        public void TestEmailAddresses()
        {
            // Email addresses with various formats
            string csv = "name,email\nJohn,john.doe+tag@example.com\nJane,jane_doe@sub-domain.example.co.uk";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("john.doe+tag@example.com", result.DataRows[0][1]);
            Assert.AreEqual("jane_doe@sub-domain.example.co.uk", result.DataRows[1][1]);
        }

        [Test]
        public void TestMarkdownContent()
        {
            // Markdown-formatted text
            string csv = "name,description\nItem1,\"# Header\\n## Subheader\\n- List item 1\\n- List item 2\"\nItem2,\"**bold** and *italic*\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][1].Contains("# Header"));
        }

        [Test]
        public void TestHTMLContent()
        {
            // HTML content in quoted fields
            string csv = "name,html\nButton,\"<button class='btn'>Click Me</button>\"\nLink,\"<a href='http://example.com'>Example</a>\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][1].Contains("<button"));
            Assert.IsTrue(result.DataRows[1][1].Contains("href"));
        }

        [Test]
        public void TestRegexPatterns()
        {
            // Regular expressions
            string csv = "name,pattern\nEmail,^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$\nPhone,^\\+?1?\\d{9,15}$";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][1].Contains("@"));
            Assert.IsTrue(result.DataRows[1][1].Contains("\\d"));
        }

        [Test]
        public void TestBase64EncodedData()
        {
            // Base64 encoded strings (common for binary data in databases)
            string csv = "id,data\n1,SGVsbG8gV29ybGQh\n2,VGhpcyBpcyBhIHRlc3Q=";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("SGVsbG8gV29ybGQh", result.DataRows[0][1]);
            Assert.AreEqual("VGhpcyBpcyBhIHRlc3Q=", result.DataRows[1][1]);
        }

        [Test]
        public void TestCurrencyValues()
        {
            // Currency values with various symbols
            string csv = "item,price_usd,price_eur,price_gbp\nSword,$29.99,€25.50,£22.00\nShield,$49.99,€42.50,£38.00";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("$29.99", result.DataRows[0][1]);
            Assert.AreEqual("€25.50", result.DataRows[0][2]);
            Assert.AreEqual("£22.00", result.DataRows[0][3]);
        }

        [Test]
        public void TestColorHexCodes()
        {
            // Hex color codes (starting with #, but not directives)
            string csv = "name,primary,secondary\nTheme1,\"#FF5733\",\"#33FF57\"\nTheme2,\"#3357FF\",\"#F333FF\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            // When quoted, # is part of data, not a comment
            Assert.AreEqual("#FF5733", result.DataRows[0][1]);
            Assert.AreEqual("#33FF57", result.DataRows[0][2]);
        }

        [Test]
        public void TestVersionNumbers()
        {
            // Software version numbers
            string csv = "software,version\nUnity,2022.3.15f1\nBlender,4.0.2\nPhotoshop,2024.1.0";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("2022.3.15f1", result.DataRows[0][1]);
        }

        [Test]
        public void TestTimestampsAndDates()
        {
            // Various date/time formats
            string csv = "event,timestamp\nLogin,2024-02-07T15:30:00Z\nLogout,2024-02-07 15:45:00\nAction,02/07/2024 3:30 PM";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("2024-02-07T15:30:00Z", result.DataRows[0][1]);
        }

        [Test]
        public void TestFormulasAndExpressions()
        {
            // Mathematical expressions that might be stored
            string csv = "name,formula\nDamage,\"baseDamage * (1 + strengthBonus / 100)\"\nHealth,\"maxHealth - currentDamage\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][1].Contains("baseDamage"));
        }

        #endregion

        #region Malformed and Edge Case Handling

        [Test]
        public void TestConsecutiveDelimiters()
        {
            // Consecutive delimiters create empty fields
            string csv = "a,b,c\n1,,3\n,2,\n,,";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
            Assert.AreEqual("", result.DataRows[0][1]);
            Assert.AreEqual("3", result.DataRows[0][2]);
            Assert.AreEqual("", result.DataRows[1][0]);
            Assert.AreEqual("2", result.DataRows[1][1]);
        }

        [Test]
        public void TestTrailingDelimiters()
        {
            // Trailing delimiters at end of rows
            string csv = "a,b,c\n1,2,3,\n4,5,6,";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            // Should handle gracefully - either ignore or treat as extra column
            Assert.IsTrue(result.DataRows[0].Length >= 3);
        }

        [Test]
        public void TestLeadingDelimiters()
        {
            // Leading delimiters at start of rows
            string csv = "a,b,c\n,1,2,3\n,4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("", result.DataRows[0][0]);
            Assert.AreEqual("1", result.DataRows[0][1]);
        }

        [Test]
        public void TestUnbalancedQuotes()
        {
            // Missing closing quote - TextFieldParser should handle or throw MalformedLineException
            string csv = "a,b\n\"unclosed,value";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            // Should either parse with best effort or skip malformed line
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestQuotesInMiddleOfUnquotedField()
        {
            // Quotes in middle of unquoted field
            string csv = "a,b\nval\"ue,test";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            // Should treat quote as literal character when not at field start
        }

        [Test]
        public void TestEmptyLinesAtStart()
        {
            // Empty lines before headers
            string csv = "\n\n\na,b,c\n1,2,3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
        }

        [Test]
        public void TestEmptyLinesAtEnd()
        {
            // Empty lines after data
            string csv = "a,b,c\n1,2,3\n\n\n";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
        }

        [Test]
        public void TestMixedLineEndingsSingleFile()
        {
            // Mix of \n, \r\n in same file
            string csv = "a,b,c\r\n1,2,3\n4,5,6\r\n7,8,9";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
            Assert.AreEqual("4", result.DataRows[1][0]);
            Assert.AreEqual("7", result.DataRows[2][0]);
        }

        [Test]
        public void TestOnlyWhitespaceFields()
        {
            // Fields containing only whitespace
            string csv = "a,b,c\n   ,\t,  ";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            // TrimWhiteSpace is false, so whitespace should be preserved
            Assert.AreEqual("   ", result.DataRows[0][0]);
            Assert.AreEqual("\t", result.DataRows[0][1]);
        }

        [Test]
        public void TestVeryLongFields()
        {
            // Fields with very long content (1000+ characters)
            string longText = new string('x', 1000);
            string csv = $"a,b\n{longText},short";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual(1000, result.DataRows[0][0].Length);
            Assert.AreEqual("short", result.DataRows[0][1]);
        }

        #endregion

        #region Multi-Row Header Edge Cases

        [Test]
        public void TestThreeRowHeaderWithSpecialChars()
        {
            // Three-row header with special characters in type hints
            string csv = "name,damage,armor\nstring,int32,float\n,>0,>=0.0\nSword,50,0.5";
            var result = CSVReader.Parse(csv, ",", "#", 2);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.IsNotNull(result.TypeHints);
            Assert.AreEqual("int32", result.TypeHints[1]);
        }

        [Test]
        public void TestThreeRowHeaderWithEnumFlags()
        {
            // Enum flags spread across multiple columns
            string csv = "name,type,rarity\nstring,enum,enum\n,Fire|Water|Earth,Common|Rare|Epic|Legendary\nSword,Fire,Rare";
            var result = CSVReader.Parse(csv, ",", "#", 3);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.IsNotNull(result.Flags);
            // Flags row should not be normalized
            Assert.IsTrue(result.Flags.Length >= 3);
        }

        [Test]
        public void TestTwoRowHeaderWithEmptyTypeHints()
        {
            // Some columns have type hints, others don't
            string csv = "id,name,value,description\nint,,float,\n1,test,3.14,desc";
            var result = CSVReader.Parse(csv, ",", "#", 2);
            
            Assert.AreEqual(4, result.Headers.Length);
            Assert.IsNotNull(result.TypeHints);
            Assert.AreEqual("int", result.TypeHints[0]);
            Assert.AreEqual("", result.TypeHints[1]);
            Assert.AreEqual("float", result.TypeHints[2]);
        }

        [Test]
        public void TestHeadersWithQuotedCommas()
        {
            // Header names containing commas (quoted)
            string csv = "\"name, title\",value\nJohn,100";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Headers.Length);
            Assert.AreEqual("name, title", result.Headers[0]);
        }

        [Test]
        public void TestHeadersWithLeadingTrailingSpaces()
        {
            // Headers with whitespace (should be preserved if not trimmed)
            string csv = " name , value \ntest,123";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Headers.Length);
            // TrimWhiteSpace is false, so spaces should be preserved
            Assert.AreEqual(" name ", result.Headers[0]);
        }

        [Test]
        public void TestInconsistentColumnCount()
        {
            // Rows with different numbers of columns
            string csv = "a,b,c\n1,2\n3,4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(2, result.DataRows.Length);
            // First row should be padded with empty strings
            Assert.AreEqual(3, result.DataRows[0].Length);
            // Second row should be truncated to header count
            Assert.AreEqual(3, result.DataRows[1].Length);
        }

        [Test]
        public void TestZeroWidthColumns()
        {
            // Headers followed immediately by delimiter (empty header names)
            string csv = ",a,,b,\n1,2,3,4,5";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(5, result.Headers.Length);
            Assert.AreEqual("", result.Headers[0]);
            Assert.AreEqual("a", result.Headers[1]);
            Assert.AreEqual("", result.Headers[2]);
        }

        [Test]
        public void TestAllEmptyHeaders()
        {
            // All headers are empty
            string csv = ",,\n1,2,3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
        }

        #endregion

        #region Directive Edge Cases

        [Test]
        public void TestDirectiveWithWhitespaceVariations()
        {
            // Directives with various whitespace
            string csv = "#class:  MyClass  \n  #namespace:MyNamespace\na,b\n1,2";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Directives.Length);
            Assert.IsTrue(result.Directives[0].Contains("MyClass"));
        }

        [Test]
        public void TestDirectiveWithSpecialCharacters()
        {
            // Directives with special characters in values
            string csv = "#class:My.Special-Class_Name123\n#database:DB-Prod_v2\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Directives.Length);
            Assert.IsTrue(result.Directives[0].Contains("My.Special-Class_Name123"));
            Assert.IsTrue(result.Directives[1].Contains("DB-Prod_v2"));
        }

        [Test]
        public void TestDirectivesWithEmptyLines()
        {
            // Directives separated by empty lines
            string csv = "#class:Test\n\n#namespace:MyNS\n\na,b\n1,2";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Directives.Length);
        }

        [Test]
        public void TestDirectiveAfterComment()
        {
            // Comment followed by directive
            string csv = "# This is a comment\n#class:MyClass\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            // Comment should be skipped, directive should be preserved
            Assert.AreEqual(1, result.Directives.Length);
            Assert.IsTrue(result.Directives[0].Contains("MyClass"));
        }

        [Test]
        public void TestSlashSlashCommentWithDirective()
        {
            // // comment followed by directive
            string csv = "// Comment line\n#class:Test\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.Directives.Length);
        }

        [Test]
        public void TestDirectivesCaseSensitive()
        {
            // Directives should be case-sensitive
            string csv = "#Class:Test\n#CLASS:Test2\na\n1";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            // Only lowercase #class: should be recognized as directive
            Assert.AreEqual(0, result.Directives.Length);
        }

        #endregion

        #region Stress and Performance Scenarios

        [Test]
        public void TestManyColumns()
        {
            // CSV with many columns (100+)
            var headers = new List<string>();
            var values = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                headers.Add($"col{i}");
                values.Add($"val{i}");
            }
            string csv = string.Join(",", headers) + "\n" + string.Join(",", values);
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(100, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("val50", result.DataRows[0][50]);
        }

        [Test]
        public void TestManyRows()
        {
            // CSV with many rows (1000+)
            var lines = new List<string> { "a,b,c" };
            for (int i = 0; i < 1000; i++)
            {
                lines.Add($"{i},value{i},data{i}");
            }
            string csv = string.Join("\n", lines);
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1000, result.DataRows.Length);
            Assert.AreEqual("500", result.DataRows[500][0]);
            Assert.AreEqual("value999", result.DataRows[999][1]);
        }

        [Test]
        public void TestDeeplyNestedQuotes()
        {
            // Multiple levels of quote escaping
            string csv = "a\n\"\"\"\"\"test\"\"\"\"\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            // Each "" becomes one "
            Assert.IsTrue(result.DataRows[0][0].Contains("\""));
        }

        [Test]
        public void TestLargeQuotedField()
        {
            // Single quoted field with lots of content
            string largeContent = new string('x', 5000);
            string csv = $"a\n\"{largeContent}\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual(5000, result.DataRows[0][0].Length);
        }

        [Test]
        public void TestManyMultilineFields()
        {
            // Multiple rows with multiline quoted fields
            string csv = "a,b\n\"line1\nline2\",\"line3\nline4\"\n\"line5\nline6\",\"line7\nline8\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][0].Contains("\n"));
            Assert.IsTrue(result.DataRows[1][1].Contains("\n"));
        }

        #endregion

        #region BOM and Encoding

        [Test]
        public void TestUTF8BOM()
        {
            // UTF-8 BOM should be stripped
            string csv = "\uFEFFa,b,c\n1,2,3";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual("a", result.Headers[0]); // Not "\uFEFFa"
        }

        [Test]
        public void TestZeroWidthCharacters()
        {
            // Zero-width characters (invisible but present)
            string csv = "a\u200B,b,c\n1,2,3"; // \u200B is zero-width space
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            // Should preserve the zero-width character
            Assert.IsTrue(result.Headers[0].Contains("\u200B"));
        }

        [Test]
        public void TestRightToLeftCharacters()
        {
            // Right-to-left text (Arabic, Hebrew)
            string csv = "name,arabic,hebrew\nTest,مرحبا,שלום";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("مرحبا", result.DataRows[0][1]);
            Assert.AreEqual("שלום", result.DataRows[0][2]);
        }

        #endregion

        #region Alternative Delimiter Scenarios

        [Test]
        public void TestPipeDelimiterWithQuotes()
        {
            // Pipe delimiter with quoted fields
            string csv = "a|b|c\n\"value|with|pipes\"|normal|\"another\"";
            var result = CSVReader.Parse(csv, "|", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("value|with|pipes", result.DataRows[0][0]);
        }

        [Test]
        public void TestSemicolonWithCommasInData()
        {
            // Semicolon delimiter with commas in data
            string csv = "a;b;c\n1,000;2,000;3,000";
            var result = CSVReader.Parse(csv, ";", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual("1,000", result.DataRows[0][0]);
            Assert.AreEqual("2,000", result.DataRows[0][1]);
        }

        [Test]
        public void TestTabWithSpacesInData()
        {
            // Tab delimiter with spaces in data
            string csv = "a\tb\tc\nvalue with spaces\tmore  spaces\tend";
            var result = CSVReader.Parse(csv, "\t", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual("value with spaces", result.DataRows[0][0]);
            Assert.AreEqual("more  spaces", result.DataRows[0][1]);
        }

        #endregion
    }
}
