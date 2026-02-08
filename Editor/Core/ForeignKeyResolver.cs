using System.Collections.Generic;
using UnityEngine;

namespace DataToScriptableObject.Editor
{
    /// <summary>
    /// Resolves foreign key values to ScriptableObject references during import.
    /// Maintains lookup tables for each imported table to enable cross-references.
    /// </summary>
    public class ForeignKeyResolver
    {
        // tableName → { primaryKeyStringValue → generated ScriptableObject }
        private Dictionary<string, Dictionary<string, ScriptableObject>> lookups
            = new Dictionary<string, Dictionary<string, ScriptableObject>>();

        /// <summary>
        /// Register a completed table with its primary key values and generated assets.
        /// </summary>
        public void RegisterTable(
            string tableName,
            string keyColumnName,
            ScriptableObject[] assets,
            List<Dictionary<string, string>> rowData)
        {
            var tableLookup = new Dictionary<string, ScriptableObject>();
            
            for (int i = 0; i < assets.Length && i < rowData.Count; i++)
            {
                string keyValue;
                if (rowData[i].ContainsKey(keyColumnName))
                {
                    keyValue = rowData[i][keyColumnName];
                }
                else
                {
                    keyValue = i.ToString();
                }

                if (!string.IsNullOrEmpty(keyValue) && !tableLookup.ContainsKey(keyValue))
                {
                    tableLookup[keyValue] = assets[i];
                }
                // Duplicate key → warn but don't overwrite first entry
            }
            
            lookups[tableName] = tableLookup;
        }

        /// <summary>
        /// Resolve a foreign key value to its corresponding ScriptableObject reference.
        /// </summary>
        public ScriptableObject Resolve(
            string foreignKeyTable,
            string foreignKeyValue,
            out string warning)
        {
            warning = null;

            if (string.IsNullOrEmpty(foreignKeyValue) || foreignKeyValue == "null")
            {
                return null; // Null FK is valid
            }

            if (!lookups.ContainsKey(foreignKeyTable))
            {
                warning = $"Referenced table '{foreignKeyTable}' has not been processed yet. " +
                         $"Ensure tables are processed in dependency order.";
                return null;
            }

            if (!lookups[foreignKeyTable].ContainsKey(foreignKeyValue))
            {
                warning = $"Foreign key value '{foreignKeyValue}' not found in table " +
                         $"'{foreignKeyTable}'. Setting reference to null.";
                return null;
            }

            return lookups[foreignKeyTable][foreignKeyValue];
        }

        /// <summary>
        /// Clear all registered tables. Call this between import sessions.
        /// </summary>
        public void Clear()
        {
            lookups.Clear();
        }
    }
}
