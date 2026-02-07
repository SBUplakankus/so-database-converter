# Data to ScriptableObject — V2 SQLite Agent Implementation Guide

## Prerequisites

This guide assumes V1 (CSV + Google Sheets) is fully implemented as described in `AGENT_GUIDE_V1.md`. All shared infrastructure (Models, SchemaBuilder, CodeGenerator, ReflectionMapper, AssetPopulator, TypeConverters, RecompileHandler, Editor Window) already exists.

V2 adds SQLite database support as a new source type. The existing pipeline is reused — only new reader/validator/resolver classes are added.

---

## New Files to Create

```
Editor/
├── Core/
│   ├── SQLiteReader.cs              ← NEW: reads .db files via sqlite-net
│   ├── SQLiteTypeMapper.cs          ← NEW: SQLite type → ResolvedType
│   ├── SQLiteSchemaParser.cs        ← NEW: parses CREATE TABLE DDL
│   ├── DependencyResolver.cs        ← NEW: topological sort for FK order
│   └── ForeignKeyResolver.cs        ← NEW: resolves FK → SO references
├── Validation/
│   └── SQLiteValidator.cs           ← NEW: validates .db files
Dependencies/
└── sqlite-net/
    └── SQLite.cs                    ← NEW: embedded sqlite-net library (editor-only)
Samples~/
└── SQLite/
    ├── game_data_example.db         ← NEW: example database
    └── README.md                    ← NEW: SQLite usage docs
Tests/
└── Editor/
    ├── SQLiteTypeMapperTests.cs     ← NEW
    ├── DependencyResolverTests.cs   ← NEW
    └── SQLiteValidatorTests.cs      ← NEW
```

## Files to Modify

```
Editor/UI/ImporterWindow.cs          ← Enable SQLite radio button, add SQLite source UI
Editor/Core/AssetPopulator.cs        ← Add FK resolution pass using ForeignKeyResolver
Models/ColumnSchema.cs               ← FK fields already exist from V1 (no changes needed)
package.json                         ← Bump version to 0.2.0, add "sqlite" keyword
CHANGELOG.md                         ← Add v0.2.0 entry
```

---

## sqlite-net Dependency

Embed the synchronous-only version of sqlite-net as a single file at `Dependencies/sqlite-net/SQLite.cs`. Source: https://github.com/praeclarum/sqlite-net (file: `src/SQLite.cs`).

This file must be inside the Editor assembly definition scope so it does NOT ship in builds. Verify the `DataToScriptableObject.Editor.asmdef` in `Editor/` covers `Dependencies/` via its folder hierarchy, OR move `SQLite.cs` directly into `Editor/Core/Dependencies/`. The `.asmdef` `includePlatforms` must be `["Editor"]` to guarantee exclusion from builds.

Do NOT use NuGet packages or precompiled DLL references. A single embedded `.cs` file is the cleanest approach for a Unity package and avoids all platform-specific native DLL issues.

---

## Implementation Order

### Phase 1: SQLite Type Mapper

**File: `Editor/Core/SQLiteTypeMapper.cs`**

Static class. Maps SQLite declared column type strings to the shared `ResolvedType` enum.

**Method:** `ResolvedType Map(string sqliteType, out string warning)`

- If `sqliteType` is null or empty → return `ResolvedType.String`, no warning.
- Normalize: `sqliteType = sqliteType.Trim().ToUpperInvariant()`.
- Match using CONTAINS logic in this priority order (order matters — check specific types before generic ones):
  1. Contains `BIGINT` → `ResolvedType.Long`.
  2. Contains `INT` (but NOT `POINT`) → `ResolvedType.Int`.
  3. Contains `DOUBLE` → `ResolvedType.Double`.
  4. Contains `FLOAT` → `ResolvedType.Float`.
  5. Contains `REAL` → `ResolvedType.Float`.
  6. Contains `NUMERIC` → `ResolvedType.Float`.
  7. Contains `BOOL` → `ResolvedType.Bool`.
  8. Contains `TEXT` → `ResolvedType.String`.
  9. Contains `VARCHAR` → `ResolvedType.String`.
  10. Contains `CHAR` (but not `VARCHAR`) → `ResolvedType.String`.
  11. Contains `CLOB` → `ResolvedType.String`.
  12. Contains `NCHAR` ��� `ResolvedType.String`.
  13. Contains `NVARCHAR` → `ResolvedType.String`.
  14. Contains `BLOB` → `ResolvedType.String`, warning = "BLOB columns are imported as string. Binary data is not supported.".
  15. No match → `ResolvedType.String`, warning = "Unrecognized SQLite type '{sqliteType}', defaulting to string.".

