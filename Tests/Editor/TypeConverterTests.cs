using NUnit.Framework;
using DataToScriptableObject.Editor;
using UnityEngine;

namespace DataToScriptableObject.Tests.Editor
{
    public class TypeConverterTests
    {
        [Test]
        public void TestToInt()
        {
            Assert.AreEqual(42, TypeConverters.ToInt("42"));
        }

        [Test]
        public void TestToIntEmpty()
        {
            Assert.AreEqual(0, TypeConverters.ToInt(""));
        }

        [Test]
        public void TestToIntInvalid()
        {
            Assert.AreEqual(0, TypeConverters.ToInt("abc"));
        }

        [Test]
        public void TestToFloat()
        {
            Assert.AreEqual(3.14f, TypeConverters.ToFloat("3.14"), 0.001f);
        }

        [Test]
        public void TestToFloatInvariantCulture()
        {
            Assert.AreEqual(3.14f, TypeConverters.ToFloat("3.14"), 0.001f);
        }

        [Test]
        public void TestToBoolTrue()
        {
            Assert.IsTrue(TypeConverters.ToBool("true"));
            Assert.IsTrue(TypeConverters.ToBool("True"));
            Assert.IsTrue(TypeConverters.ToBool("TRUE"));
            Assert.IsTrue(TypeConverters.ToBool("yes"));
            Assert.IsTrue(TypeConverters.ToBool("1"));
        }

        [Test]
        public void TestToBoolFalse()
        {
            Assert.IsFalse(TypeConverters.ToBool("false"));
            Assert.IsFalse(TypeConverters.ToBool("False"));
            Assert.IsFalse(TypeConverters.ToBool("no"));
            Assert.IsFalse(TypeConverters.ToBool("0"));
        }

        [Test]
        public void TestToVector2()
        {
            var result = TypeConverters.ToVector2("(1.5,2.5)");
            Assert.AreEqual(1.5f, result.x, 0.001f);
            Assert.AreEqual(2.5f, result.y, 0.001f);
        }

        [Test]
        public void TestToVector2NoParen()
        {
            var result = TypeConverters.ToVector2("1.5,2.5");
            Assert.AreEqual(1.5f, result.x, 0.001f);
            Assert.AreEqual(2.5f, result.y, 0.001f);
        }

        [Test]
        public void TestToVector3()
        {
            var result = TypeConverters.ToVector3("(1,2,3)");
            Assert.AreEqual(1f, result.x, 0.001f);
            Assert.AreEqual(2f, result.y, 0.001f);
            Assert.AreEqual(3f, result.z, 0.001f);
        }

        [Test]
        public void TestToColorHex()
        {
            var result = TypeConverters.ToColor("#FF5500");
            Assert.AreEqual(1f, result.r, 0.01f);
            Assert.AreEqual(0.333f, result.g, 0.01f);
            Assert.AreEqual(0f, result.b, 0.01f);
            Assert.AreEqual(1f, result.a, 0.01f);
        }

        [Test]
        public void TestToColorRGBA()
        {
            var result = TypeConverters.ToColor("255,85,0,255");
            Assert.AreEqual(1f, result.r, 0.01f);
            Assert.AreEqual(0.333f, result.g, 0.01f);
            Assert.AreEqual(0f, result.b, 0.01f);
            Assert.AreEqual(1f, result.a, 0.01f);
        }

        [Test]
        public void TestToStringNull()
        {
            Assert.IsNull(TypeConverters.ToString("null", "null"));
        }

        [Test]
        public void TestToStringEmpty()
        {
            Assert.AreEqual("", TypeConverters.ToString("", "null"));
        }

        [Test]
        public void TestToArrayInt()
        {
            var result = TypeConverters.ToArray("1;2;3", ";", TypeConverters.ToInt);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
        }

        [Test]
        public void TestToArrayString()
        {
            var result = TypeConverters.ToArray("a;b;c", ";", s => TypeConverters.ToString(s, "null"));
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("a", result[0]);
            Assert.AreEqual("b", result[1]);
            Assert.AreEqual("c", result[2]);
        }

