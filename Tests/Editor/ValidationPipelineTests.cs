using NUnit.Framework;
using DataToScriptableObject.Editor;
using DataToScriptableObject;
using System.IO;
using System.Collections.Generic;

namespace DataToScriptableObject.Tests.Editor
{
    /// <summary>
    /// Comprehensive validation tests covering the full CSV and SQLite validation pipelines.
    /// Tests ensure validators correctly accept valid input, reject invalid input,
    /// and produce accurate warnings and error messages.
    /// </summary>
    [TestFixture]
    public class ValidationPipelineTests
    {
        private string tempDir;

        [SetUp]
        public void Setup()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "ValidationTests_" + System.Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        private GenerationSettings DefaultSettings()
        {
            return new GenerationSettings
            {
                SanitizeFieldNames = true,
                CommentPrefix = "#",
                HeaderRowCount = 3,
                NullToken = "null",
                ArrayDelimiter = ";"
            };
        }

        #region CSV Validator - Quick Validation

        [Test]
        public void CSV_QuickValidate_FileNotFound_ReturnsInvalidSource()
        {
            var path = Path.Combine(tempDir, "missing.csv");
            var validator = new CSVValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidSource, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("not found"));
        }

        [Test]
        public void CSV_QuickValidate_EmptyFile_ReturnsInvalidContent()
        {
            var path = Path.Combine(tempDir, "empty.csv");
            File.WriteAllText(path, "");
            var validator = new CSVValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidContent, result.Status);
        }

        [Test]
        public void CSV_QuickValidate_ValidFile_ReturnsValid()
        {
            var path = Path.Combine(tempDir, "valid.csv");
            File.WriteAllText(path, "id,name\n1,Sword");
            var validator = new CSVValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(SourceStatus.Valid, result.Status);
        }

        [Test]
        public void CSV_QuickValidate_UnexpectedExtension_WarnsButPasses()
        {
            var path = Path.Combine(tempDir, "data.json");
            File.WriteAllText(path, "id,name\n1,Sword");
            var validator = new CSVValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsTrue(result.IsValid);
            Assert.IsNotEmpty(result.Warnings);
            Assert.That(result.Warnings[0].message, Does.Contain(".json"));
        }

        [Test]
        public void CSV_QuickValidate_TxtExtension_Accepted()
        {
            var path = Path.Combine(tempDir, "data.txt");
            File.WriteAllText(path, "id,name\n1,Sword");
            var validator = new CSVValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Warnings);
        }

        #endregion

        #region CSV Validator - Full Validation

        [Test]
        public void CSV_FullValidate_ValidThreeRowHeader_ReturnsValidWithCounts()
        {
            var path = Path.Combine(tempDir, "full.csv");
            File.WriteAllText(path,
                "id,name,value\n" +
                "int,string,float\n" +
                "key,name,optional\n" +
                "1,Item1,10.5\n" +
                "2,Item2,20.3\n");

            var validator = new CSVValidator(path, DefaultSettings());
            SourceValidationResult result = null;
            validator.ValidateFull(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.TableCount);
            Assert.AreEqual(2, result.TotalRowCount);
            Assert.AreEqual(3, result.TotalColumnCount);
        }

        [Test]
        public void CSV_FullValidate_HeadersOnly_ValidWithZeroRows()
        {
            var path = Path.Combine(tempDir, "headersonly.csv");
            File.WriteAllText(path,
                "id,name,value\n" +
                "int,string,float\n" +
                "key,name,optional\n");

            var validator = new CSVValidator(path, DefaultSettings());
            SourceValidationResult result = null;
            validator.ValidateFull(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.TotalRowCount);
            Assert.That(result.Warnings[0].message, Does.Contain("no data rows"));
        }

        [Test]
        public void CSV_FullValidate_WithDirectives_SetsClassName()
        {
            var path = Path.Combine(tempDir, "directives.csv");
            File.WriteAllText(path,
                "#class:WeaponData\n" +
                "#database:WeaponDB\n" +
                "id,name\n" +
                "1,Sword\n");

            var settings = DefaultSettings();
            settings.HeaderRowCount = 1;
            var validator = new CSVValidator(path, settings);
            SourceValidationResult result = null;
            validator.ValidateFull(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.IsNotNull(result.TableNames);
            Assert.AreEqual("WeaponData", result.TableNames[0]);
        }

        [Test]
        public void CSV_FullValidate_SingleRowHeader_InfersTypes()
        {
            var path = Path.Combine(tempDir, "singlerow.csv");
            File.WriteAllText(path, "id,name,price\n1,Item,9.99\n2,Other,19.50\n");

            var settings = DefaultSettings();
            settings.HeaderRowCount = 1;
            var validator = new CSVValidator(path, settings);
            SourceValidationResult result = null;
            validator.ValidateFull(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(2, result.TotalRowCount);
        }

        #endregion

        #region CSV Validator - ExtractSchemas

        [Test]
        public void CSV_ExtractSchemas_ReturnsOneSchema()
        {
            var path = Path.Combine(tempDir, "extract.csv");
            File.WriteAllText(path,
                "id,name\n" +
                "int,string\n" +
                "key,name\n" +
                "1,Sword\n" +
                "2,Shield\n");

            var validator = new CSVValidator(path, DefaultSettings());
            var schemas = validator.ExtractSchemas();

            Assert.IsNotNull(schemas);
            Assert.AreEqual(1, schemas.Length);
            Assert.AreEqual(2, schemas[0].Columns.Length);
            Assert.AreEqual(2, schemas[0].Rows.Count);
        }

        [Test]
        public void CSV_ExtractSchemas_CachesResult()
        {
            var path = Path.Combine(tempDir, "cache.csv");
            File.WriteAllText(path, "id\n1\n2\n");

            var settings = DefaultSettings();
            settings.HeaderRowCount = 1;
            var validator = new CSVValidator(path, settings);

            var first = validator.ExtractSchemas();
            var second = validator.ExtractSchemas();

            Assert.AreSame(first, second, "Second call should return cached result");
        }

        [Test]
        public void CSV_ExtractSchemas_InvalidFile_ReturnsEmpty()
        {
            var path = Path.Combine(tempDir, "missing_extract.csv");
            var validator = new CSVValidator(path, DefaultSettings());
            var schemas = validator.ExtractSchemas();

            Assert.IsNotNull(schemas);
            Assert.AreEqual(0, schemas.Length);
        }

        [Test]
        public void CSV_ExtractSchemas_WithEnumColumn_PreservesEnumValues()
        {
            var path = Path.Combine(tempDir, "enum.csv");
            File.WriteAllText(path,
                "id,rarity\n" +
                "int,enum\n" +
                ",Common,Rare,Epic\n" +
                "1,Common\n");

            var validator = new CSVValidator(path, DefaultSettings());
            var schemas = validator.ExtractSchemas();

            Assert.AreEqual(1, schemas.Length);
            var rarityCol = schemas[0].Columns[1];
            Assert.AreEqual(ResolvedType.Enum, rarityCol.Type);
            Assert.IsNotNull(rarityCol.EnumValues);
            Assert.GreaterOrEqual(rarityCol.EnumValues.Length, 1);
        }

        [Test]
        public void CSV_ExtractSchemas_WithRangeAttribute_PreservesRange()
        {
            var path = Path.Combine(tempDir, "range.csv");
            File.WriteAllText(path,
                "id,health\n" +
                "int,int\n" +
                ",range(0;100)\n" +
                "1,75\n");

            var validator = new CSVValidator(path, DefaultSettings());
            var schemas = validator.ExtractSchemas();

            Assert.AreEqual(1, schemas.Length);
            var healthCol = schemas[0].Columns[1];
            Assert.IsTrue(healthCol.Attributes.ContainsKey("range"));
            Assert.AreEqual("0,100", healthCol.Attributes["range"]);
        }

        #endregion

        #region SQLite Validator - Quick Validation

        [Test]
        public void SQLite_QuickValidate_FileNotFound_ReturnsInvalidSource()
        {
            var path = Path.Combine(tempDir, "missing.db");
            var validator = new SQLiteValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidSource, result.Status);
        }

        [Test]
        public void SQLite_QuickValidate_EmptyFile_ReturnsInvalidFormat()
        {
            var path = Path.Combine(tempDir, "empty.db");
            File.WriteAllBytes(path, new byte[0]);
            var validator = new SQLiteValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidFormat, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("too small"));
        }

        [Test]
        public void SQLite_QuickValidate_TooSmall_ReturnsInvalidFormat()
        {
            var path = Path.Combine(tempDir, "small.db");
            File.WriteAllBytes(path, new byte[10]);
            var validator = new SQLiteValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidFormat, result.Status);
        }

        [Test]
        public void SQLite_QuickValidate_WrongHeader_ReturnsInvalidFormat()
        {
            var path = Path.Combine(tempDir, "wrong.db");
            File.WriteAllBytes(path, new byte[] {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
            });
            var validator = new SQLiteValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(SourceStatus.InvalidFormat, result.Status);
            Assert.That(result.ErrorMessage, Does.Contain("not a valid SQLite"));
        }

        [Test]
        public void SQLite_QuickValidate_ValidHeader_ReturnsValid()
        {
            var path = Path.Combine(tempDir, "valid.db");
            byte[] header = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");
            File.WriteAllBytes(path, header);
            var validator = new SQLiteValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(SourceStatus.Valid, result.Status);
        }

        [Test]
        public void SQLite_QuickValidate_AcceptedExtensions()
        {
            var extensions = new[] { ".db", ".sqlite", ".sqlite3", ".db3" };
            byte[] header = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");

            foreach (var ext in extensions)
            {
                var path = Path.Combine(tempDir, "test" + ext);
                File.WriteAllBytes(path, header);
                var validator = new SQLiteValidator(path, DefaultSettings());
                var result = validator.ValidateQuick();

                Assert.AreEqual(SourceStatus.Valid, result.Status,
                    $"Extension {ext} should be accepted");
            }
        }

        [Test]
        public void SQLite_QuickValidate_UnexpectedExtension_WarnsButPasses()
        {
            var path = Path.Combine(tempDir, "database.txt");
            byte[] header = System.Text.Encoding.ASCII.GetBytes("SQLite format 3\0");
            byte[] content = new byte[1024];
            System.Array.Copy(header, content, header.Length);
            File.WriteAllBytes(path, content);

            var validator = new SQLiteValidator(path, DefaultSettings());
            var result = validator.ValidateQuick();

            Assert.AreEqual(SourceStatus.Valid, result.Status);
            Assert.IsNotEmpty(result.Warnings);
            Assert.That(result.Warnings[0].message, Does.Contain(".txt"));
        }

        #endregion

        #region Google Sheets URL Validation

        [Test]
        public void GoogleSheets_ValidEditURL_ParsesCorrectly()
        {
            var result = GoogleSheetsURLParser.Parse(
                "https://docs.google.com/spreadsheets/d/ABC123/edit#gid=42");

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
            Assert.AreEqual("42", result.Gid);
        }

        [Test]
        public void GoogleSheets_ValidShareURL_ParsesCorrectly()
        {
            var result = GoogleSheetsURLParser.Parse(
                "https://docs.google.com/spreadsheets/d/ABC123/edit?usp=sharing&gid=99");

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123", result.SpreadsheetId);
            Assert.AreEqual("99", result.Gid);
        }

        [Test]
        public void GoogleSheets_RawID_ParsesCorrectly()
        {
            var result = GoogleSheetsURLParser.Parse("ABC123DEF456");

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ABC123DEF456", result.SpreadsheetId);
            Assert.AreEqual("0", result.Gid);
        }

        [Test]
        public void GoogleSheets_EmptyInput_ReturnsInvalid()
        {
            var result = GoogleSheetsURLParser.Parse("");
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void GoogleSheets_NullInput_ReturnsInvalid()
        {
            var result = GoogleSheetsURLParser.Parse(null);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void GoogleSheets_InvalidURL_ReturnsInvalid()
        {
            var result = GoogleSheetsURLParser.Parse("https://example.com/not-sheets");
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void GoogleSheets_NoGid_DefaultsToZero()
        {
            var result = GoogleSheetsURLParser.Parse(
                "https://docs.google.com/spreadsheets/d/ABC123/edit");

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("0", result.Gid);
        }

        [Test]
        public void GoogleSheets_BuildExportURL_FormatsCorrectly()
        {
            string url = GoogleSheetsURLParser.BuildExportURL("SHEET_ID", "42");
            Assert.AreEqual(
                "https://docs.google.com/spreadsheets/d/SHEET_ID/export?format=csv&gid=42",
                url);
        }

        #endregion

        #region Cross-Cutting Validation

        [Test]
        public void CSV_SchemaBuilder_StaticCall_Works()
        {
            // Regression test: SchemaBuilder is a static class.
            // CSVValidator must use SchemaBuilder.Build(), not new SchemaBuilder().BuildSchema().
            var path = Path.Combine(tempDir, "static.csv");
            File.WriteAllText(path, "id,name\nint,string\nkey,name\n1,Sword\n");

            var validator = new CSVValidator(path, DefaultSettings());
            SourceValidationResult result = null;
            validator.ValidateFull(r => result = r);

            Assert.IsNotNull(result, "ValidateFull should complete without exception");
            Assert.IsTrue(result.IsValid, "Valid CSV should pass full validation");
        }

        [Test]
        public void CSV_ExtractSchemas_ThenFullValidate_BothWork()
        {
            var path = Path.Combine(tempDir, "both.csv");
            File.WriteAllText(path, "id,name\nint,string\nkey,name\n1,A\n2,B\n");

            var validator = new CSVValidator(path, DefaultSettings());

            // Extract first
            var schemas = validator.ExtractSchemas();
            Assert.AreEqual(1, schemas.Length);

            // Then validate
            SourceValidationResult result = null;
            validator.ValidateFull(r => result = r);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void CSV_FullValidation_DuplicateHeaders_ProducesWarnings()
        {
            var path = Path.Combine(tempDir, "dup.csv");
            File.WriteAllText(path, "a,b,a\n1,2,3\n");

            var settings = DefaultSettings();
            settings.HeaderRowCount = 1;
            var validator = new CSVValidator(path, settings);
            var schemas = validator.ExtractSchemas();

            Assert.AreEqual(1, schemas.Length);
            Assert.AreEqual(3, schemas[0].Columns.Length);
            // The third column should have been renamed
            Assert.AreNotEqual(schemas[0].Columns[0].OriginalHeader,
                              schemas[0].Columns[2].OriginalHeader,
                              "Duplicate headers should be renamed");
        }

        [Test]
        public void CSV_FullValidation_EmptyHeaders_ProducesWarnings()
        {
            var path = Path.Combine(tempDir, "emptyheader.csv");
            File.WriteAllText(path, ",name,\n1,Item,3\n");

            var settings = DefaultSettings();
            settings.HeaderRowCount = 1;
            var validator = new CSVValidator(path, settings);
            var schemas = validator.ExtractSchemas();

            Assert.AreEqual(1, schemas.Length);
            Assert.AreEqual(3, schemas[0].Columns.Length);
            // Empty headers should be filled
            Assert.IsNotEmpty(schemas[0].Columns[0].OriginalHeader,
                "Empty header should be replaced with column_N");
            Assert.IsNotEmpty(schemas[0].Columns[2].OriginalHeader,
                "Empty header should be replaced with column_N");
        }

        #endregion
    }
}