- The CONTAINS logic handles parameterized types like `VARCHAR(255)`, `NUMERIC(10,2)`, `INTEGER PRIMARY KEY` etc.

---

### Phase 2: SQLite Schema Parser

**File: `Editor/Core/SQLiteSchemaParser.cs`**

Static class. Parses raw `CREATE TABLE` SQL statements to extract constraints that `PRAGMA table_info` does not provide.

**Method:** `Dictionary<string, ColumnConstraints> ParseDDL(string createTableSQL)`

- Returns a dictionary mapping column name → extracted constraints.
- `ColumnConstraints` is a simple class:

```csharp
public class ColumnConstraints
{
    public float? rangeMin;
    public float? rangeMax;
    public string[] checkInValues;   // if CHECK(col IN ('A','B','C'))
    public bool isUnique;
}
```

**Parsing rules (all regex-based, best-effort):**

1. **Range CHECK constraints:**
   - Pattern: `CHECK\s*\(\s*["']?(\w+)["']?\s*>=\s*(-?\d+\.?\d*)\s+AND\s+["']?(\w+)["']?\s*<=\s*(-?\d+\.?\d*)\s*\)`
   - Also match the reverse order: `<=` first, `>=` second.
   - Extract column name, min, max.
   - Store as `rangeMin` and `rangeMax`.

2. **IN CHECK constraints (enum-like):**
   - Pattern: `CHECK\s*\(\s*["']?(\w+)["']?\s+IN\s*\(\s*(.+?)\s*\)\s*\)`
   - Extract column name and the values list.
   - Parse individual values: split by comma, strip quotes (single and double), trim whitespace.
   - Store as `checkInValues`.

3. **UNIQUE constraints:**
   - Column-level: look for `UNIQUE` keyword after the column's type declaration in the DDL.
   - Table-level: look for `UNIQUE\s*\(\s*["']?(\w+)["']?\s*\)` at the end of the CREATE TABLE.
   - Store as `isUnique = true`.

- If a pattern doesn't match or the SQL is too complex, skip gracefully. Never throw from this class. Return an empty dictionary on unparseable input.

---

### Phase 3: Dependency Resolver

**File: `Editor/Core/DependencyResolver.cs`**

**Method:** `DependencyResolveResult Resolve(TableSchema[] schemas)`

```csharp
public class DependencyResolveResult
{
    public bool success;
    public TableSchema[] orderedSchemas;   // topologically sorted
    public string errorMessage;            // cycle description if failed
}
```

**Algorithm: Kahn's algorithm for topological sort.**

1. Build adjacency list from foreign key info:
   - For each schema, for each column where `isForeignKey == true`:
     - Find the schema whose `sourceTableName` matches `foreignKeyTable`.
     - Add an edge: this schema depends on that schema.
   - If a foreign key references a table not in the selected schemas, add a warning: "Table '{tableName}' references '{fkTable}' which is not selected for import. Foreign key column '{columnName}' will be imported as a plain integer instead of a ScriptableObject reference." Set `isForeignKey = false` on that column and revert its type to `ResolvedType.Int`.

2. Compute in-degree for each schema (number of schemas it depends on).

3. Initialize queue with all schemas that have in-degree 0 (no dependencies).

4. While queue is not empty:
   - Dequeue a schema, add to result list.
   - For each schema that depends on the dequeued one: decrement its in-degree. If it reaches 0, enqueue it.

5. If result list length equals input length → success. Return ordered schemas.

6. If result list is shorter → circular dependency detected:
   - Find the cycle: collect all schemas NOT in the result list. Pick one, follow its foreign key dependencies until you revisit a schema already in the walk path.
   - Build error message: `"Circular dependency detected: {A} → {B} → {C} → {A}. Break the cycle by removing a foreign key relationship or excluding one of these tables."`.
   - Return `success = false`.

---

### Phase 4: SQLite Reader

**File: `Editor/Core/SQLiteReader.cs`**