        [Test]
        public void TestNullHandling()
        {
            Assert.AreEqual(0, TypeConverters.ToInt(null));
            Assert.AreEqual(0f, TypeConverters.ToFloat(null));
            Assert.IsFalse(TypeConverters.ToBool(null));
            Assert.AreEqual(Vector2.zero, TypeConverters.ToVector2(null));
            Assert.AreEqual(Vector3.zero, TypeConverters.ToVector3(null));
            Assert.AreEqual(Color.white, TypeConverters.ToColor(null));
        }

        // ==================== DIAGNOSTIC TESTS ====================

        #region Integer Conversion Diagnostics

        [Test]
        public void TestToIntNegative()
        {
            Assert.AreEqual(-42, TypeConverters.ToInt("-42"));
        }

        [Test]
        public void TestToIntLarge()
        {
            Assert.AreEqual(2000000000, TypeConverters.ToInt("2000000000"));
        }

        [Test]
        public void TestToIntWhitespace()
        {
            Assert.AreEqual(0, TypeConverters.ToInt("  42  "));
        }

        [Test]
        public void TestToIntWithDecimal()
        {
            // Should fail gracefully
            Assert.AreEqual(0, TypeConverters.ToInt("42.5"));
        }

        #endregion

        #region Float Conversion Diagnostics

        [Test]
        public void TestToFloatNegative()
        {
            Assert.AreEqual(-3.14f, TypeConverters.ToFloat("-3.14"), 0.001f);
        }

        [Test]
        public void TestToFloatVerySmall()
        {
            Assert.AreEqual(0.0001f, TypeConverters.ToFloat("0.0001"), 0.00001f);
        }

        [Test]
        public void TestToFloatVeryLarge()
        {
            Assert.AreEqual(1000000.0f, TypeConverters.ToFloat("1000000"), 0.1f);
        }

        [Test]
        public void TestToFloatScientific()
        {
            Assert.AreEqual(1500f, TypeConverters.ToFloat("1.5e3"), 0.1f);
        }

        [Test]
        public void TestToFloatEmpty()
        {
            Assert.AreEqual(0f, TypeConverters.ToFloat(""));
        }

        [Test]
        public void TestToFloatInvalid()
        {
            Assert.AreEqual(0f, TypeConverters.ToFloat("abc"));
        }

        #endregion

        #region Boolean Conversion Diagnostics

        [Test]
        public void TestToBoolYes()
        {
            Assert.IsTrue(TypeConverters.ToBool("yes"));
            Assert.IsTrue(TypeConverters.ToBool("YES"));
            Assert.IsTrue(TypeConverters.ToBool("Yes"));
        }

        [Test]
        public void TestToBoolNo()
        {
            Assert.IsFalse(TypeConverters.ToBool("no"));
            Assert.IsFalse(TypeConverters.ToBool("NO"));
            Assert.IsFalse(TypeConverters.ToBool("No"));
        }

        [Test]
        public void TestToBoolEmpty()
        {
            Assert.IsFalse(TypeConverters.ToBool(""));
        }

        [Test]
        public void TestToBoolInvalid()
        {
            Assert.IsFalse(TypeConverters.ToBool("maybe"));
        }

        #endregion

        #region Vector2 Conversion Diagnostics

        [Test]
        public void TestToVector2Negative()
        {
            var result = TypeConverters.ToVector2("(-1.5,-2.5)");
            Assert.AreEqual(-1.5f, result.x, 0.001f);
            Assert.AreEqual(-2.5f, result.y, 0.001f);
        }

        [Test]
        public void TestToVector2Integers()
        {
            var result = TypeConverters.ToVector2("(10,20)");
            Assert.AreEqual(10f, result.x, 0.001f);
            Assert.AreEqual(20f, result.y, 0.001f);
        }

        [Test]
        public void TestToVector2WithSpaces()
        {
            var result = TypeConverters.ToVector2("( 1.5 , 2.5 )");
            Assert.AreEqual(1.5f, result.x, 0.001f);
            Assert.AreEqual(2.5f, result.y, 0.001f);
        }

