# Data to ScriptableObject — V1 Agent Implementation Guide

## Project Overview

Unity Editor tool (UPM package) that converts CSV files and Google Sheets into typed ScriptableObject assets. Editor-only — no runtime dependencies ship with the game.

**Target Unity Version:** 2021.3+ LTS
**License:** MIT
**Package Name:** `com.data-to-scriptableobject`
**Distribution:** Unity Package Manager via Git URL

---

## Repository Structure

```
DataToScriptableObject/
├── package.json
├── LICENSE
├── README.md
├── CHANGELOG.md
├── Editor/
│   ├── DataToScriptableObject.Editor.asmdef
│   ├── Core/
│   │   ├── Interfaces/
│   │   │   └── ISourceReader.cs
│   │   ├── CSVReader.cs
│   │   ├── GoogleSheetsReader.cs
│   │   ├── GoogleSheetsURLParser.cs
│   │   ├── SchemaBuilder.cs
│   │   ├── TypeInference.cs
│   │   ├── TypeConverters.cs
│   │   ├── CodeGenerator.cs
│   │   ├── ReflectionMapper.cs
│   │   ├── AssetPopulator.cs
│   │   ├── RecompileHandler.cs
│   │   └── NameSanitizer.cs
│   ├── UI/
│   │   ├── ImporterWindow.cs
│   │   ├── PreviewTable.cs
│   │   ├── ColumnMappingUI.cs
│   │   ├── SettingsPanel.cs
│   │   └── ImportReporter.cs
│   ├── Validation/
│   │   ├── CSVValidator.cs
│   │   └── GoogleSheetsValidator.cs
│   └── Utility/
│       └── PendingImportState.cs
├── Models/
│   ├── DataToScriptableObject.Models.asmdef
│   ├── RawTableData.cs
│   ├── TableSchema.cs
│   ├── ColumnSchema.cs
│   ├── ResolvedType.cs
│   ├── FieldMapping.cs
│   ├── GenerationSettings.cs
│   ├── PopulationSettings.cs
│   ├── ImportResult.cs
│   ├── SourceValidationResult.cs
│   └── GoogleSheetsParseResult.cs
├── Samples~/
│   └── CSV/
│       ├── items_example.csv
│       ├── enemies_example.csv
│       └── README.md
└── Tests/
    └── Editor/
        ├── DataToScriptableObject.Tests.Editor.asmdef
        ├── CSVReaderTests.cs
        ├── SchemaBuilderTests.cs
        ├── TypeInferenceTests.cs
        ├── TypeConverterTests.cs
        ├── CodeGeneratorTests.cs
        ├── NameSanitizerTests.cs
        └── GoogleSheetsURLParserTests.cs
```

---

## Assembly Definitions

1. **`DataToScriptableObject.Models.asmdef`** in `Models/` — no Unity Editor references, contains all data structures. No platform restrictions.
2. **`DataToScriptableObject.Editor.asmdef`** in `Editor/` — references `Models` asmdef, includes `UnityEditor` references, marked as Editor-only platform. Set `includePlatforms` to `["Editor"]`.
3. **`DataToScriptableObject.Tests.Editor.asmdef`** in `Tests/Editor/` — references both above, includes `UnityEngine.TestRunner` and `UnityEditor.TestRunner`. Set `includePlatforms` to `["Editor"]`. Set `optionalUnityReferences` to `["TestAssemblies"]`.

---

## package.json

```json
{
  "name": "com.data-to-scriptableobject",
  "version": "0.1.0",
  "displayName": "Data to ScriptableObject",
  "description": "Convert CSV files and Google Sheets into typed ScriptableObject assets with automatic code generation.",
  "unity": "2021.3",
  "keywords": ["csv", "google-sheets", "scriptableobject", "database", "import", "codegen"],
  "license": "MIT",
  "author": {
    "name": "SBUplakankus"
  },
  "samples": [
    {
      "displayName": "CSV Examples",
      "description": "Example CSV files demonstrating the multi-row header convention.",
      "path": "Samples~/CSV"
    }
  ]
}
```

---

## Implementation Order

Build in this exact order. Each phase should compile and be testable before moving to the next.

### Phase 1: Models and Data Structures

Create all model classes first. Everything else depends on these.

**File: `Models/ResolvedType.cs`**
```csharp
namespace DataToScriptableObject
{
    public enum ResolvedType
    {
        Int,
        Float,
        Double,
        Long,
        Bool,
        String,
        Enum,
        Vector2,
        Vector3,
        Color,
        Sprite,
        Prefab,
        Asset,
        IntArray,
        FloatArray,
        StringArray,
        BoolArray
    }
}
```

**File: `Models/ColumnSchema.cs`**
```csharp
using System.Collections.Generic;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class ColumnSchema
    {
        public string fieldName;            // sanitized C# field name
        public string originalHeader;       // raw CSV header name
        public ResolvedType resolvedType;
        public bool isKey;
        public bool isName;
        public bool isOptional;
        public bool isSkipped;
        public bool isList;                 // List<T> instead of T[]
        public string[] enumValues;         // populated if resolvedType == Enum
        public string defaultValue;
        public Dictionary<string, string> attributes; // "range" => "0,100", "tooltip" => "Base damage"

        // Reserved for v2 SQLite support — leave these fields in place
        public bool isForeignKey;
        public string foreignKeyTable;
        public string foreignKeyColumn;
        public string foreignKeySOType;
    }
}
```