**Method:** `SQLiteReadResult Read(string dbPath, GenerationSettings settings)`

```csharp
public class SQLiteReadResult
{
    public bool success;
    public string errorMessage;
    public TableSchema[] schemas;          // in dependency order
    public List<ValidationWarning> warnings;
}
```

**Implementation steps:**

1. Open connection: `new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly)`. Wrap in try/catch — on failure, return error "Cannot open database: {exception.Message}".

2. Query all user tables:
   ```sql
   SELECT name, sql FROM sqlite_master
   WHERE type='table'
   AND name NOT LIKE 'sqlite_%'
   AND name NOT LIKE '\_%' ESCAPE '\'
   AND name != 'android_metadata'
   ORDER BY name
   ```
   - The `android_metadata` exclusion handles a common Android SQLite artifact.
   - Store each table's name and raw DDL SQL.

3. For each table, read column schema:
   ```sql
   PRAGMA table_info({tableName})
   ```
   - Returns rows with columns: `cid`, `name`, `type`, `notnull`, `dflt_value`, `pk`.
   - For each row, create a `ColumnSchema`:
     - `originalHeader` = column `name`.
     - `fieldName` = sanitized via `NameSanitizer.SanitizeFieldName(name)`.
     - `resolvedType` = `SQLiteTypeMapper.Map(type, out warning)`. If warning, add to warnings list.
     - `isOptional` = `notnull == 0`.
     - `defaultValue` = `dflt_value` (may be null).
     - `isKey` = `pk == 1`.

4. For each table, read foreign keys:
   ```sql
   PRAGMA foreign_key_list({tableName})
   ```
   - Returns rows with columns: `id`, `seq`, `table`, `from`, `to`, `on_update`, `on_delete`, `match`.
   - For each row:
     - Find the `ColumnSchema` where `originalHeader == from`.
     - Set `isForeignKey = true`.
     - Set `foreignKeyTable = table`.
     - Set `foreignKeyColumn = to`.
     - Set `foreignKeySOType` = PascalCase(table) + "Entry". Use the same PascalCase logic as `NameSanitizer` (e.g., `"loot_drops"` → `"LootDropsEntry"`, `"elements"` → `"ElementsEntry"` or singularized if you want — keep it simple, just PascalCase + "Entry").
     - Change the column's `resolvedType` to `ResolvedType.Asset`.

5. For each table, parse DDL constraints:
   - Call `SQLiteSchemaParser.ParseDDL(rawSQL)`.
   - For each column with a range constraint: add `"range" => "{min},{max}"` to the column's `attributes` dictionary.
   - For each column with CHECK IN values: if the column is currently typed as `String` or `Int`, change `resolvedType` to `ResolvedType.Enum`, set `enumValues` to the parsed values.
   - For each column with UNIQUE: add to warnings as info "Column '{name}' is UNIQUE — duplicate values will cause asset naming collisions."

6. For each table, read all data:
   ```sql
   SELECT * FROM {tableName}
   ```
   - Read using `SQLiteConnection.Query()` or execute command and read via `SQLiteDataReader`.
   - Convert every cell value to its string representation for the shared pipeline: `value?.ToString() ?? ""`.
   - For boolean columns: SQLite stores as `0`/`1` integers. Convert: `0` → `"false"`, `1` → `"true"` (or leave as `"0"`/`"1"` — the shared `TypeConverters.ToBool()` handles both).
   - Store as `List<Dictionary<string, string>>` in the `TableSchema.rows`.

7. Construct `TableSchema` for each table:
   - `className` = PascalCase(tableName) + "Entry".
   - `databaseName` = PascalCase(tableName) + "Database".
   - `namespaceName` = from `GenerationSettings.namespaceName` (user sets this globally).
   - `sourceTableName` = raw table name from SQLite.

8. Close the SQLite connection. Always close in a `finally` block.

9. Run `DependencyResolver.Resolve()` on all schemas.
   - If cycle detected, return the error.
   - If success, return schemas in the resolved order.

---

### Phase 5: Foreign Key Resolver

**File: `Editor/Core/ForeignKeyResolver.cs`**

Instance class. Maintains state across the processing of multiple tables during a single import operation.

