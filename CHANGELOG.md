# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-02-08

### Added
- **Google Sheets Import**: Full pipeline -- URL validation, CSV download, schema building, and code generation.
- **CSV Generation**: CSV source now generates ScriptableObject scripts via the importer window.
- **SQLite Generation**: SQLite source generates scripts for all selected tables with dependency ordering.
- **Settings Persistence**: Importer window settings saved and restored via EditorPrefs.
- **Validation Test Suite**: Comprehensive tests for CSVValidator, SQLiteValidator, and GoogleSheetsURLParser.
- **MkDocs Documentation**: Full documentation site with Material for MkDocs theme.

### Fixed
- CSVValidator used `new SchemaBuilder().BuildSchema()` but SchemaBuilder is a static class. Replaced with `SchemaBuilder.Build()`.
- CSVValidator.ExtractSchemas had the same static class error.
- ImporterWindow blocked CSV and Google Sheets with "Only SQLite is implemented" message. All three sources now work.
- ImporterWindow had orphaned try/catch braces after GenerateAssets refactor.

### Changed
- ImporterWindow refactored into regions (Drawing, Validation, Generation, Utilities).
- Validation status shows table and row counts for all source types.
- Removed agent artifact files from repository root.
- Repository cleaned up for open source release.

## [0.2.0] - 2026-02-08

### Added
- SQLite import pipeline with foreign key resolution.
- SQLiteTypeMapper, SQLiteSchemaParser, DependencyResolver, ForeignKeyResolver, SQLiteReader, SQLiteValidator.
- ImporterWindow with table selection and dependency visualization.
- Sample SQLite database with 4 relational tables.
- SQLiteTypeMapperTests (19), DependencyResolverTests (10), SQLiteValidatorTests (8).

## [0.1.1] - 2026-02-08

### Fixed
- SchemaBuilder: OriginalHeader uses normalized header for empty and duplicate columns.
- CodeGenerator: GenerateScriptableObject includes database class in output.
- CodeGenerator: Tooltip, multiline, and range attribute generation.
- NameSanitizer: Namespace sanitization for generated code.

## [0.1.0] - 2026-02-07

### Added
- Initial package with CSV parser, type inference, schema builder, code generator.
- Google Sheets URL parser.
- Name sanitizer, type converters, reflection mapper.
- Sample CSV files and test suite.

[0.3.0]: https://github.com/SBUplakankus/so-database-converter/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/SBUplakankus/so-database-converter/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/SBUplakankus/so-database-converter/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/SBUplakankus/so-database-converter/releases/tag/v0.1.0
