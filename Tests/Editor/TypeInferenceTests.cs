using DataToScriptableObject;
using DataToScriptableObject.Editor;
using NUnit.Framework;

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

        // ==================== DIAGNOSTIC TESTS ====================

        #region Numeric Inference Diagnostics

        [Test]
        public void TestNegativeInts()
        {
            var values = new[] { "-1", "-42", "-100" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestLargeInts()
        {
            var values = new[] { "1000000", "2000000", "999999999" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestNegativeFloats()
        {
            var values = new[] { "-1.5", "-3.14", "-0.001" };
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(values));
        }

        [Test]
        public void TestScientificNotation()
        {
            var values = new[] { "1e5", "2.5e-3", "3E10" };
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(values));
        }

        [Test]
        public void TestZeroValues()
        {
            var values = new[] { "0", "0", "0" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestZeroPointZero()
        {
            var values = new[] { "0.0", "0.0", "0.0" };
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(values));
        }

        [Test]
        public void TestLeadingZeros()
        {
            var values = new[] { "001", "042", "007" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestDecimalWithoutLeadingZero()
        {
            var values = new[] { ".5", ".25", ".001" };
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(values));
        }

        #endregion

        #region Boolean Inference Diagnostics

        [Test]
        public void TestMixedCaseBools()
        {
            var values = new[] { "True", "FALSE", "tRuE", "FaLsE" };
            Assert.AreEqual(ResolvedType.Bool, TypeInference.Infer(values));
        }

        [Test]
        public void TestMixedYesNo()
        {
            var values = new[] { "yes", "NO", "Yes", "no" };
            Assert.AreEqual(ResolvedType.Bool, TypeInference.Infer(values));
        }

        [Test]
        public void TestMixedTrueFalseYesNo()
        {
            var values = new[] { "true", "no", "yes", "false" };
            Assert.AreEqual(ResolvedType.Bool, TypeInference.Infer(values));
        }

        [Test]
        public void TestNumericBoolsAsInts()
        {
            // 1 and 0 should be treated as ints, not bools
            var values = new[] { "1", "0" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestMixed10WithOthers()
        {
            // If there's other numeric values, should still be int
            var values = new[] { "1", "0", "2" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        #endregion

        #region Empty and Null Handling

        [Test]
        public void TestNullArray()
        {
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(null));
        }

        [Test]
        public void TestEmptyArray()
        {
            var values = new string[0];
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(values));
        }

        [Test]
        public void TestAllNullValues()
        {
            var values = new string[] { null, null, null };
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(values));
        }

        [Test]
        public void TestAllWhitespace()
        {
            var values = new[] { "   ", "\t", "  \n  " };
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(values));
        }

        [Test]
        public void TestMixedEmptyAndNumbers()
        {
            var values = new[] { "", "1", null, "2", "   " };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestSingleValue()
        {
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(new[] { "42" }));
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(new[] { "3.14" }));
            Assert.AreEqual(ResolvedType.Bool, TypeInference.Infer(new[] { "true" }));
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(new[] { "hello" }));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void TestNumericStrings()
        {
            // String that looks like number followed by text
            var values = new[] { "123abc", "456def" };
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(values));
        }

        [Test]
        public void TestSpecialFloatValues()
        {
            // +/- signs
            var values = new[] { "+1.5", "-2.5", "+3.5" };
            Assert.AreEqual(ResolvedType.Float, TypeInference.Infer(values));
        }

        [Test]
        public void TestMixedValidInvalid()
        {
            // One bad apple spoils the bunch
            var values = new[] { "1", "2", "three", "4" };
            Assert.AreEqual(ResolvedType.String, TypeInference.Infer(values));
        }

        [Test]
        public void TestAllSameValue()
        {
            var values = new[] { "42", "42", "42", "42" };
            Assert.AreEqual(ResolvedType.Int, TypeInference.Infer(values));
        }

        [Test]
        public void TestBoolNotString()
        {
            // "TRUE" and "FALSE" should be bool, not string
            var values = new[] { "TRUE", "FALSE" };
            Assert.AreEqual(ResolvedType.Bool, TypeInference.Infer(values));
        }

        [Test]
        public void TestBoolPriorityOverInt()
        {
            // "true" should be bool even though it's not int
            var values = new[] { "true" };
            Assert.AreEqual(ResolvedType.Bool, TypeInference.Infer(values));
        }

        #endregion
    }
}
