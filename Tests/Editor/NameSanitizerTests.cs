using NUnit.Framework;
using DataToScriptableObject.Editor;

namespace DataToScriptableObject.Tests.Editor
{
    public class NameSanitizerTests
    {
        [Test]
        public void TestSpaceToCamel()
        {
            Assert.AreEqual("maxHealth", NameSanitizer.SanitizeFieldName("Max Health"));
        }

        [Test]
        public void TestSnakeToCamel()
        {
            Assert.AreEqual("playerName", NameSanitizer.SanitizeFieldName("player_name"));
        }

        [Test]
        public void TestAllCapsToLower()
        {
            Assert.AreEqual("id", NameSanitizer.SanitizeFieldName("ID"));
        }

        [Test]
        public void TestLeadingDigit()
        {
            Assert.AreEqual("_3dModel", NameSanitizer.SanitizeFieldName("3dModel"));
        }

        [Test]
        public void TestReservedWord()
        {
            Assert.AreEqual("class_", NameSanitizer.SanitizeFieldName("class"));
        }

        [Test]
        public void TestSpecialChars()
        {
            Assert.AreEqual("damage", NameSanitizer.SanitizeFieldName("damage!!!"));
        }

        [Test]
        public void TestHyphen()
        {
            Assert.AreEqual("firstName", NameSanitizer.SanitizeFieldName("first-name"));
        }

        [Test]
        public void TestEmpty()
        {
            Assert.AreEqual("field", NameSanitizer.SanitizeFieldName(""));
        }

        [Test]
        public void TestAlreadyCamel()
        {
            Assert.AreEqual("maxHealth", NameSanitizer.SanitizeFieldName("maxHealth"));
        }

        [Test]
        public void TestFileNameSpaces()
        {
            Assert.AreEqual("My_Item", NameSanitizer.SanitizeFileName("My Item"));
        }

        [Test]
        public void TestFileNameSpecialChars()
        {
            Assert.AreEqual("item1test", NameSanitizer.SanitizeFileName("item<1>:test"));
        }

        [Test]
        public void TestFileNameLength()
        {
            string longName = new string('a', 100);
            string result = NameSanitizer.SanitizeFileName(longName);
            Assert.LessOrEqual(result.Length, 64);
        }

        // ==================== DIAGNOSTIC TESTS ====================

        #region Field Name Sanitization Diagnostics

        [Test]
        public void TestMultipleSpaces()
        {
            Assert.AreEqual("maxHealth", NameSanitizer.SanitizeFieldName("Max   Health"));
        }

        [Test]
        public void TestTabCharacter()
        {
            Assert.AreEqual("firstName", NameSanitizer.SanitizeFieldName("first\tname"));
        }

        [Test]
        public void TestMixedDelimiters()
        {
            // Mix of spaces, underscores, hyphens
            Assert.AreEqual("myVariableName", NameSanitizer.SanitizeFieldName("my-variable_name"));
        }

        [Test]
        public void TestPascalCaseInput()
        {
            // Already PascalCase should become camelCase
            Assert.AreEqual("maxHealth", NameSanitizer.SanitizeFieldName("MaxHealth"));
        }

        [Test]
        public void TestAllCapsMultiWord()
        {
            // Multiple all-caps words
            Assert.AreEqual("httpUrl", NameSanitizer.SanitizeFieldName("HTTP URL"));
        }

        [Test]
        public void TestAcronymInMiddle()
        {
            // Acronym in middle of name
            Assert.AreEqual("getHttpResponse", NameSanitizer.SanitizeFieldName("get HTTP response"));
        }

        [Test]
        public void TestNumbersInMiddle()
        {
            Assert.AreEqual("item2Price", NameSanitizer.SanitizeFieldName("item2 price"));
        }

        [Test]
        public void TestNumbersAtEnd()
        {
            Assert.AreEqual("item123", NameSanitizer.SanitizeFieldName("item 123"));
        }

        [Test]
        public void TestTrailingSpecialChars()
        {
            Assert.AreEqual("name", NameSanitizer.SanitizeFieldName("name!!!???"));
        }

        [Test]
        public void TestLeadingSpecialChars()
        {
            Assert.AreEqual("name", NameSanitizer.SanitizeFieldName("!!!name"));
        }

        [Test]
        public void TestOnlyDigits()
        {
            // Only digits should get underscore prefix
            Assert.AreEqual("_123", NameSanitizer.SanitizeFieldName("123"));
        }

        [Test]
        public void TestSingleCharacter()
        {
            Assert.AreEqual("x", NameSanitizer.SanitizeFieldName("X"));
        }

        [Test]
        public void TestUnderscore()
        {
            Assert.AreEqual("fieldName", NameSanitizer.SanitizeFieldName("field_name"));
        }

        [Test]
        public void TestDoubleUnderscore()
        {
            Assert.AreEqual("fieldName", NameSanitizer.SanitizeFieldName("field__name"));
        }

        [Test]
        public void TestLeadingUnderscore()
        {
            // Leading underscore handling
            var result = NameSanitizer.SanitizeFieldName("_internal");
            Assert.IsTrue(result == "internal_" || result == "_internal" || result == "internal");
        }

