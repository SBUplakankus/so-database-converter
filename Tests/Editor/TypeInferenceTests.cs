using NUnit.Framework;
using DataToScriptableObject;
using DataToScriptableObject.Editor;

namespace DataToScriptableObject.Tests.Editor
{
    public class TypeInferenceTests
    {
        [Test]
        public void TestAllInts()
        {
            var values = new[] { "1", "2", "42", "-5" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestAllFloats()
        {
            var values = new[] { "1.5", "2.7", "3.14" };
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(values));
        }

        [Test]
        public void TestMixedIntFloat()
        {
            var values = new[] { "1", "2", "3.5" };
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(values));
        }

        [Test]
        public void TestAllBools()
        {
            var values = new[] { "true", "false", "True", "FALSE" };
            Assert.AreEqual(ResolvedType.Bool, TypeInference.Infer(values));
        }

        [Test]
        public void TestBoolWithYesNo()
        {
            var values = new[] { "yes", "no", "Yes", "NO" };
            Assert.AreEqual(ResolvedType.Bool, TypeInference.Infer(values));
        }

        [Test]
        public void TestBoolWith01()
        {
            var values = new[] { "1", "0", "1", "0" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestOneBadIntFallsToFloat()
        {
            var values = new[] { "1", "2", "3.5", "4" };
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(values));
        }

        [Test]
        public void TestOneBadFloatFallsToString()
        {
            var values = new[] { "1.5", "abc", "3.14" };
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(values));
        }

        [Test]
        public void TestAllEmpty()
        {
            var values = new[] { "", "", "" };
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(values));
        }

        [Test]
        public void TestMixedEmptyAndInt()
        {
            var values = new[] { "", "1", "", "2" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestStrings()
        {
            var values = new[] { "hello", "world" };
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(values));
        }
    }
}
