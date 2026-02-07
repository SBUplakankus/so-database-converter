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
    }
}