        [Test]
        public void TestToVector2Empty()
        {
            var result = TypeConverters.ToVector2("");
            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void TestToVector2Invalid()
        {
            var result = TypeConverters.ToVector2("not a vector");
            Assert.AreEqual(Vector2.zero, result);
        }

        #endregion

        #region Vector3 Conversion Diagnostics

        [Test]
        public void TestToVector3NoParen()
        {
            var result = TypeConverters.ToVector3("1,2,3");
            Assert.AreEqual(1f, result.x, 0.001f);
            Assert.AreEqual(2f, result.y, 0.001f);
            Assert.AreEqual(3f, result.z, 0.001f);
        }

        [Test]
        public void TestToVector3Negative()
        {
            var result = TypeConverters.ToVector3("(-1,-2,-3)");
            Assert.AreEqual(-1f, result.x, 0.001f);
            Assert.AreEqual(-2f, result.y, 0.001f);
            Assert.AreEqual(-3f, result.z, 0.001f);
        }

        [Test]
        public void TestToVector3Empty()
        {
            var result = TypeConverters.ToVector3("");
            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void TestToVector3Invalid()
        {
            var result = TypeConverters.ToVector3("not a vector");
            Assert.AreEqual(Vector3.zero, result);
        }

        #endregion

        #region Color Conversion Diagnostics

        [Test]
        public void TestToColorHexWithAlpha()
        {
            var result = TypeConverters.ToColor("#FF550080");
            Assert.AreEqual(1f, result.r, 0.01f);
            Assert.AreEqual(0.333f, result.g, 0.01f);
            Assert.AreEqual(0f, result.b, 0.01f);
        }

        [Test]
        public void TestToColorHexLowercase()
        {
            var result = TypeConverters.ToColor("#ff5500");
            Assert.AreEqual(1f, result.r, 0.01f);
        }

        [Test]
        public void TestToColorRGB()
        {
            var result = TypeConverters.ToColor("255,85,0");
            Assert.AreEqual(1f, result.r, 0.01f);
            Assert.AreEqual(0.333f, result.g, 0.01f);
            Assert.AreEqual(0f, result.b, 0.01f);
        }

        [Test]
        public void TestToColorEmpty()
        {
            var result = TypeConverters.ToColor("");
            Assert.AreEqual(Color.white, result);
        }

        [Test]
        public void TestToColorInvalid()
        {
            var result = TypeConverters.ToColor("not a color");
            Assert.AreEqual(Color.white, result);
        }

        #endregion

        #region String Conversion Diagnostics

        [Test]
        public void TestToStringNormal()
        {
            Assert.AreEqual("hello", TypeConverters.ToString("hello", "null"));
        }

        [Test]
        public void TestToStringCustomNullToken()
        {
            Assert.IsNull(TypeConverters.ToString("nil", "nil"));
        }

        [Test]
        public void TestToStringNullTokenCaseInsensitive()
        {
            Assert.IsNull(TypeConverters.ToString("NULL", "null"));
        }

        [Test]
        public void TestToStringWhitespace()
        {
            Assert.AreEqual("  ", TypeConverters.ToString("  ", "null"));
        }

        #endregion

        #region Array Conversion Diagnostics

        [Test]
        public void TestToArrayEmpty()
        {
            var result = TypeConverters.ToArray("", ";", TypeConverters.ToInt);
            Assert.AreEqual(1, result.Length);
        }

        [Test]
        public void TestToArraySingleElement()
        {
            var result = TypeConverters.ToArray("42", ";", TypeConverters.ToInt);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(42, result[0]);
        }

        [Test]
        public void TestToArrayFloats()
        {
            var result = TypeConverters.ToArray("1.5;2.5;3.5", ";", TypeConverters.ToFloat);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1.5f, result[0], 0.001f);
            Assert.AreEqual(2.5f, result[1], 0.001f);
            Assert.AreEqual(3.5f, result[2], 0.001f);
        }

        [Test]
        public void TestToArrayCustomDelimiter()
        {
            var result = TypeConverters.ToArray("1|2|3", "|", TypeConverters.ToInt);
            Assert.AreEqual(3, result.Length);
        }

        [Test]
        public void TestToArrayWithSpaces()
        {
            var result = TypeConverters.ToArray(" a ; b ; c ", ";", s => TypeConverters.ToString(s.Trim(), "null"));
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("a", result[0]);
            Assert.AreEqual("b", result[1]);
            Assert.AreEqual("c", result[2]);
        }

        #endregion
    }
}