        #endregion

        #region Reserved Words Diagnostics

        [Test]
        public void TestAllReservedWords()
        {
            // Test a sample of common reserved words
            var reservedWords = new[] { "class", "namespace", "int", "float", "bool", "string", "public", "private", "static", "void" };
            
            foreach (var word in reservedWords)
            {
                var result = NameSanitizer.SanitizeFieldName(word);
                Assert.AreEqual(word + "_", result, $"Reserved word '{word}' should be escaped");
            }
        }

        [Test]
        public void TestReservedWordMixedCase()
        {
            // "CLASS" should become lowercase "class" which is reserved
            var result = NameSanitizer.SanitizeFieldName("CLASS");
            Assert.AreEqual("class_", result);
        }

        [Test]
        public void TestNotReservedWord()
        {
            // "classes" is not a reserved word
            var result = NameSanitizer.SanitizeFieldName("classes");
            Assert.AreEqual("classes", result);
        }

        #endregion

        #region Class Name Sanitization Diagnostics

        [Test]
        public void TestClassNameFromSpace()
        {
            Assert.AreEqual("MyClass", NameSanitizer.SanitizeClassName("my class"));
        }

        [Test]
        public void TestClassNameFromSnake()
        {
            Assert.AreEqual("MyClassName", NameSanitizer.SanitizeClassName("my_class_name"));
        }

        [Test]
        public void TestClassNameEmpty()
        {
            Assert.AreEqual("GeneratedClass", NameSanitizer.SanitizeClassName(""));
        }

        [Test]
        public void TestClassNameNull()
        {
            Assert.AreEqual("GeneratedClass", NameSanitizer.SanitizeClassName(null));
        }

        [Test]
        public void TestClassNameLeadingDigit()
        {
            Assert.AreEqual("_3dModel", NameSanitizer.SanitizeClassName("3d model"));
        }

        [Test]
        public void TestClassNameReservedWord()
        {
            var result = NameSanitizer.SanitizeClassName("class");
            Assert.AreEqual("Class_", result);
        }

        #endregion

        #region File Name Sanitization Diagnostics

        [Test]
        public void TestFileNameEmpty()
        {
            Assert.AreEqual("unnamed", NameSanitizer.SanitizeFileName(""));
        }

        [Test]
        public void TestFileNameNull()
        {
            Assert.AreEqual("unnamed", NameSanitizer.SanitizeFileName(null));
        }

        [Test]
        public void TestFileNameSlashes()
        {
            var result = NameSanitizer.SanitizeFileName("path/to/file");
            Assert.IsFalse(result.Contains("/"));
        }

        [Test]
        public void TestFileNameBackslashes()
        {
            var result = NameSanitizer.SanitizeFileName("path\\to\\file");
            Assert.IsFalse(result.Contains("\\"));
        }

        [Test]
        public void TestFileNameColons()
        {
            var result = NameSanitizer.SanitizeFileName("C:file");
            Assert.IsFalse(result.Contains(":"));
        }

        [Test]
        public void TestFileNameQuotes()
        {
            var result = NameSanitizer.SanitizeFileName("file\"name");
            Assert.IsFalse(result.Contains("\""));
        }

        [Test]
        public void TestFileNameQuestionMark()
        {
            var result = NameSanitizer.SanitizeFileName("file?name");
            Assert.IsFalse(result.Contains("?"));
        }

        [Test]
        public void TestFileNameAsterisk()
        {
            var result = NameSanitizer.SanitizeFileName("file*name");
            Assert.IsFalse(result.Contains("*"));
        }

        [Test]
        public void TestFileNameAngles()
        {
            var result = NameSanitizer.SanitizeFileName("file<name>");
            Assert.IsFalse(result.Contains("<") || result.Contains(">"));
        }

        [Test]
        public void TestFileNamePipe()
        {
            var result = NameSanitizer.SanitizeFileName("file|name");
            Assert.IsFalse(result.Contains("|"));
        }

        [Test]
        public void TestFileNamePreservesExtension()
        {
            var result = NameSanitizer.SanitizeFileName("myfile.txt");
            Assert.AreEqual("myfile.txt", result);
        }

        [Test]
        public void TestFileNameUnicode()
        {
            // Unicode should generally be preserved
            var result = NameSanitizer.SanitizeFileName("日本語");
            Assert.AreEqual("日本語", result);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void TestOnlySpecialCharsField()
        {
            Assert.AreEqual("field", NameSanitizer.SanitizeFieldName("!@#$%"));
        }

        [Test]
        public void TestOnlySpecialCharsFile()
        {
            Assert.AreEqual("unnamed", NameSanitizer.SanitizeFileName("???"));
        }

        [Test]
        public void TestWhitespaceOnly()
        {
            Assert.AreEqual("field", NameSanitizer.SanitizeFieldName("   "));
        }

        [Test]
        public void TestVeryLongFieldName()
        {
            string longName = new string('a', 200);
            var result = NameSanitizer.SanitizeFieldName(longName);
            // Should handle gracefully (not throw)
            Assert.IsNotNull(result);
        }

        #endregion
    }
}
