# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-02-08

### Added
- **Google Sheets Import**: Full pipeline support for importing data from Google Sheets URLs.
  - URL validation via GoogleSheetsURLParser.
  - CSV download from published sheets.
  - Schema building and code generation using the CSV pipeline.
- **CSV Generation**: CSV source type now generates ScriptableObject scripts (previously blocked).
- **SQLite Generation**: SQLite source type now generates scripts for all selected tables with dependency ordering.
- **Settings Persistence**: Importer window settings are saved/restored via EditorPrefs.
- **Validation Test Suite**: Comprehensive tests for CSVValidator and SQLiteValidator.
- **MkDocs Documentation**: Full documentation site structure using Material for MkDocs.

### Fixed
- CSVValidator: Replaced invalid `new SchemaBuilder().BuildSchema()` with `SchemaBuilder.Build()` (SchemaBuilder is a static class).
- CSVValidator.ExtractSchemas: Same static class fix applied.
- ImporterWindow: Removed "Only SQLite is implemented" blocker for CSV and Google Sheets sources.
- ImporterWindow: Fixed orphaned try/catch from GenerateAssets refactor.

### Changed
- ImporterWindow: Refactored into regions (Drawing, Validation, Generation, Utilities).
- ImporterWindow: Validation status now shows table/row counts for all source types, not just SQLite.
- Removed agent artifact markdown files from repository root (AGENT_GUIDE, FIX_SUMMARY, etc.).

## [0.2.0] - 2026-02-08

### Added - SQLite Database Support (Phase 2)
- SQLite import pipeline with full foreign key resolution.
- SQLiteTypeMapper, SQLiteSchemaParser, DependencyResolver, ForeignKeyResolver, SQLiteReader, SQLiteValidator.
- ImporterWindow with table selection UI and dependency visualization.
- Sample SQLite database with 4 relational tables.

### Tests Added
- SQLiteTypeMapperTests (19 tests), DependencyResolverTests (10 tests), SQLiteValidatorTests (8 tests).

## [0.1.1] - 2026-02-08

### Fixed
- SchemaBuilder: OriginalHeader uses normalized header for empty/duplicate columns.
- CodeGenerator: GenerateScriptableObject includes database class in output.
- CodeGenerator: Tooltip, multiline, and range attribute generation.
- NameSanitizer: Namespace sanitization for generated code.

## [0.1.0] - 2026-02-07

### Added
- Initial package structure with Unity Package Manager configuration.
- CSV parser with RFC 4180 compliance and multi-row header convention.
- Type inference, type converters, name sanitizer, schema builder.
- Code generator for ScriptableObject and database classes.
- Google Sheets URL parser.
- Sample CSV files and comprehensive test suite.