**File: `Models/TableSchema.cs`**
```csharp
using System.Collections.Generic;

namespace DataToScriptableObject
{
    [System.Serializable]
    public class TableSchema
    {
        public string className;
        public string databaseName;
        public string namespaceName;
        public string sourceTableName;      // original filename or table name
        public ColumnSchema[] columns;
        public List<Dictionary<string, string>> rows; // each row is { originalHeader → rawValue }
        public List<ValidationWarning> warnings;
    }
}
```

**File: `Models/RawTableData.cs`**
```csharp
namespace DataToScriptableObject
{
    public class RawTableData
    {
        public string[] directives;         // #class:, #database:, #namespace: lines
        public string[] headers;            // Row 1: column names
        public string[] typeHints;          // Row 2: type declarations (null if headerRowCount < 2)
        public string[] flags;              // Row 3: flags/constraints (null if headerRowCount < 3)
        public string[][] dataRows;         // All data rows after headers
    }
}
```

**File: `Models/SourceValidationResult.cs`**
```csharp
using System.Collections.Generic;

namespace DataToScriptableObject
{
    public enum SourceStatus
    {
        NotLoaded,
        Validating,
        Valid,
        InvalidSource,
        InvalidFormat,
        InvalidContent,
        InvalidSchema,
        NetworkError,
        AccessDenied,
        NotFound
    }

    public enum WarningLevel { Info, Warning, Error }

    [System.Serializable]
    public class ValidationWarning
    {
        public WarningLevel level;
        public string message;
        public string table;
        public int row;             // -1 if not row-specific
        public string column;

        public ValidationWarning()
        {
            row = -1;
        }
    }

    public class SourceValidationResult
    {
        public bool isValid;
        public SourceStatus status;
        public string errorMessage;
        public List<ValidationWarning> warnings = new List<ValidationWarning>();
        public int tableCount;
        public int totalRowCount;
        public int totalColumnCount;
        public string[] tableNames;
    }
}
```

**File: `Models/FieldMapping.cs`**
```csharp
using System.Reflection;

namespace DataToScriptableObject
{
    public class FieldMapping
    {
        public ColumnSchema column;
        public FieldInfo targetField;       // null if column is skipped or unmapped
        public bool isAutoMapped;           // true if matched by name
    }
}
```

**File: `Models/GenerationSettings.cs`**
```csharp
namespace DataToScriptableObject
{
    [System.Serializable]
    public class GenerationSettings
    {
        public string className;
        public string databaseName;
        public string namespaceName;
        public string scriptOutputPath = "Assets/Scripts/Generated/";
        public string assetOutputPath = "Assets/Data/";
        public string assetPrefix = "";
        public bool useListInsteadOfArray = true;
        public bool generateDatabaseContainer = true;
        public bool generateEnumScripts = false;
        public bool overwriteExistingAssets = true;
        public bool deleteOrphanedAssets = false;
        public bool sanitizeFieldNames = true;
        public bool useSerializeField = false;
        public bool generatePropertyAccessors = false;
        public bool generateTooltips = false;
        public bool generateCreateAssetMenu = false;
        public bool serializableClassMode = false;
        public bool addAutoGeneratedHeader = true;
        public string assetNamingColumn = null;
        public string arrayDelimiter = ";";
        public string nullToken = "null";
        public string commentPrefix = "#";
        public int headerRowCount = 3;
    }
}
```

**File: `Models/PopulationSettings.cs`**
```csharp
namespace DataToScriptableObject
{
    [System.Serializable]
    public class PopulationSettings
    {
        public string assetOutputPath;
        public string assetPrefix;
        public string assetNamingColumn;
        public bool overwriteExisting;
        public bool deleteOrphaned;
    }
}
```

**File: `Models/ImportResult.cs`**
```csharp
using System.Collections.Generic;

namespace DataToScriptableObject
{
    public class ImportResult
    {
        public int created;
        public int updated;
        public int skipped;
        public int deleted;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public List<string> createdAssetPaths = new List<string>();
        public string generatedScriptPath;
        public string databaseAssetPath;
    }
}
```

**File: `Models/GoogleSheetsParseResult.cs`**
```csharp
namespace DataToScriptableObject
{
    public class GoogleSheetsParseResult
    {
        public bool isValid;
        public string spreadsheetId;
        public string gid;
        public string error;
    }
}
```

---

### Phase 2: CSV Reader and Core Parsing

Pure C# string processing. No Unity Editor dependencies in CSVReader itself.

**File: `Editor/Core/Interfaces/ISourceReader.cs`**
```csharp
namespace DataToScriptableObject.Editor
{
    public interface ISourceReader
    {
        SourceValidationResult ValidateQuick();
        void ValidateFull(System.Action<SourceValidationResult> onComplete);
        TableSchema[] ExtractSchemas();
    }
}
```