```csharp
public class ForeignKeyResolver
{
    // tableName → { primaryKeyStringValue → generated ScriptableObject }
    private Dictionary<string, Dictionary<string, ScriptableObject>> lookups
        = new Dictionary<string, Dictionary<string, ScriptableObject>>();

    public void RegisterTable(
        string tableName,
        string keyColumnName,
        ScriptableObject[] assets,
        List<Dictionary<string, string>> rowData)
    {
        var tableLookup = new Dictionary<string, ScriptableObject>();
        for (int i = 0; i < assets.Length && i < rowData.Count; i++)
        {
            var keyValue = rowData[i].ContainsKey(keyColumnName)
                ? rowData[i][keyColumnName]
                : i.ToString();

            if (!string.IsNullOrEmpty(keyValue) && !tableLookup.ContainsKey(keyValue))
            {
                tableLookup[keyValue] = assets[i];
            }
            // Duplicate key → warn but don't overwrite first entry
        }
        lookups[tableName] = tableLookup;
    }

    public ScriptableObject Resolve(
        string foreignKeyTable,
        string foreignKeyValue,
        out string warning)
    {
        warning = null;

        if (string.IsNullOrEmpty(foreignKeyValue) || foreignKeyValue == "null")
            return null; // Null FK is valid

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

    public void Clear()
    {
        lookups.Clear();
    }
}
```

---

### Phase 6: SQLite Validator

**File: `Editor/Validation/SQLiteValidator.cs`**

Implements `ISourceReader`.

**Constructor:** takes `string filePath` and `GenerationSettings settings`.

**`ValidateQuick()` implementation:**

1. Check `File.Exists(filePath)`. If not → `status = InvalidSource`, `errorMessage = "File not found: {filePath}"`.

2. Check extension. Accepted: `.db`, `.sqlite`, `.sqlite3`, `.db3`. If different: add warning "Unexpected file extension '.{ext}'. Attempting to read as SQLite anyway." but do NOT fail — SQLite databases can have any extension.

3. Read first 16 bytes of the file:
   ```csharp
   using (var stream = File.OpenRead(filePath))
   {
       byte[] header = new byte[16];
       int bytesRead = stream.Read(header, 0, 16);
       if (bytesRead < 16)
       {
           // File too small to be SQLite
           return InvalidFormat("File is too small to be a valid SQLite database.");
       }
       string headerString = System.Text.Encoding.ASCII.GetString(header);
       if (!headerString.StartsWith("SQLite format 3\0"))
       {
           return InvalidFormat(
               "File is not a valid SQLite database. Expected 'SQLite format 3' header. " +
               "The file may be corrupted, encrypted, or a different database format " +
               "(such as Microsoft Access). This tool requires standard SQLite 3.x files.");
       }
   }
   ```
   - Catch `IOException` → "Cannot read file. It may be locked by another application."
   - Catch `UnauthorizedAccessException` → "Permission denied when reading file."

4. If all checks pass → `status = Valid`.

**`ValidateFull(Action<SourceValidationResult> onComplete)` implementation:**

1. Try opening `SQLiteConnection` with `ReadOnly` flag. Catch `SQLiteException` or general `Exception`:
   - Message: "Cannot open database: {exception.Message}. The file may be corrupted, encrypted with a tool like SQLCipher, or created with an incompatible SQLite version."

2. Query user tables (same query as SQLiteReader). If zero tables found:
   - Message: "Database contains no user tables. Only SQLite system tables were found. Ensure the database has at least one table with data."

3. For each table, query row count: `SELECT COUNT(*) FROM {tableName}`.
   - If a table has 0 rows: add warning (Info level) "Table '{tableName}' has 0 rows and will produce no assets."
   - If ALL tables have 0 rows: status `InvalidContent`, message "All tables in the database are empty."

4. For each table, read column info via PRAGMA. If any table returns 0 columns:
   - Message: "Cannot read schema for table '{tableName}'. The table may be corrupted."

5. For each table, read foreign keys. Validate that each FK target table exists in the database:
   - If target table doesn't exist: add warning (Warning level) "Table '{tableName}' column '{columnName}' references table '{fkTable}' which does not exist in this database. The column will be imported as a plain integer."

6. Build schemas and run `DependencyResolver.Resolve()`. If cycle detected:
   - Status `InvalidSchema`, message from the resolver.

