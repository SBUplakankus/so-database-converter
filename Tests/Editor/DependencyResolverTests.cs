using NUnit.Framework;
using DataToScriptableObject.Editor;
using DataToScriptableObject;
using System.Collections.Generic;
using System.Linq;

namespace DataToScriptableObject.Tests.Editor
{
    [TestFixture]
    public class DependencyResolverTests
    {
        private TableSchema CreateSchema(string tableName, params string[] foreignKeyTables)
        {
            var columns = new List<ColumnSchema>();
            
            // Add a primary key column
            columns.Add(new ColumnSchema
            {
                FieldName = "id",
                OriginalHeader = "id",
                Type = ResolvedType.Int,
                IsKey = true,
                Attributes = new Dictionary<string, string>()
            });

            // Add FK columns for each referenced table
            foreach (var fkTable in foreignKeyTables)
            {
                columns.Add(new ColumnSchema
                {
                    FieldName = fkTable + "_id",
                    OriginalHeader = fkTable + "_id",
                    Type = ResolvedType.Asset,
                    IsForeignKey = true,
                    ForeignKeyTable = fkTable,
                    ForeignKeyColumn = "id",
                    ForeignKeySoType = fkTable + "Entry",
                    Attributes = new Dictionary<string, string>()
                });
            }

            return new TableSchema
            {
                ClassName = tableName + "Entry",
                DatabaseName = tableName + "Database",
                NamespaceName = "",
                SourceTableName = tableName,
                Columns = columns.ToArray()
            };
        }

        [Test]
        public void TestNoDependencies()
        {
            var schemaA = CreateSchema("A");
            var schemaB = CreateSchema("B");
            var schemaC = CreateSchema("C");

            var result = DependencyResolver.Resolve(new[] { schemaA, schemaB, schemaC });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.OrderedSchemas.Length);
            Assert.That(result.OrderedSchemas, Contains.Item(schemaA));
            Assert.That(result.OrderedSchemas, Contains.Item(schemaB));
            Assert.That(result.OrderedSchemas, Contains.Item(schemaC));
        }

        [Test]
        public void TestLinearChain()
        {
            // A depends on B, B depends on C
            var schemaC = CreateSchema("C");
            var schemaB = CreateSchema("B", "C");
            var schemaA = CreateSchema("A", "B");

            var result = DependencyResolver.Resolve(new[] { schemaA, schemaB, schemaC });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.OrderedSchemas.Length);
            
            // C must come before B, B must come before A
            var indexC = System.Array.IndexOf(result.OrderedSchemas, schemaC);
            var indexB = System.Array.IndexOf(result.OrderedSchemas, schemaB);
            var indexA = System.Array.IndexOf(result.OrderedSchemas, schemaA);
            
            Assert.Less(indexC, indexB, "C should come before B");
            Assert.Less(indexB, indexA, "B should come before A");
        }

        [Test]
        public void TestDiamond()
        {
            // A→B, A→C, B→D, C→D
            var schemaD = CreateSchema("D");
            var schemaB = CreateSchema("B", "D");
            var schemaC = CreateSchema("C", "D");
            var schemaA = CreateSchema("A", "B", "C");

            var result = DependencyResolver.Resolve(new[] { schemaA, schemaB, schemaC, schemaD });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(4, result.OrderedSchemas.Length);
            
            // D first, then B and C (either order), then A
            var indexD = System.Array.IndexOf(result.OrderedSchemas, schemaD);
            var indexB = System.Array.IndexOf(result.OrderedSchemas, schemaB);
            var indexC = System.Array.IndexOf(result.OrderedSchemas, schemaC);
            var indexA = System.Array.IndexOf(result.OrderedSchemas, schemaA);
            
            Assert.AreEqual(0, indexD, "D should be first");
            Assert.Less(indexB, indexA, "B should come before A");
            Assert.Less(indexC, indexA, "C should come before A");
        }

        [Test]
        public void TestSingleTable()
        {
            var schemaA = CreateSchema("A");
            var result = DependencyResolver.Resolve(new[] { schemaA });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.OrderedSchemas.Length);
            Assert.AreEqual(schemaA, result.OrderedSchemas[0]);
        }

        [Test]
        public void TestCircularDetected()
        {
            // A→B, B→A (circular)
            var schemaA = CreateSchema("A", "B");
            var schemaB = CreateSchema("B", "A");

            var result = DependencyResolver.Resolve(new[] { schemaA, schemaB });

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.That(result.ErrorMessage, Does.Contain("Circular"));
            Assert.That(result.ErrorMessage, Does.Contain("A").Or.Contains("B"));
        }

        [Test]
        public void TestThreeWayCycle()
        {
            // A→B, B→C, C→A (cycle)
            var schemaA = CreateSchema("A", "B");
            var schemaB = CreateSchema("B", "C");
            var schemaC = CreateSchema("C", "A");

            var result = DependencyResolver.Resolve(new[] { schemaA, schemaB, schemaC });

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.That(result.ErrorMessage, Does.Contain("Circular"));
        }

        [Test]
        public void TestPartialCycle()
        {
            // A→B, B→C, C→B (cycle between B and C), D→A
            var schemaA = CreateSchema("A", "B");
            var schemaB = CreateSchema("B", "C");
            var schemaC = CreateSchema("C", "B");
            var schemaD = CreateSchema("D", "A");

            var result = DependencyResolver.Resolve(new[] { schemaA, schemaB, schemaC, schemaD });

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.That(result.ErrorMessage, Does.Contain("Circular"));
        }

        [Test]
        public void TestFKToMissingTable()
        {
            // A has FK to table "missing" not in schemas
            var schemaA = CreateSchema("A", "missing");

            var result = DependencyResolver.Resolve(new[] { schemaA });

            Assert.IsTrue(result.Success, "Should succeed after downgrading FK");
            Assert.AreEqual(1, result.OrderedSchemas.Length);
            
            // Check that warning was added
            Assert.IsNotEmpty(result.Warnings);
            var warning = result.Warnings.FirstOrDefault(w => w.table == "A");
            Assert.IsNotNull(warning);
            Assert.That(warning.message, Does.Contain("missing"));
            Assert.That(warning.message, Does.Contain("not selected"));
            
            // Check that FK was cleared
            var fkColumn = schemaA.Columns.FirstOrDefault(c => c.OriginalHeader == "missing_id");
            Assert.IsNotNull(fkColumn);
            Assert.IsFalse(fkColumn.IsForeignKey, "FK flag should be cleared");
            Assert.AreEqual(ResolvedType.Int, fkColumn.Type, "Type should revert to Int");
        }

        [Test]
        public void TestEmptyInput()
        {
            var result = DependencyResolver.Resolve(new TableSchema[0]);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.OrderedSchemas.Length);
        }

        [Test]
        public void TestNullInput()
        {
            var result = DependencyResolver.Resolve(null);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, result.OrderedSchemas.Length);
        }
    }
}
