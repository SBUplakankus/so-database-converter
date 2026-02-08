using System.Collections.Generic;
using System.Linq;

namespace DataToScriptableObject.Editor
{
    public class DependencyResolveResult
    {
        public bool Success { get; set; }
        public TableSchema[] OrderedSchemas { get; set; }
        public string ErrorMessage { get; set; }
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
    }

    /// <summary>
    /// Resolves table dependencies using topological sort (Kahn's algorithm).
    /// Tables with foreign keys must be processed after the tables they reference.
    /// </summary>
    public static class DependencyResolver
    {
        public static DependencyResolveResult Resolve(TableSchema[] schemas)
        {
            var result = new DependencyResolveResult();

            if (schemas == null || schemas.Length == 0)
            {
                result.Success = true;
                result.OrderedSchemas = new TableSchema[0];
                return result;
            }

            // Build a map of table names to schemas for quick lookup
            var schemaMap = schemas.ToDictionary(s => s.SourceTableName, s => s);

            // Build adjacency list: schema → list of schemas it depends on
            var dependencies = new Dictionary<TableSchema, List<TableSchema>>();
            var inDegree = new Dictionary<TableSchema, int>();

            // Initialize
            foreach (var schema in schemas)
            {
                dependencies[schema] = new List<TableSchema>();
                inDegree[schema] = 0;
            }

            // Build dependency graph
            foreach (var schema in schemas)
            {
                if (schema.Columns == null) continue;

                foreach (var column in schema.Columns)
                {
                    if (!column.IsForeignKey || string.IsNullOrEmpty(column.ForeignKeyTable))
                        continue;

                    // Check if the referenced table exists in the selected schemas
                    if (!schemaMap.ContainsKey(column.ForeignKeyTable))
                    {
                        // Referenced table not in selection — downgrade to plain int
                        result.Warnings.Add(new ValidationWarning
                        {
                            level = WarningLevel.Warning,
                            message = $"Table '{schema.SourceTableName}' references '{column.ForeignKeyTable}' " +
                                     $"which is not selected for import. Foreign key column '{column.OriginalHeader}' " +
                                     $"will be imported as a plain integer instead of a ScriptableObject reference.",
                            table = schema.SourceTableName,
                            column = column.OriginalHeader
                        });

                        // Revert to Int type
                        column.IsForeignKey = false;
                        column.ForeignKeyTable = null;
                        column.ForeignKeyColumn = null;
                        column.ForeignKeySoType = null;
                        column.Type = ResolvedType.Int;
                        continue;
                    }

                    var referencedSchema = schemaMap[column.ForeignKeyTable];

                    // Add dependency: this schema depends on referencedSchema
                    if (!dependencies[schema].Contains(referencedSchema))
                    {
                        dependencies[schema].Add(referencedSchema);
                        inDegree[schema]++;
                    }
                }
            }

            // Kahn's algorithm for topological sort
            var queue = new Queue<TableSchema>();
            var ordered = new List<TableSchema>();

            // Start with schemas that have no dependencies
            foreach (var schema in schemas)
            {
                if (inDegree[schema] == 0)
                {
                    queue.Enqueue(schema);
                }
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                ordered.Add(current);

                // For each schema that depends on current
                foreach (var other in schemas)
                {
                    if (dependencies[other].Contains(current))
                    {
                        inDegree[other]--;
                        if (inDegree[other] == 0)
                        {
                            queue.Enqueue(other);
                        }
                    }
                }
            }

            // Check if all schemas were processed
            if (ordered.Count == schemas.Length)
            {
                result.Success = true;
                result.OrderedSchemas = ordered.ToArray();
                return result;
            }

            // Circular dependency detected
            var unprocessed = schemas.Except(ordered).ToList();
            var cycle = FindCycle(unprocessed, dependencies);

            result.Success = false;
            result.ErrorMessage = $"Circular dependency detected: {cycle}. " +
                                 $"Break the cycle by removing a foreign key relationship or excluding one of these tables.";
            return result;
        }

        private static string FindCycle(List<TableSchema> unprocessed, Dictionary<TableSchema, List<TableSchema>> dependencies)
        {
            if (unprocessed.Count == 0) return "";

            var visited = new HashSet<TableSchema>();
            var path = new List<TableSchema>();

            TableSchema start = unprocessed[0];
            TableSchema current = start;

            while (true)
            {
                path.Add(current);
                visited.Add(current);

                // Find a dependency that's in the unprocessed set
                var next = dependencies[current].FirstOrDefault(d => unprocessed.Contains(d));

                if (next == null)
                {
                    // Dead end, try another starting point
                    if (unprocessed.Count > 1)
                    {
                        start = unprocessed[1];
                        current = start;
                        path.Clear();
                        visited.Clear();
                        continue;
                    }
                    break;
                }

                if (visited.Contains(next))
                {
                    // Found cycle
                    var cycleStart = path.IndexOf(next);
                    var cyclePath = path.Skip(cycleStart).Select(s => s.SourceTableName).ToList();
                    cyclePath.Add(next.SourceTableName); // Close the cycle
                    return string.Join(" → ", cyclePath);
                }

                current = next;
            }

            // Fallback: just list the unprocessed tables
            return string.Join(" → ", unprocessed.Select(s => s.SourceTableName));
        }
    }
}
