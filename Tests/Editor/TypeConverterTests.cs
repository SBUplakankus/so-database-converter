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
    }
}