**CSVReader.cs specification:**
- Public static methods — no instance state needed.
- `RawTableData Parse(string csvText, string delimiter, string commentPrefix, int headerRowCount)` — main parse method accepting raw CSV text.
- `RawTableData ReadFromFile(string filePath, string delimiter, string commentPrefix, int headerRowCount)` — reads file then calls Parse.
- Delimiter parameter accepts: `","`, `";"`, `"\t"`, or `"auto"`. Auto-detect works by counting occurrences of each candidate delimiter in the first 5 non-comment, non-directive lines and choosing the delimiter that produces the most consistent column count across those lines.
- Handle RFC 4180 CSV: fields enclosed in double quotes can contain the delimiter, newlines, and escaped quotes (`""`). A quoted field starts with `"` immediately after a delimiter (or start of line) and ends at the next `"` that is followed by a delimiter, end of line, or end of file.
- Strip UTF-8 BOM (`\uFEFF`) from the start of text if present.
- Normalize line endings: replace `\r\n` and `\r` with `\n` before processing.
- Parse directive lines: any line before the first non-directive, non-comment, non-empty line that matches `#class:VALUE`, `#database:VALUE`, or `#namespace:VALUE`. Extract the value after the colon, trimmed. Store in `RawTableData.directives` as the full line (e.g., `"#class:ItemEntry"`).
- After directives, skip empty lines and comment lines (lines starting with `commentPrefix` or `//`).
- The next non-skipped line is Row 1 (headers). If `headerRowCount >= 2`, the next line is Row 2 (type hints). If `headerRowCount >= 3`, the next line is Row 3 (flags).
- All remaining non-comment, non-empty lines are data rows.
- If a data row has fewer columns than the header, pad with empty strings. If more columns, truncate to header length. Add a warning for either case.