7. Populate the result:
   - `tableCount` = number of user tables.
   - `totalRowCount` = sum of all table row counts.
   - `totalColumnCount` = sum of all table column counts.
   - `tableNames` = array of table names.
   - Collect all warnings.

8. Close the connection in a `finally` block.

9. Call `onComplete` synchronously.

**`ExtractSchemas()` implementation:**
- Call `SQLiteReader.Read()` and return the schemas array.

---

### Phase 7: Modify AssetPopulator for FK Support

**Changes to `Editor/Core/AssetPopulator.cs`:**

Add a new method or overload for multi-table SQLite import:

```csharp
public ImportResult PopulateMultiTable(
    TableSchema[] orderedSchemas,
    Dictionary<string, System.Type> typeMap,  // sourceTableName → compiled Type
    Dictionary<string, FieldMapping[]> mappingMap, // sourceTableName → mappings
    PopulationSettings settings)
```

**Implementation:**

1. Create a `ForeignKeyResolver` instance.

2. For each schema in order (already topologically sorted):
   - Get the type and mappings from the dictionaries.
   - Find the key column for this table (where `isKey == true`).
   - Create all assets for this table using the existing single-table logic.
   - **Foreign key column handling:** When setting a field value and the column `isForeignKey == true`:
     - Get the raw FK value from the row data.
     - Call `fkResolver.Resolve(column.foreignKeyTable, rawValue, out warning)`.
     - The returned `ScriptableObject` is the value to set on the field.
     - If warning, add to import result warnings.
   - After all assets for this table are created, call `fkResolver.RegisterTable(tableName, keyColumnName, assets, rowData)`.

3. Continue with next table. Because of dependency ordering, all referenced tables are already registered in the resolver before any table that references them is processed.

4. After all tables: create database containers for each table (if enabled), save assets, refresh.

5. Return combined `ImportResult` for all tables.

The existing single-table `Populate()` method for CSV remains unchanged. The new `PopulateMultiTable()` wraps it with FK resolution.

---

### Phase 8: Modify Editor Window

**Changes to `Editor/UI/ImporterWindow.cs`:**

1. **Enable the SQLite radio button.** Remove the disabled/tooltip state from V1. The source type enum now has three fully functional options: `CSV File`, `Google Sheets`, `SQLite Database`.

