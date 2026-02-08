# API Reference

## Core Classes

### CSVReader

Static class for parsing CSV text into `RawTableData`.

```csharp
// Parse CSV text with specified options
RawTableData result = CSVReader.Parse(
    csvText,        // string: CSV content
    delimiter,      // string: Column delimiter ("," or "auto")
    commentPrefix,  // string: Comment line prefix ("#")
    headerRowCount  // int: Number of header rows (1, 2, or 3)
);

// Read from file path
RawTableData result = CSVReader.ReadFromFile(filePath, ",", "#", 3);
```

### SchemaBuilder

Static class for building `TableSchema` from `RawTableData`.

```csharp
TableSchema schema = SchemaBuilder.Build(rawData, settings);
```

### CodeGenerator

Static class for generating C# code from schemas.

```csharp
// Generate and write files to disk
CodeGenerationResult result = CodeGenerator.Generate(schema, settings);

// Generate entry + database class code as a string
string code = CodeGenerator.GenerateScriptableObject(schema, settings);

// Generate only the database class code
string dbCode = CodeGenerator.GenerateDatabaseClass(schema, settings);
```

### GoogleSheetsURLParser

Static class for parsing Google Sheets URLs.

```csharp
GoogleSheetsParseResult result = GoogleSheetsURLParser.Parse(urlOrId);
// result.IsValid, result.SpreadsheetId, result.Gid, result.Error

string exportUrl = GoogleSheetsURLParser.BuildExportURL(spreadsheetId, gid);
```

## Validation Classes

### CSVValidator

Implements `ISourceReader` for CSV file validation.

```csharp
var validator = new CSVValidator(filePath, settings);

SourceValidationResult quick = validator.ValidateQuick();
validator.ValidateFull(result => { /* handle result */ });
TableSchema[] schemas = validator.ExtractSchemas();
```

### SQLiteValidator

Implements `ISourceReader` for SQLite database validation.

```csharp
var validator = new SQLiteValidator(filePath, settings);

SourceValidationResult quick = validator.ValidateQuick();
validator.ValidateFull(result => { /* handle result */ });
TableSchema[] schemas = validator.ExtractSchemas();
```

## ISourceReader Interface

```csharp
public interface ISourceReader
{
    SourceValidationResult ValidateQuick();
    void ValidateFull(Action<SourceValidationResult> onComplete);
    TableSchema[] ExtractSchemas();
}
```

## Data Models

### RawTableData

Raw parsed CSV data before schema building.

| Property | Type | Description |
|----------|------|-------------|
| `Directives` | `string[]` | Parsed directives from prelude |
| `Headers` | `string[]` | Column header names |
| `TypeHints` | `string[]` | Type hint row (nullable) |
| `Flags` | `string[]` | Flags row (nullable) |
| `DataRows` | `string[][]` | Data rows |

### TableSchema

Complete schema for a single table.

| Property | Type | Description |
|----------|------|-------------|
| `ClassName` | `string` | Entry class name |
| `DatabaseName` | `string` | Database class name |
| `NamespaceName` | `string` | C# namespace |
| `SourceTableName` | `string` | Original table/file name |
| `Columns` | `ColumnSchema[]` | Column definitions |
| `Rows` | `List<Dictionary<string, string>>` | Data rows keyed by FieldName |
| `Warnings` | `List<ValidationWarning>` | Warnings generated during parsing |

### ColumnSchema

Definition for a single column.

| Property | Type | Description |
|----------|------|-------------|
| `FieldName` | `string` | Sanitized C# field name |
| `OriginalHeader` | `string` | Normalized header (after empty/duplicate handling) |
| `Type` | `ResolvedType` | Resolved data type |
| `IsKey` | `bool` | Primary key flag |
| `IsName` | `bool` | Display name flag |
| `IsOptional` | `bool` | Optional/nullable flag |
| `IsSkipped` | `bool` | Excluded from generation |
| `IsList` | `bool` | Force List type |
| `EnumValues` | `string[]` | Enum member names |
| `DefaultValue` | `string` | Default value |
| `Attributes` | `Dictionary<string, string>` | Code generation attributes |
| `IsForeignKey` | `bool` | SQLite foreign key flag |
| `ForeignKeyTable` | `string` | FK target table name |
| `ForeignKeyColumn` | `string` | FK target column name |
| `ForeignKeySoType` | `string` | FK target ScriptableObject type name |