**SchemaBuilder.cs specification:**
- `TableSchema Build(RawTableData rawData, GenerationSettings settings)`.
- Extract `className`, `databaseName`, `namespaceName` from directives. Fallback: `className` defaults to PascalCase of source filename (strip extension, replace spaces/underscores with PascalCase boundaries). `databaseName` defaults to `{className}Database`. `namespaceName` defaults to empty string.
- For each column header, create a `ColumnSchema`.
- If `headerRowCount >= 2` and `typeHints` is not null: map each type hint string to a `ResolvedType`. Recognized strings (case-insensitive): `int`, `float`, `double`, `long`, `bool`, `string`, `enum`, `vector2`, `vector3`, `color`, `sprite`, `prefab`, `asset`, `int[]`, `float[]`, `string[]`, `bool[]`. Unrecognized type → add warning, default to `String`.
- If `headerRowCount < 2` or `typeHints` is null: run `TypeInference.Infer()` on each column's data values to determine type.
- If `headerRowCount >= 3` and `flags` is not null: parse each flag cell. Known flags:
  - `key` — set `isKey = true`.
  - `name` — set `isName = true`.
  - `optional` — set `isOptional = true`.
  - `skip` — set `isSkipped = true`.
  - `list` — set `isList = true`.
  - `hide` — add `"hide" => ""` to attributes.
  - `header(...)` — add `"header" => value` to attributes.
  - `tooltip(...)` — add `"tooltip" => value` to attributes.
  - `range(min;max)` — add `"range" => "min,max"` to attributes. Note semicolon separator inside parens.
  - If column type is `enum`, the flags cell contains comma-separated enum values (e.g., `"Fire,Water,Earth"` — this arrives as a single string because it's a quoted CSV field). Split by comma, trim, store in `enumValues`.
  - Multiple flags can appear in one cell separated by `|`: `key|tooltip(Primary ID)`.
  - Empty flags cell → no flags for that column.
- Sanitize field names using `NameSanitizer` if `settings.sanitizeFieldNames` is true.
- Populate `rows` as `List<Dictionary<string, string>>` where each dictionary maps `originalHeader → raw cell value` for that row.
- Collect warnings: duplicate headers (append `_2`, `_3` etc.), empty headers (name as `column_0`, `column_1`), unrecognized types, data values that don't match declared type.

**TypeInference.cs specification:**
- `ResolvedType Infer(string[] values)` — accepts all values from one column across all data rows.
- Ignore empty strings and null tokens when inferring.
- If ALL non-empty values are empty → return `String`.
- Try parse in order: `int` → `float` → `bool` → `string`. A type is selected only if every non-empty value successfully parses to that type.
- `int`: `int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)`.
- `float`: `float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)`.
- `bool`: value is one of (case-insensitive) `true`, `false`, `yes`, `no`, `1`, `0`.
- `string` is the final fallback — always succeeds.
- Do NOT attempt Vector2/Vector3 inference in auto-detect mode (too ambiguous with comma delimiters). Those types are only available via explicit type hints in Row 2.

**NameSanitizer.cs specification:**
- `string SanitizeFieldName(string raw)` — for C# field names.
- `string SanitizeFileName(string raw)` — for `.asset` file names.
- Field name rules:
  - Trim whitespace.
  - Replace hyphens and spaces with camelCase boundaries: `"max health"` → `"maxHealth"`, `"first-name"` → `"firstName"`.
  - Convert snake_case to camelCase: `"player_name"` → `"playerName"`.
  - Strip all characters that are not `[a-zA-Z0-9_]`.
  - If first character is a digit, prefix with `_`.
  - If result is empty after sanitization, return `"field"`.
  - Ensure first character is lowercase (camelCase).
  - Check against C# reserved keywords: `abstract, as, base, bool, break, byte, case, catch, char, checked, class, const, continue, decimal, default, delegate, do, double, else, enum, event, explicit, extern, false, finally, fixed, float, for, foreach, goto, if, implicit, in, int, interface, internal, is, lock, long, namespace, new, null, object, operator, out, override, params, private, protected, public, readonly, ref, return, sbyte, sealed, short, sizeof, stackalloc, static, string, struct, switch, this, throw, true, try, typeof, uint, ulong, unchecked, unsafe, ushort, using, virtual, void, volatile, while`. If collision, append `_`.
- File name rules:
  - Replace spaces with `_`.
  - Strip characters not valid in file paths: `< > : " / \ | ? *`.
  - Trim to 64 characters max.
  - If empty after sanitization, return `"unnamed"`.

---

### Phase 3: Code Generator

**CodeGenerator.cs specification:**
- `CodeGenerationResult Generate(TableSchema schema, GenerationSettings settings)`.
- `CodeGenerationResult` contains: `string entryClassCode`, `string databaseClassCode`, `List<EnumDefinition> enumDefinitions`, `string entryClassPath`, `string databaseClassPath`.
- Use `StringBuilder` for all code construction.
- Generate entry class file content:

```
// ─────────────────────────────────────────────────────────────
// Auto-generated by Data to ScriptableObject
// This file can be modified. Re-imports match fields by name.
// ─────────────────────────────────────────────────────────────
```

- If `namespaceName` is not empty, wrap in `namespace { }`.
- Add `using UnityEngine;` and `using System.Collections.Generic;` at the top (outside namespace if no namespace, inside if namespaced).
- If `generateCreateAssetMenu`: add `[CreateAssetMenu(fileName = "New{className}", menuName = "Data/{className}")]`.
- Class declaration: `public class {className} : ScriptableObject` (unless `serializableClassMode` — then `[System.Serializable] public class {className}`).
- For each non-skipped column:
  - If `generateTooltips` and column has `tooltip` attribute: add `[Tooltip("{value}")]`.
  - If column has `range` attribute: add `[Range({min}f, {max}f)]`.
  - If column has `hide` attribute: add `[HideInInspector]`.
  - If column has `header` attribute: add `[Header("{value}")]`.
  - If `useSerializeField`: `[SerializeField] private {type} {fieldName};` followed by `public {Type} {PascalName} => {fieldName};` if `generatePropertyAccessors`.
  - Else: `public {type} {fieldName};`.
  - Type string mapping: `Int` → `int`, `Float` → `float`, `Double` → `double`, `Long` → `long`, `Bool` → `bool`, `String` → `string`, `Enum` → generated enum name (PascalCase of column field name), `Vector2` → `Vector2`, `Vector3` → `Vector3`, `Color` → `Color`, `Sprite` → `Sprite`, `Prefab` → `GameObject`, `Asset` → `ScriptableObject`.
  - For array types: if `isList` or `settings.useListInsteadOfArray` → `List<{elementType}>`, else `{elementType}[]`.
- Generate database class: `public class {databaseName} : ScriptableObject` with `public List<{className}> entries;`. Always add `[CreateAssetMenu]`.
- If `serializableClassMode`: put both classes in one file. The entry class is `[System.Serializable]` (not SO), the database class is the SO with the list.
- For each enum column (if `generateEnumScripts` is true OR if inline): generate enum definition. If `generateEnumScripts`, write to separate file. Otherwise, include above the class in the same file.

```csharp
public enum {PascalCaseFieldName}
{
    {Value1},
    {Value2},
    {Value3}
}
```

- Write files using `System.IO.File.WriteAllText()`. Ensure output directory exists (`Directory.CreateDirectory`).
- After writing, call `AssetDatabase.Refresh()`.

---

### Phase 4: Recompile Handler and Pending State

**PendingImportState.cs specification:**
- Static class with static methods.
- `Save(PendingImportData data)` — serialize `data` to JSON via `JsonUtility.ToJson()`, store in `EditorPrefs` with key `"DataToSO_PendingImport"`.
- `PendingImportData Load()` — read from EditorPrefs, deserialize.
- `void Clear()` — delete the EditorPrefs key.
- `bool HasPending()` — check if the key exists and is not empty.
- `PendingImportData` is a `[Serializable]` class containing: `string sourceFilePath`, `string cachedCSVText` (for Sheets), `string generationSettingsJson`, `string tableSchemaJson`, `string generatedTypeName`, `string generatedTypeNamespace`, `long timestampTicks`.

**RecompileHandler.cs specification:**
- Static class with `[InitializeOnLoadMethod]` static method.
- On editor load: check `PendingImportState.HasPending()`.
- If pending: load the state, find the generated type by iterating all loaded assemblies: `AppDomain.CurrentDomain.GetAssemblies()`, then `assembly.GetTypes()`, find type matching `generatedTypeName` (with namespace if set).
- If type found: reconstruct `TableSchema` from JSON, create `FieldMapping[]` via `ReflectionMapper`, run `AssetPopulator.Populate()`, call `PendingImportState.Clear()`, log results.
- If type NOT found: log error `"Could not find generated type '{name}' after recompile. Was the script deleted?"`, clear state.
- Wrap everything in try/catch. On exception: log error, clear state (don't leave stale pending data).

---

### Phase 5: Reflection Mapper

**ReflectionMapper.cs specification:**
- `FieldMapping[] Map(TableSchema schema, System.Type targetType)`.
- Get all fields: `targetType.GetFields(BindingFlags.Public | BindingFlags.Instance)` plus `targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)` filtered to only those with `[SerializeField]` attribute.
- For each non-skipped column in the schema, find a matching field:
  1. Exact name match: `column.fieldName == field.Name`.
  2. Case-insensitive: `column.fieldName.ToLower() == field.Name.ToLower()`.
  3. Normalized: strip underscores and compare lowercase. `max_health` matches `maxHealth` because both normalize to `maxhealth`.
- If matched, create `FieldMapping` with `isAutoMapped = true`.
- If no match, create `FieldMapping` with `targetField = null`, add warning: `"Column '{originalHeader}' has no matching field on type '{typeName}'"`.
- For fields on the type that have no matching column, add info warning: `"Field '{fieldName}' on type '{typeName}' has no matching column and will keep its default value"`.

---

### Phase 6: Asset Populator

**AssetPopulator.cs specification:**
- `ImportResult Populate(TableSchema schema, System.Type type, FieldMapping[] mappings, PopulationSettings settings)`.
- Ensure output directory exists.
- Track existing assets in the output directory (for overwrite and orphan detection).
- For each row in `schema.rows`:
  1. `var instance = ScriptableObject.CreateInstance(type);`
  2. For each mapping where `targetField != null`:
     - Get raw value: `row[mapping.column.originalHeader]`.
     - If value equals `settings.nullToken` and column `isOptional` and field type is a reference type: set null.
     - If value is empty: use default for the type (don't set — Unity default is fine).
     - Otherwise: convert using `TypeConverters` and set via `mapping.targetField.SetValue(instance, convertedValue)`.
     - Catch conversion exceptions: add warning, skip that field.
  3. Determine asset name: use `assetNamingColumn` value from the row, or key column value, or row index. Apply prefix. Sanitize with `NameSanitizer.SanitizeFileName()`. Handle duplicates by appending `_1`, `_2`.
  4. Construct path: `{assetOutputPath}/{assetPrefix}{sanitizedName}.asset`.
  5. Check if asset exists at path:
     - If exists AND `overwriteExisting`: load existing with `AssetDatabase.LoadAssetAtPath`, copy field values from instance to existing via `EditorUtility.CopySerialized(instance, existing)`, call `EditorUtility.SetDirty(existing)`. Increment `updated`. Destroy the temp instance.
     - If exists AND NOT `overwriteExisting`: skip. Increment `skipped`.
     - If not exists: `AssetDatabase.CreateAsset(instance, path)`. Increment `created`.
  6. Track this asset path.
- After all rows:
  - If `generateDatabaseContainer` (check from original GenerationSettings — pass as bool in PopulationSettings or expand the class): create or load the database SO at `{assetOutputPath}/{databaseName}.asset`. Populate its `entries` list field via reflection with all created/updated entry assets. `SetDirty`.
  - If `deleteOrphaned`: find all `.asset` files in the output directory of the entry type. Delete any whose path is not in the tracked list. Increment `deleted`.
- `AssetDatabase.SaveAssets()`.
- `AssetDatabase.Refresh()`.
- Return `ImportResult`.

**TypeConverters.cs specification:**
- All methods are static. All catch exceptions internally and return default + log warning.
- Always use `System.Globalization.CultureInfo.InvariantCulture` for numeric parsing.
- `int ToInt(string value)` — `int.TryParse` → fallback `0`.
- `float ToFloat(string value)` — `float.TryParse` with `NumberStyles.Float | NumberStyles.AllowLeadingSign`, `InvariantCulture` → fallback `0f`.
- `double ToDouble(string value)` — same pattern → fallback `0.0`.
- `long ToLong(string value)` → fallback `0L`.
- `bool ToBool(string value)` — accept (case-insensitive): `true/false/yes/no/1/0` → fallback `false`.
- `string ToString(string value, string nullToken)` — if value equals `nullToken` return `null`, else return value as-is. Empty string returns `""`.
- `Vector2 ToVector2(string value)` — strip parentheses, split by `,`, parse two floats. Fallback `Vector2.zero`.
- `Vector3 ToVector3(string value)` — strip parentheses, split by `,`, parse three floats. Fallback `Vector3.zero`.
- `Color ToColor(string value)` — try `ColorUtility.TryParseHtmlString` for hex formats. If that fails, try splitting by `,` for RGBA (values 0-255 divide by 255f, or if all <= 1.0 treat as 0-1 range). Fallback `Color.white`.
- `object ToEnum(string value, System.Type enumType)` — `System.Enum.TryParse(enumType, value, true, out result)`. Fallback: first enum value.
- `T[] ToArray<T>(string value, string delimiter, System.Func<string, T> elementConverter)` — split by delimiter, trim each element, convert each, return array.
- `List<T> ToList<T>(string value, string delimiter, System.Func<string, T> elementConverter)` — same as array but return `List<T>`.
- `UnityEngine.Object ToAssetReference(string value, System.Type assetType)` — `AssetDatabase.LoadAssetAtPath(value, assetType)`. If null, try adding common extensions (`.png`, `.prefab`, `.asset`). If still null, warn. This is Editor-only.

---

### Phase 7: Source Validators (CSV + Google Sheets)

**CSVValidator.cs specification:**
- Implements `ISourceReader`.
- Constructor takes `string filePath` and `GenerationSettings settings`.
- `ValidateQuick()`:
  - Check `File.Exists(filePath)`. If not → status `InvalidSource`, message "File not found: {path}".
  - Check extension is `.csv`, `.tsv`, or `.txt`. If not → status `InvalidFormat`, message "Unsupported file type: .{ext}. Expected .csv, .tsv, or .txt".
  - Return `Valid` status (full content not checked yet).
- `ValidateFull(Action<SourceValidationResult> onComplete)`:
  - Try `File.ReadAllText` — catch `IOException` → "Cannot read file. Is it open in another application?", catch `UnauthorizedAccessException` → "Permission denied reading file.".
  - Check not empty.
  - Parse with `CSVReader.Parse()`.
  - Build schema with `SchemaBuilder.Build()`.
  - Collect all warnings from parsing and schema building.
  - Call `onComplete` synchronously (no async for local files).
- `ExtractSchemas()` → return single-element array with the built `TableSchema`.

**GoogleSheetsValidator.cs specification:**
- Implements `ISourceReader`.
- Constructor takes `string url` and `GenerationSettings settings`.
- Stores cached CSV text and fetch timestamp.
- `ValidateQuick()`:
  - Parse URL with `GoogleSheetsURLParser.Parse(url)`.
  - If parse fails → status `InvalidSource`, message from parser.
  - If parse succeeds → status `Valid` (data not fetched yet — `ValidateFull` does that).
- `ValidateFull(Action<SourceValidationResult> onComplete)`:
  - Check `Application.internetReachability != NetworkReachability.NotReachable`. If not reachable → status `NetworkError`, message "No internet connection detected. Use a local CSV file or check your network.".
  - Construct export URL via `GoogleSheetsURLParser.BuildExportURL(id, gid)`.
  - Create `UnityWebRequest.Get(exportURL)`. Set timeout to 10 seconds.
  - Send request. Use synchronous wait loop:
    ```csharp
    var request = UnityWebRequest.Get(url);
    request.timeout = 10;
    var operation = request.SendWebRequest();
    while (!operation.isDone)
    {
        EditorUtility.DisplayProgressBar("Fetching Google Sheet",
            "Downloading...", operation.progress);
    }
    EditorUtility.ClearProgressBar();
    ```
  - Handle response:
    - `request.result == ConnectionError` → status `NetworkError`, message "Network error: {request.error}".
    - `request.result == ProtocolError`:
      - 400 → "Bad request. Check the spreadsheet URL."
      - 403 → "Access denied. The sheet is private or not published. In Google Sheets, go to File > Share > Publish to Web, select CSV format, click Publish."
      - 404 → "Spreadsheet not found. It may have been deleted or the URL is incorrect."
      - 429 → "Rate limited by Google. Wait a moment and try again."
      - 500+ → "Google Sheets is temporarily unavailable. Try again later."
    - `request.result == Success`:
      - Check for HTML trap: if response body starts with `<!DOCTYPE` or `<html` (case-insensitive, after trimming) → status `AccessDenied`, message "Received an HTML page instead of CSV data. The sheet is likely not published. In Google Sheets: File > Share > Publish to Web."
      - Check not empty.
      - Cache the response text and timestamp.
      - Feed into `CSVReader.Parse()` and `SchemaBuilder.Build()`.
      - Collect warnings.
      - Call `onComplete`.
  - Dispose request.
- `ExtractSchemas()` → parse cached CSV text, return schema.

**GoogleSheetsURLParser.cs specification:**
- Static class with static methods.
- `GoogleSheetsParseResult Parse(string input)`:
  - If input is null or whitespace → error "URL is empty".
  - Trim input.
  - Try regex: `docs\.google\.com/spreadsheets/d/([a-zA-Z0-9_-]+)` → extract group 1 as spreadsheetId.
  - Try regex for GID: `[?&#]gid=(\d+)` or `#gid=(\d+)` → extract as gid. Default gid is `"0"`.
  - If no URL match, try raw ID regex: `^[a-zA-Z0-9_-]{20,}$` → treat as spreadsheetId, gid `"0"`.
  - If nothing matches → error "Not a valid Google Sheets URL. Expected format: https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit".
- `string BuildExportURL(string spreadsheetId, string gid)` → return `$"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}"`.

---

### Phase 8: Editor Window UI

**ImporterWindow.cs specification:**
- `EditorWindow` subclass. Menu item at `Tools/Data to ScriptableObject`.
- Window title: "Data to ScriptableObject". Minimum size: 450x600.
- Store all settings in an instance of `GenerationSettings`. Save/load to `EditorPrefs` as JSON on window open/close.
- Track active `ISourceReader`, `SourceValidationResult`, `TableSchema[]`.

Layout (top to bottom, using `EditorGUILayout`):

**Source Section** (boxed):
- `SourceType` enum popup: `CSV File`, `Google Sheets`, `SQLite Database` (SQLite option present but disabled with tooltip "Coming in v2").
- If CSV: `EditorGUILayout.ObjectField` for TextAsset. Also a string field for manual path entry with a folder browse button.
- If Google Sheets: `EditorGUILayout.TextField` for URL. Button `"🔄 Fetch"` next to it. Label showing "Last fetched: {time}" or "Not fetched yet".
- Delimiter dropdown: `Auto`, `Comma`, `Semicolon`, `Tab`.
- On source change → create appropriate `ISourceReader`, run `ValidateQuick()`, and for CSV also `ValidateFull()`.

**Status Bar**:
- Colored `HelpBox` based on `SourceStatus`:
  - `NotLoaded` → `MessageType.Info`, "Select a source to begin."
  - `Validating` → `MessageType.Info`, "Fetching data..."
  - `Valid` → `MessageType.Info`, "✓ Ready. {rows} rows, {cols} columns."
  - `InvalidSource` → `MessageType.Error`, error message.
  - `NetworkError` → `MessageType.Error`, error message.
  - `AccessDenied` → `MessageType.Warning`, error message.
  - etc.

**Generation Mode Section** (boxed):
- Radio toggle: `Generate New Class` / `Use Existing Class`.
- If Generate New: text fields for Class Name, Database Name, Namespace, Script Output Path (with folder picker).
- If Use Existing: `ObjectField` for `MonoScript`. On assignment, extract type, run `ReflectionMapper.Map()`, show mapping results.

**Asset Settings Section** (boxed):
- Folder picker for Asset Output Path.
- Dropdown for Asset Naming: `From key column`, `From name column`, `From specific column...` (shows column picker), `Row index`.
- Text field for Asset Prefix.

**Options Section** (foldout):
- All boolean toggles from `GenerationSettings`, each as `EditorGUILayout.Toggle`.
- Group them logically:
  - Code Generation: `useListInsteadOfArray`, `generateDatabaseContainer`, `generateEnumScripts`, `serializableClassMode`, `useSerializeField`, `generatePropertyAccessors`, `generateTooltips`, `generateCreateAssetMenu`, `addAutoGeneratedHeader`.
  - Import Behavior: `overwriteExistingAssets`, `deleteOrphanedAssets`, `sanitizeFieldNames`.
  - Advanced (sub-foldout): `headerRowCount` (IntSlider 1-3), `arrayDelimiter`, `nullToken`, `commentPrefix`.

**Preview Section** (foldout, only shown when schema is valid):
- Scrollable table showing each column: Field Name | Type | Flags | Sample Value | Override dropdown.
- Override dropdown lets user change the detected type for any column.
- Show count: "{N} columns, {M} data rows, {W} warnings".

**Warnings Section** (only shown if warnings exist):
- Scrollable list of warnings with icons: ℹ for Info, ⚠ for Warning, ❌ for Error.

**Action Buttons** (bottom, always visible):
- `[Generate]` button — disabled unless `SourceStatus == Valid`. On click: run the full pipeline (CodeGenerator if new class, then AssetPopulator if type exists or set pending state for recompile).
- If Use Existing Class mode: button says `[Import]` instead (no code generation step).

---

### Phase 9: Tests

**CSVReaderTests.cs:**
```
- TestParseBasicCSV: "a,b,c\n1,2,3" → headers [a,b,c], 1 data row [1,2,3].
- TestParseSemicolonDelimiter: "a;b;c\n1;2;3" with delimiter ";".
- TestParseTabDelimiter: "a\tb\tc\n1\t2\t3" with delimiter "\t".
- TestAutoDetectComma: "a,b,c\n1,2,3" with delimiter "auto" → detects comma.
- TestAutoDetectSemicolon: "a;b;c\n1;2;3" with delimiter "auto" → detects semicolon.
- TestQuotedFieldWithComma: 'a,b,c\n1,"hello, world",3' → row 0 col 1 = "hello, world".
- TestQuotedFieldWithNewline: field containing \n inside quotes.
- TestEscapedDoubleQuotes: 'a\n"he said ""hi"""' → value is 'he said "hi"'.
- TestBOMStripped: prepend \uFEFF to CSV, verify headers are clean.
- TestCommentLinesSkipped: lines starting with # after headers are skipped.
- TestDirectivesParsed: "#class:MyClass\n#namespace:Game\na,b\n1,2" → directives populated.
- TestEmptyFile: "" → empty RawTableData.
- TestMismatchedColumns: row with fewer columns → padded. Row with more → truncated.
- TestWindowsLineEndings: \r\n handled.
- TestMacLineEndings: \r handled.
- TestEmptyRowsSkipped: blank lines between data rows are skipped.
```

**TypeInferenceTests.cs:**
```
- TestAllInts: ["1","2","42","-5"] → Int.
- TestAllFloats: ["1.5","2.7","3.14"] → Float.
- TestMixedIntFloat: ["1","2","3.5"] → Float.
- TestAllBools: ["true","false","True","FALSE"] → Bool.
- TestBoolWithYesNo: ["yes","no","Yes","NO"] → Bool.
- TestBoolWith01: ["1","0","1","0"] → Int (1 and 0 are valid ints, int wins over bool).
- TestOneBadIntFallsToFloat: ["1","2","3.5","4"] → Float.
- TestOneBadFloatFallsToString: ["1.5","abc","3.14"] → String.
- TestAllEmpty: ["","",""] → String.
- TestMixedEmptyAndInt: ["","1","","2"] → Int (empties ignored).
- TestStrings: ["hello","world"] → String.
```

**TypeConverterTests.cs:**
```
- TestToInt: "42" → 42.
- TestToIntEmpty: "" → 0.
- TestToIntInvalid: "abc" → 0.
- TestToFloat: "3.14" → 3.14f.
- TestToFloatInvariantCulture: "3.14" works regardless of system locale.
- TestToBoolTrue: "true","True","TRUE","yes","1" → all true.
- TestToBoolFalse: "false","False","no","0" → all false.
- TestToVector2: "(1.5,2.5)" → Vector2(1.5f, 2.5f).
- TestToVector2NoParen: "1.5,2.5" → Vector2(1.5f, 2.5f).
- TestToVector3: "(1,2,3)" → Vector3(1,2,3).
- TestToColorHex: "#FF5500" → Color(1, 0.333, 0, 1).
- TestToColorRGBA: "255,85,0,255" → same color.
- TestToStringNull: "null" with nullToken "null" → null.
- TestToStringEmpty: "" → "".
- TestToArrayInt: "1;2;3" → [1,2,3].
- TestToArrayString: "a;b;c" → ["a","b","c"].
```

**GoogleSheetsURLParserTests.cs:**
```
- TestParseEditURL: "https://docs.google.com/spreadsheets/d/ABC123/edit#gid=0" → id "ABC123", gid "0".
- TestParseShareURL: "https://docs.google.com/spreadsheets/d/ABC123/edit?usp=sharing" → id "ABC123", gid "0".
- TestParseShortURL: "https://docs.google.com/spreadsheets/d/ABC123/" → id "ABC123", gid "0".
- TestParseExportURL: "https://docs.google.com/spreadsheets/d/ABC123/export?format=csv&gid=12345" → id "ABC123", gid "12345".
- TestParseRawID: "ABC123DEF456GHI789JKL012" → id is the raw string, gid "0".
- TestParseGIDFromFragment: "#gid=42" in URL → gid "42".
- TestParseGIDFromQuery: "?gid=42" in URL → gid "42".
- TestInvalidURL: "https://example.com" → error.
- TestEmptyString: "" → error.
- TestNullString: null → error.
- TestBuildExportURL: id "ABC", gid "0" → correct URL.
```

**NameSanitizerTests.cs:**
```
- TestSpaceToCamel: "Max Health" → "maxHealth".
- TestSnakeToCamel: "player_name" → "playerName".
- TestAllCapsToLower: "ID" → "id".
- TestLeadingDigit: "3dModel" → "_3dModel".
- TestReservedWord: "class" → "class_".
- TestSpecialChars: "damage!!!" → "damage".
- TestHyphen: "first-name" → "firstName".
- TestEmpty: "" → "field".
- TestAlreadyCamel: "maxHealth" → "maxHealth" (unchanged).
- TestFileNameSpaces: "My Item" → "My_Item".
- TestFileNameSpecialChars: "item<1>:test" → "item1test".
- TestFileNameLength: 100-char string → truncated to 64.
```

**SchemaBuilderTests.cs:**
```
- TestBasicThreeRowHeader: full 3-row header CSV → correct ColumnSchema types and flags.
- TestSingleRowHeader: 1-row header → type inference runs.
- TestTwoRowHeader: 2-row header with types but no flags.
- TestEnumParsing: type "enum", flag "Fire,Water,Earth" → enumValues populated.
- TestRangeParsing: flag "range(0;100)" → attribute "range" = "0,100".
- TestTooltipParsing: flag "tooltip(Base damage)" → attribute "tooltip" = "Base damage".
- TestSkipColumn: flag "skip" → isSkipped true.
- TestKeyAndName: flag "key" on column A, "name" on column B → correct booleans.
- TestDirectivesOverride: #class:MyName overrides default class name.
- TestDuplicateHeaders: "a,b,a" → third header renamed to "a_2" with warning.
- TestEmptyHeader: ",name," → unnamed columns get "column_0", "column_2".
- TestUnrecognizedType: type "vectr3" → warning, defaults to String.
```

**CodeGeneratorTests.cs:**
```
- TestBasicGeneration: simple schema → valid C# that contains class declaration, fields.
- TestNamespaceWrapping: non-empty namespace → code wrapped in namespace block.
- TestSerializableClassMode: entry is [Serializable] class, not SO.
- TestListGeneration: array column with useListInsteadOfArray → "List<int>".
- TestArrayGeneration: array column without list mode → "int[]".
- TestEnumGeneration: enum column → enum declaration present.
- TestRangeAttribute: column with range → [Range(0f, 100f)] in output.
- TestTooltipAttribute: column with tooltip → [Tooltip("...")] in output.
- TestSerializeFieldMode: useSerializeField → "[SerializeField] private".
- TestCreateAssetMenu: flag on → attribute present.
- TestAutoGeneratedHeader: flag on → comment block present.
- TestDatabaseClassGeneration: database class has List<EntryType> field.
```

---

## Sample CSV File

**File: `Samples~/CSV/items_example.csv`**
```csv
#class:ItemEntry
#database:ItemDatabase
#namespace:Game.Data
id,name,description,damage,element,tags,icon,isStackable,maxStack
int,string,string,float,enum,string[],sprite,bool,int
key,name,optional,"range(0;100)","Fire,Water,Earth,None",list,,,"range(1;99)"
1,Sword,A sharp blade,10.5,Fire,melee;sharp,Assets/Samples/Icons/sword.png,false,1
2,Bow,A ranged weapon,7.2,Water,ranged;wooden,Assets/Samples/Icons/bow.png,false,1
3,Arrow,null,2.0,None,ranged;ammo,,true,64
4,Staff,A magical staff,15.0,Earth,magic;wooden,Assets/Samples/Icons/staff.png,false,1
5,Potion,Restores health,0.0,None,consumable;magic,,true,16
```

**File: `Samples~/CSV/enemies_example.csv`**
```csv
#class:EnemyEntry
#database:EnemyDatabase
id,name,health,damage,speed,isBoss
int,string,float,float,float,bool
key,name,,"range(1;50)","range(0;10)",
1,Goblin,30.0,5.0,3.5,false
2,Skeleton,45.0,8.0,2.0,false
3,Dragon,500.0,50.0,5.0,true
4,Slime,15.0,2.0,1.0,false
```

**File: `Samples~/CSV/README.md`**
````markdown
# CSV Examples

These example CSV files demonstrate the multi-row header convention used by Data to ScriptableObject.

## CSV Format

- **Row 1**: Column headers (become C# field names)
- **Row 2**: Type declarations (`int`, `string`, `float`, `bool`, `enum`, `string[]`, etc.)
- **Row 3**: Flags and constraints (`key`, `name`, `optional`, `range(min;max)`, `skip`, etc.)
- **Row 4+**: Data rows

## Directives

Optional lines before the header:
- `#class:ClassName` — name of the generated ScriptableObject class
- `#database:DatabaseName` — name of the generated database container class
- `#namespace:My.Namespace` — C# namespace for generated code

## Usage

1. Open `Tools > Data to ScriptableObject`
2. Drag a CSV file into the source field
3. Review the preview
4. Click Generate