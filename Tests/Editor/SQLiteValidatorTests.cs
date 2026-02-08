using NUnit.Framework;
using DataToScriptableObject.Editor;
using DataToScriptableObject;
using System.IO;

namespace DataToScriptableObject.Tests.Editor
{
    [TestFixture]
    public class SQLiteValidatorTests
    {
        private string tempDir;

        [SetUp]
        public void Setup()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "SQLiteValidatorTests_" + System.Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void TestFileNotFound()
        {
            string nonExistentPath = Path.Combine(tempDir, "nonexistent.db");
            var settings = new GenerationSettings();
            var validator = new SQLiteValidator(nonExistentPath, settings);

            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidSource, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("not found"));
        }

        [Test]
        public void TestNotSQLiteFile()
        {
            string randomFilePath = Path.Combine(tempDir, "random.db");
            File.WriteAllBytes(randomFilePath, new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 
                                                             0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F });
            var settings = new GenerationSettings();
            var validator = new SQLiteValidator(randomFilePath, settings);

            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidFormat, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("not a valid SQLite"));
            Assert.That(result.ErrorMessage, Does.Contain("SQLite format 3"));
        }

        [Test]
        public void TestValidExtensions()
        {
            // Test that all common SQLite extensions are accepted
            var extensions = new[] { ".db", ".sqlite", ".sqlite3", ".db3" };
            
            foreach (var ext in extensions)
            {
                string filePath = Path.Combine(tempDir, "test" + ext);
                // Create a valid SQLite header
                byte[] header = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");
                File.WriteAllBytes(filePath, header);

                var settings = new GenerationSettings();
                var validator = new SQLiteValidator(filePath, settings);
                var result = validator.ValidateQuick();

                Assert.AreEqual(SourceStatus.Valid, result.Status, 
                    $"Extension {ext} should be accepted");
            }
        }

        [Test]
        public void TestUnexpectedExtension()
        {
            string txtFilePath = Path.Combine(tempDir, "database.txt");
            // Create a valid SQLite header even though it's .txt
            byte[] header = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");
            byte[] fullContent = new byte[1024];
            System.Array.Copy(header, fullContent, header.Length);
            File.WriteAllBytes(txtFilePath, fullContent);

            var settings = new GenerationSettings();
            var validator = new SQLiteValidator(txtFilePath, settings);
            var result = validator.ValidateQuick();

            // Should pass validation but with a warning
            Assert.AreEqual(SourceStatus.Valid, result.Status);
            Assert.IsNotEmpty(result.Warnings);
            var warning = result.Warnings.Find(w => w.message.Contains(".txt"));
            Assert.IsNotNull(warning, "Should have warning about unexpected extension");
        }

        [Test]
        public void TestEmptyFile()
        {
            string emptyFilePath = Path.Combine(tempDir, "empty.db");
            File.WriteAllBytes(emptyFilePath, new byte[0]);

            var settings = new GenerationSettings();
            var validator = new SQLiteValidator(emptyFilePath, settings);
            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidFormat, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("too small"));
        }

        [Test]
        public void TestTooSmallFile()
        {
            string smallFilePath = Path.Combine(tempDir, "small.db");
            File.WriteAllBytes(smallFilePath, new byte[10]);

            var settings = new GenerationSettings();
            var validator = new SQLiteValidator(smallFilePath, settings);
            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidFormat, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("too small"));
        }

        [Test]
        public void TestValidSampleDatabase()
        {
            // This test assumes the sample database exists
            string samplePath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "..", "..", "..", "Samples~", "SQLite", "game_data_example.db"
            );
            
            samplePath = Path.GetFullPath(samplePath);

            if (!File.Exists(samplePath))
            {
                Assert.Ignore($"Sample database not found at {samplePath}");
                return;
            }

            var settings = new GenerationSettings();
            var validator = new SQLiteValidator(samplePath, settings);

            var result = validator.ValidateQuick();

            Assert.IsTrue(result.IsValid, "Sample database should pass quick validation");
            Assert.AreEqual(SourceStatus.Valid, result.Status);
        }
    }
}
