using NUnit.Framework;
using DataToScriptableObject.Editor;
using DataToScriptableObject;

namespace DataToScriptableObject.Tests.Editor
{
    [TestFixture]
    public class SQLiteTypeMapperTests
    {
        [Test]
        public void TestInteger()
        {
            var result = SQLiteTypeMapper.Map("INTEGER", out string warning);
            Assert.AreEqual(ResolvedType.Int, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestIntShort()
        {
            var result = SQLiteTypeMapper.Map("INT", out string warning);
            Assert.AreEqual(ResolvedType.Int, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestBigint()
        {
            var result = SQLiteTypeMapper.Map("BIGINT", out string warning);
            Assert.AreEqual(ResolvedType.Long, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestBigintBeforeInt()
        {
            // Ensure BIGINT is matched before INT (priority order check)
            var result = SQLiteTypeMapper.Map("BIGINT", out string warning);
            Assert.AreEqual(ResolvedType.Long, result, "BIGINT should map to Long, not Int");
            Assert.IsNull(warning);
        }

        [Test]
        public void TestReal()
        {
            var result = SQLiteTypeMapper.Map("REAL", out string warning);
            Assert.AreEqual(ResolvedType.Float, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestFloat()
        {
            var result = SQLiteTypeMapper.Map("FLOAT", out string warning);
            Assert.AreEqual(ResolvedType.Float, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestDouble()
        {
            var result = SQLiteTypeMapper.Map("DOUBLE", out string warning);
            Assert.AreEqual(ResolvedType.Double, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestText()
        {
            var result = SQLiteTypeMapper.Map("TEXT", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestVarchar()
        {
            var result = SQLiteTypeMapper.Map("VARCHAR(255)", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestBoolean()
        {
            var result = SQLiteTypeMapper.Map("BOOLEAN", out string warning);
            Assert.AreEqual(ResolvedType.Bool, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestBool()
        {
            var result = SQLiteTypeMapper.Map("BOOL", out string warning);
            Assert.AreEqual(ResolvedType.Bool, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestBlob()
        {
            var result = SQLiteTypeMapper.Map("BLOB", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNotNull(warning);
            Assert.That(warning, Does.Contain("BLOB"));
            Assert.That(warning, Does.Contain("not supported"));
        }

        [Test]
        public void TestEmpty()
        {
            var result = SQLiteTypeMapper.Map("", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestNull()
        {
            var result = SQLiteTypeMapper.Map(null, out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestUnknown()
        {
            var result = SQLiteTypeMapper.Map("MONEY", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNotNull(warning);
            Assert.That(warning, Does.Contain("MONEY"));
            Assert.That(warning, Does.Contain("Unrecognized"));
        }

        [Test]
        public void TestCaseInsensitive()
        {
            var result = SQLiteTypeMapper.Map("integer", out string warning);
            Assert.AreEqual(ResolvedType.Int, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestParameterized()
        {
            var result = SQLiteTypeMapper.Map("NUMERIC(10,2)", out string warning);
            Assert.AreEqual(ResolvedType.Float, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestIntegerPrimaryKey()
        {
            var result = SQLiteTypeMapper.Map("INTEGER PRIMARY KEY", out string warning);
            Assert.AreEqual(ResolvedType.Int, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestChar()
        {
            var result = SQLiteTypeMapper.Map("CHAR(10)", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestNChar()
        {
            var result = SQLiteTypeMapper.Map("NCHAR(10)", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestNVarchar()
        {
            var result = SQLiteTypeMapper.Map("NVARCHAR(255)", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestClob()
        {
            var result = SQLiteTypeMapper.Map("CLOB", out string warning);
            Assert.AreEqual(ResolvedType.String, result);
            Assert.IsNull(warning);
        }

        [Test]
        public void TestNoIntInPoint()
        {
            // "POINT" contains "INT" but should not match because of the exclusion
            var result = SQLiteTypeMapper.Map("POINT", out string warning);
            Assert.AreNotEqual(ResolvedType.Int, result, "POINT should not be matched as INT");
            Assert.AreEqual(ResolvedType.String, result);
        }
    }
}
