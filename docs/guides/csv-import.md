# CSV Import

## Overview

CSV import reads local `.csv` files and converts them into ScriptableObject classes and assets. The parser is RFC 4180 compliant and supports quoted fields, embedded delimiters, and multi-line values.

## Multi-Row Header Convention

The CSV format uses up to three header rows to define column metadata:

| Row | Purpose | Required |
|-----|---------|----------|
| 1 | Column names | Yes |
| 2 | Type hints | No (types are inferred from data) |
| 3 | Flags and attributes | No |

### Type Hints (Row 2)

Specify the C# type for each column:

```
int, float, double, long, bool, string, enum,
vector2, vector3, color, sprite, prefab, asset,
int[], float[], string[], bool[]
```

If omitted, types are inferred from the data values.

### Flags (Row 3)

Flags control code generation behavior:

| Flag | Effect |
|------|--------|
| `key` | Marks the column as the primary key |
| `name` | Marks the column as the display name |
| `optional` | Field may be empty |
| `skip` | Column is excluded from generation |
| `list` | Forces List type instead of array |
| `hide` | Adds `[HideInInspector]` attribute |
| `range(min;max)` | Adds `[Range(min, max)]` attribute |
| `tooltip(text)` | Adds `[Tooltip("text")]` attribute |
| `header(text)` | Adds `[Header("text")]` attribute |
| `multiline(N)` | Adds `[Multiline(N)]` attribute |

Multiple flags can be combined with the pipe character: `optional|range(0;100)|tooltip(Health points)`

### Enum Columns

For enum columns, the flags row contains the enum values instead of flags:

```csv
id,rarity
int,enum
,Common,Rare,Epic,Legendary
1,Common
```

## Directives

Directives control class and namespace naming. They must appear before the header row:

```csv
#class:WeaponData
#database:WeaponDatabase
#namespace:MyGame.Data
```

| Directive | Purpose | Default |
|-----------|---------|---------|
| `#class:` | Entry class name | `GeneratedEntry` |
| `#database:` | Database class name | `{ClassName}Database` |
| `#namespace:` | C# namespace | None |

## Validation

The CSV validator performs two levels of validation:

1. **Quick validation** -- Checks file existence, extension, and read access.
2. **Full validation** -- Parses the CSV, builds a schema, and reports warnings for empty headers, duplicate columns, and unrecognized type hints.