2. **Add SQLite source UI.** When SQLite is selected:
   - Show `EditorGUILayout.ObjectField` filtered to `DefaultAsset` (Unity doesn't have a specific type for .db files). Add a manual path text field with browse button as fallback.
   - On file selection: create `SQLiteValidator`, run `ValidateQuick()`, then `ValidateFull()`.

3. **Add table selection UI.** After successful SQLite validation, show a list of discovered tables with checkboxes:
   ```
   ┌─── Tables Found: 4 ──────────────────────────────────────┐
   │ ☑ items        (47 rows, 8 columns, 1 FK)                │
   │ ☑ elements     (5 rows, 3 columns, 0 FK)                 │
   │ ☑ enemies      (23 rows, 6 columns, 1 FK)                │
   │ ☐ _migrations  (system table, auto-excluded)              │
   └───────────────────────────────────────────────────────────┘
   ```
   - System/migration tables unchecked by default.
   - Show FK count per table.
   - Deselecting a table that others depend on → show warning.

4. **Add per-table settings.** When SQLite is selected:
   - Dropdown to select which table to configure.
   - Per table: Generation Mode (Generate New / Use Existing), Class Name override, Naming Column.
   - Default class name: PascalCase(tableName) + "Entry".

5. **Add relationship visualization.** Below the table list, show FK relationships:
   ```
   elements ◄── items.element_id
   items    ◄── loot_drops.item_id
   enemies  ◄── loot_drops.enemy_id

   Generation order: 1. elements  2. enemies  3. items  4. loot_drops
   ```

6. **Modify Generate button behavior for SQLite:**
   - If Generate New Class mode: generate ALL selected table classes in one batch before triggering recompile. Store all pending schemas in `PendingImportState`. After single recompile, `RecompileHandler` processes all tables in dependency order.
   - If Use Existing Class mode: map all tables, then populate all in dependency order using `PopulateMultiTable()`.

---

### Phase 9: Tests

**File: `Tests/Editor/SQLiteTypeMapperTests.cs`**
```
- TestInteger: "INTEGER" → Int.
- TestIntShort: "INT" → Int.
- TestBigint: "BIGINT" → Long.
- TestBigintBeforeInt: "BIGINT" not matched as Int (priority order).
- TestReal: "REAL" → Float.
- TestFloat: "FLOAT" → Float.
- TestDouble: "DOUBLE" → Double.
- TestText: "TEXT" → String.
- TestVarchar: "VARCHAR(255)" → String.
- TestBoolean: "BOOLEAN" → Bool.
- TestBool: "BOOL" → Bool.
- TestBlob: "BLOB" → String with warning.
- TestEmpty: "" → String.
- TestNull: null → String.
- TestUnknown: "MONEY" → String with warning.
- TestCaseInsensitive: "integer" → Int.
- TestParameterized: "NUMERIC(10,2)" → Float.
- TestIntegerPrimaryKey: "INTEGER PRIMARY KEY" → Int (still matches INT).
```

**File: `Tests/Editor/DependencyResolverTests.cs`**
```
- TestNoDependencies: [A, B, C] with no FKs → returned in some valid order, all present.
- TestLinearChain: A depends on B, B depends on C → order is [C, B, A].
- TestDiamond: A→B, A→C, B→D, C→D → D first, then B and C (either order), then A.
- TestSingleTable: [A] → [A].
- TestCircularDetected: A→B, B→A → success is false, errorMessage contains both table names.
- TestThreeWayCycle: A→B, B→C, C→A → detected with cycle path.
- TestPartialCycle: A→B, B→C, C→B (cycle), D→A → cycle detected for B,C. D and A not in cycle but can't be resolved either.
- TestFKToMissingTable: A has FK to table "missing" not in schemas → column FK cleared, column reverted to Int, warning added.
- TestEmptyInput: [] → empty result, success true.
```

**File: `Tests/Editor/SQLiteValidatorTests.cs`**
```
- TestFileNotFound: non-existent path → InvalidSource.
- TestNotSQLiteFile: create temp file with random bytes → InvalidFormat with magic bytes message.
- TestValidExtensions: .db, .sqlite, .sqlite3, .db3 → all accepted.
- TestUnexpectedExtension: .txt → warning but still attempts (does not fail on extension alone).
- TestEmptyFile: 0-byte file → InvalidFormat (too small).
- TestTooSmallFile: 10-byte file → InvalidFormat.
```

---

## Sample SQLite Database

**File: `Samples~/SQLite/README.md`**

````markdown
# SQLite Examples

The `game_data_example.db` file contains a sample game database with relational tables.

## Tables

### elements
| Column | Type    | Constraints |
|--------|---------|-------------|
| id     | INTEGER | PRIMARY KEY |
| name   | TEXT    | NOT NULL    |
| color  | TEXT    |             |

### enemies
| Column | Type    | Constraints         |
|--------|---------|---------------------|
| id     | INTEGER | PRIMARY KEY         |
| name   | TEXT    | NOT NULL            |
| health | REAL    | CHECK(health >= 0)  |

### items
| Column     | Type    | Constraints                  |
|------------|---------|------------------------------|
| id         | INTEGER | PRIMARY KEY                  |
| name       | TEXT    | NOT NULL                     |
| damage     | REAL    | CHECK(damage >= 0 AND damage <= 100) |
| element_id | INTEGER | REFERENCES elements(id)      |

### loot_drops
| Column    | Type    | Constraints              |
|-----------|---------|--------------------------|
| id        | INTEGER | PRIMARY KEY              |
| item_id   | INTEGER | REFERENCES items(id)     |
| enemy_id  | INTEGER | REFERENCES enemies(id)   |
| drop_rate | REAL    | CHECK(drop_rate >= 0 AND drop_rate <= 1) |

## Relationships

```
elements ◄── items.element_id
items    ◄── loot_drops.item_id
enemies  ◄── loot_drops.enemy_id
```

## Usage

1. Open `Tools > Data to ScriptableObject`
2. Select `SQLite Database` as the source type
3. Drag `game_data_example.db` into the file field
4. Select which tables to import
5. Click Generate

The tool will:
- Generate ScriptableObject classes for each table
- Create assets for each row
- Automatically resolve foreign keys as ScriptableObject references