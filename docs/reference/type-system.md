# Type System

## Resolved Types

The `ResolvedType` enum defines all supported data types:

| Type | C# Output | CSV Type Hint | Example Value |
|------|-----------|---------------|---------------|
| `Int` | `int` | `int` | `42` |
| `Float` | `float` | `float` | `3.14` |
| `Double` | `double` | `double` | `3.14159265` |
| `Long` | `long` | `long` | `9999999999` |
| `Bool` | `bool` | `bool` | `true`, `false` |
| `String` | `string` | `string` | `Hello World` |
| `Enum` | Generated enum | `enum` | `Common` |
| `Vector2` | `Vector2` | `vector2` | `(1,2)` |
| `Vector3` | `Vector3` | `vector3` | `(1,2,3)` |
| `Color` | `Color` | `color` | `#FF0000` |
| `Sprite` | `Sprite` | `sprite` | Asset path |
| `Prefab` | `GameObject` | `prefab` | Asset path |
| `Asset` | `ScriptableObject` | `asset` | Asset reference |
| `IntArray` | `int[]` or `List<int>` | `int[]` | `1;2;3` |
| `FloatArray` | `float[]` or `List<float>` | `float[]` | `1.0;2.0` |
| `StringArray` | `string[]` or `List<string>` | `string[]` | `a;b;c` |
| `BoolArray` | `bool[]` or `List<bool>` | `bool[]` | `true;false` |

## Type Inference

When type hints are not provided (1-row header mode), types are inferred from data values:

1. If all non-empty values parse as `int`, the column is `Int`.
2. If all non-empty values parse as `float`, the column is `Float`.
3. If all non-empty values are `true` or `false`, the column is `Bool`.
4. Otherwise, the column is `String`.

## Array Delimiter

Array values use semicolons (`;`) as the default delimiter. This is configurable via `GenerationSettings.ArrayDelimiter`.

## SQLite Type Mapping

SQLite types are mapped using affinity rules. See the [SQLite Import](../guides/sqlite-import.md) guide for the full mapping table.
