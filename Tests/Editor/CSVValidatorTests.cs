using NUnit.Framework;
using DataToScriptableObject.Editor;
using DataToScriptableObject;
using System.IO;

namespace DataToScriptableObject.Tests.Editor
{
    [TestFixture]
    public class CSVValidatorTests
    {
        private string tempDir;

        [SetUp]
        public void Setup()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "CSVValidatorTests_" + System.Guid.NewGuid().ToString());
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
            string nonExistentPath = Path.Combine(tempDir, "nonexistent.csv");
            var settings = new GenerationSettings();
            var validator = new CSVValidator(nonExistentPath, settings);

            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidSource, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("not found"));
        }

        [Test]
        public void TestEmptyFile()
        {
            string emptyFilePath = Path.Combine(tempDir, "empty.csv");
            File.WriteAllText(emptyFilePath, "");

            var settings = new GenerationSettings();
            var validator = new CSVValidator(emptyFilePath, settings);

            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidContent, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("empty"));
        }

        [Test]
        public void TestValidCSVFile()
        {
            string csvFilePath = Path.Combine(tempDir, "valid.csv");
            File.WriteAllText(csvFilePath, 
                "id,name,value\n" +
                "int,string,float\n" +
                "key,name,optional\n" +
                "1,Item1,10.5\n" +
                "2,Item2,20.3\n");

            var settings = new GenerationSettings();
            var validator = new CSVValidator(csvFilePath, settings);

            var result = validator.ValidateQuick();
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(SourceStatus.Valid, result.Status);
        }

        [Test]
        public void TestValidCSVFileFullValidation()
        {
            string csvFilePath = Path.Combine(tempDir, "valid_full.csv");
            File.WriteAllText(csvFilePath,
                "id,name,value\n" +
                "int,string,float\n" +
                "key,name,optional\n" +
                "1,Item1,10.5\n" +
                "2,Item2,20.3\n");

            var settings = new GenerationSettings();
            var validator = new CSVValidator(csvFilePath, settings);

            SourceValidationResult result = null;
            validator.ValidateFull(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(SourceStatus.Valid, result.Status);
            Assert.AreEqual(1, result.TableCount);
            Assert.AreEqual(2, result.TotalRowCount);
            Assert.AreEqual(3, result.TotalColumnCount);
        }

        [Test]
        public void TestCSVWithNoDataRows()
        {
            string csvFilePath = Path.Combine(tempDir, "nodata.csv");
            File.WriteAllText(csvFilePath,
                "id,name,value\n" +
                "int,string,float\n" +
                "key,name,optional\n");

            var settings = new GenerationSettings();
            var validator = new CSVValidator(csvFilePath, settings);

            SourceValidationResult result = null;
            validator.ValidateFull(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(SourceStatus.Valid, result.Status);
            Assert.AreEqual(0, result.TotalRowCount);
            Assert.IsNotEmpty(result.Warnings);
            Assert.That(result.Warnings[0].message, Does.Contain("no data rows"));
        }

        [Test]
        public void TestUnexpectedExtension()
        {
            string txtFilePath = Path.Combine(tempDir, "data.json");
            File.WriteAllText(txtFilePath,
                "id,name,value\n" +
                "int,string,float\n" +
                "key,name,optional\n" +
                "1,Item1,10.5\n");

            var settings = new GenerationSettings();
            var validator = new CSVValidator(txtFilePath, settings);

            var result = validator.ValidateQuick();

            // Should pass validation but with a warning
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(SourceStatus.Valid, result.Status);
            Assert.IsNotEmpty(result.Warnings);
            var warning = result.Warnings.Find(w => w.message.Contains(".json"));
            Assert.IsNotNull(warning, "Should have warning about unexpected extension");
        }

        [Test]
        public void TestExtractSchemas()
        {
            string csvFilePath = Path.Combine(tempDir, "extract.csv");
            File.WriteAllText(csvFilePath,
                "id,name,value\n" +
                "int,string,float\n" +
                "key,name,optional\n" +
                "1,Item1,10.5\n" +
                "2,Item2,20.3\n");

            var settings = new GenerationSettings();
            var validator = new CSVValidator(csvFilePath, settings);

            var schemas = validator.ExtractSchemas();

            Assert.IsNotNull(schemas);
            Assert.AreEqual(1, schemas.Length);
            Assert.AreEqual(3, schemas[0].Columns.Length);
            Assert.AreEqual(2, schemas[0].Rows.Count);
        }

        [Test]
        public void TestValidSampleCSV()
        {
            // This test assumes the sample CSV exists
            string samplePath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "..", "..", "..", "Samples~", "CSV", "items_example.csv"
            );

            samplePath = Path.GetFullPath(samplePath);

            if (!File.Exists(samplePath))
            {
                Assert.Ignore($"Sample CSV not found at {samplePath}");
                return;
            }

            var settings = new GenerationSettings();
            var validator = new CSVValidator(samplePath, settings);

            var result = validator.ValidateQuick();

            Assert.IsTrue(result.IsValid, "Sample CSV should pass quick validation");
            Assert.AreEqual(SourceStatus.Valid, result.Status);
        }
    }
}
