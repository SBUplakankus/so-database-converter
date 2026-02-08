# Agent Context

Essential context for future agent sessions working on this repository.

## Project Overview

Unity Editor package (UPM) that converts CSV, Google Sheets, and SQLite databases into ScriptableObject assets with automatic C# code generation. Package name: `com.data-to-scriptableobject`. Minimum Unity version: 2021.3.

## Architecture

### Assembly Structure

- `DataToScriptableObject.Models` (Models/) -- Data classes shared between Editor and Runtime.
- `DataToScriptableObject.Editor` (Editor/) -- Editor-only code. Assembly includes `Editor` platform only to prevent shipping in builds.
- `DataToScriptableObject.Tests.Editor` (Tests/Editor/) -- NUnit tests run via Unity Test Runner in EditMode.

### Data Flow

1. **Source** (CSV file / Google Sheets URL / SQLite database)
2. **Parsing** -- CSVReader.Parse() returns RawTableData (headers, type hints, flags, data rows, directives).
3. **Schema Building** -- SchemaBuilder.Build() converts RawTableData into TableSchema (columns, types, attributes, rows).
4. **Code Generation** -- CodeGenerator.Generate() produces C# files (entry class + database class) via CodeFileBuilder.
5. **Validation** -- CSVValidator and SQLiteValidator implement ISourceReader (ValidateQuick, ValidateFull, ExtractSchemas).
6. **UI** -- ImporterWindow (Editor/UI/) orchestrates the pipeline via Tools > Data to ScriptableObject menu.

### Key Design Decisions

- SchemaBuilder is a **static class**. Always use `SchemaBuilder.Build()`, never instantiate it.
- OriginalHeader in ColumnSchema stores the **normalized** header (after empty/duplicate renaming), not the raw CSV header.
- TableSchema.Rows dictionaries are keyed by **ColumnSchema.FieldName** (sanitized), not by OriginalHeader.
- CSV directives (#class:, #database:, #namespace:) are parsed from the prelude before the first data line.
- Flags row must NOT be normalized to header count because enum values may extend beyond headers.
- BOM detection uses `csvText[0] == '\uFEFF'` (char comparison), never `string.StartsWith("\uFEFF")` which is culture-sensitive and matches any string.
- sqlite-net library (SQLite.cs, ~175KB) is embedded in Editor/Core/Dependencies/ for editor-only SQLite support.
- Google Sheets flow: parse URL -> download CSV via WebClient -> reuse CSV pipeline.

## File Layout

```
Models/                     -- Shared data models (TableSchema, ColumnSchema, GenerationSettings, etc.)
Editor/
  Core/
    CSVReader.cs            -- RFC 4180 CSV parser with custom ParseRecord
    SchemaBuilder.cs        -- Builds TableSchema from RawTableData (STATIC CLASS)
    CodeGenerator.cs        -- Generates C# ScriptableObject code
    TypeInference.cs        -- Infers types from data values
    TypeConverters.cs       -- Converts string values to typed values
    NameSanitizer.cs        -- Sanitizes identifiers (camelCase, PascalCase, reserved words)
    GoogleSheetsURLParser.cs-- Parses Google Sheets URLs, builds export URLs
    SQLiteReader.cs         -- Reads SQLite databases into TableSchema[]
    SQLiteSchemaParser.cs   -- Parses CREATE TABLE DDL for constraints
    SQLiteTypeMapper.cs     -- Maps SQLite types to ResolvedType
    DependencyResolver.cs   -- Topological sort via Kahn's algorithm
    ForeignKeyResolver.cs   -- Resolves FK values to ScriptableObject references
    ReflectionMapper.cs     -- Maps data to existing C# types via reflection
    Builders/
      CodeFileBuilder.cs    -- StringBuilder wrapper with indentation and C# structure helpers
    Interfaces/
      ISourceReader.cs      -- ValidateQuick, ValidateFull, ExtractSchemas
    Dependencies/
      SQLite.cs             -- Embedded sqlite-net library
    Constants/
      DataConstants.cs      -- Global constants
  Validation/
    CSVValidator.cs         -- ISourceReader for CSV files
    SQLiteValidator.cs      -- ISourceReader for SQLite databases
  UI/
    ImporterWindow.cs       -- EditorWindow with CSV, Google Sheets, SQLite support
Tests/Editor/               -- NUnit tests (run via Unity Test Runner, EditMode)
Samples~/                   -- CSV and SQLite example files
docs/                       -- MkDocs Material documentation site
```

## Testing

Tests are NUnit-based and run inside Unity Test Runner (EditMode). There is no `dotnet test` support because the code depends on UnityEngine and UnityEditor assemblies.

Test files follow the pattern `{ClassName}Tests.cs`. The test suite includes:
- Unit tests for each core class (CSVReader, SchemaBuilder, CodeGenerator, TypeInference, etc.)
- Integration tests (CSVReaderIntegrationTests, IntegrationTests)
- Full pipeline tests (FullPipelineTests) covering CSV -> Schema -> Code end-to-end
- Validation pipeline tests (ValidationPipelineTests) covering all validators
- Stress tests (SchemaBuilderStressTests)
- Edge case tests (CSVReaderEdgeCaseTests)

## Common Pitfalls

1. SchemaBuilder is static. `new SchemaBuilder()` will not compile.
2. CSVReader.Parse returns `string[]` for Headers. Empty strings are valid header values.
3. NormalizeHeader renames empty headers to `column_N` and duplicates to `name_N`. These renamed values become OriginalHeader.
4. The Flags row in CSV can have MORE columns than Headers (for enum values). Do not truncate it.
5. CodeGenerator.GenerateScriptableObject generates BOTH the entry class AND the database class when SerializableClassMode is false.
6. Google Sheets requires the sheet to be publicly shared. The download uses System.Net.WebClient.
7. SQLite connections must be closed in finally blocks. Use SQLiteOpenFlags.ReadOnly for validation.
