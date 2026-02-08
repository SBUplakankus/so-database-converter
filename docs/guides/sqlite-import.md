# SQLite Import

## Overview

SQLite import reads `.db`, `.sqlite`, `.sqlite3`, and `.db3` database files. It extracts table schemas, resolves foreign key relationships, and generates ScriptableObject classes with proper dependency ordering.

## Supported Features

| Feature | Description |
|---------|-------------|
| Multi-table import | Select which tables to import from the database |
| Foreign key resolution | FK columns become ScriptableObject references |
| Dependency ordering | Tables are processed in topological order based on FK dependencies |
| Type mapping | SQLite types are mapped to C# types automatically |
| CHECK constraints | `CHECK(col BETWEEN min AND max)` becomes `[Range(min, max)]` |
| CHECK IN | `CHECK(col IN ('A','B','C'))` generates an enum type |
| UNIQUE warnings | Unique columns produce warnings about potential naming collisions |

## Type Mapping

SQLite column types are mapped to C# types as follows:

| SQLite Type | C# Type |
|-------------|---------|
| INTEGER, INT, TINYINT, SMALLINT, MEDIUMINT | int |
| BIGINT | long |
| REAL, DOUBLE, FLOAT, NUMERIC, DECIMAL | float |
| BOOLEAN, BOOL | bool |
| TEXT, VARCHAR, CHAR, CLOB, NVARCHAR | string |
| BLOB | string (base64) |

Parameterized types like `VARCHAR(255)` and `DECIMAL(10,2)` are handled correctly.

## Foreign Key Resolution

When a table has foreign key constraints, the tool:

1. Detects FK relationships via `PRAGMA foreign_key_list`.
2. Maps FK columns to `ResolvedType.Asset` with the target ScriptableObject type name.
3. Resolves table dependencies using Kahn's algorithm for topological sorting.
4. Reports warnings for missing FK targets or circular dependencies.

## Validation

SQLite validation checks:

1. **File existence and readability**.
2. **SQLite magic header** -- The first 16 bytes must contain `SQLite format 3\0`.
3. **User tables** -- At least one non-system table must exist.
4. **Schema integrity** -- All table schemas must be readable.
5. **Foreign key targets** -- Referenced tables should exist in the database.
6. **Dependency cycles** -- Circular foreign key references are detected and reported.

## Example

Given a database with tables `elements`, `enemies`, `items`, and `loot_drops` where `loot_drops` references `enemies` and `items`:

1. The tool detects the dependency graph.
2. Tables are sorted: `elements` -> `enemies` -> `items` -> `loot_drops`.
3. Each table generates an entry class and a database class.
4. FK columns in `loot_drops` become references to `EnemiesEntry` and `ItemsEntry` ScriptableObjects.
