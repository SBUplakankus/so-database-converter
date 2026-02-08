# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2026-02-08

### Fixed
- SchemaBuilder: OriginalHeader now uses normalized header for empty/duplicate columns
- CodeGenerator: GenerateScriptableObject now includes database class in output
- CodeGenerator: Auto-generate tooltips when GenerateTooltips is enabled
- CodeGenerator: Multiline attribute support ([Multiline(N)])
- SchemaBuilder: Parse multiline(N) flag in flags row
- NameSanitizer: Added SanitizeNamespace for safe namespace identifiers
- CodeGenerator: Namespace sanitization in generated code
- FullPipelineTests: Corrected Range attribute format expectations

### Test Results
- 14 previously-failing full pipeline tests now pass
- All 466 tests passing (100%)

## [0.1.0] - 2026-02-07

### Added
- Initial package structure with Unity Package Manager configuration
- Complete data model classes for CSV/Google Sheets import pipeline
- CSV parser with RFC 4180 compliance
- Multi-row header convention (headers, types, flags)
- Type inference engine for automatic type detection
- Type converters for all supported Unity types
- Name sanitizer for C# identifiers and file names
- Schema builder with directive parsing (#class, #database, #namespace)
- Code generator for ScriptableObject classes
- Code generator for Database container classes
- Reflection mapper for using existing types
- Google Sheets URL parser with export URL generation
- Comprehensive test suite with 92 test cases
- Sample CSV files demonstrating the multi-row header format
- Documentation and usage guides

### Supported Types
- Primitives: int, float, double, long, bool, string
- Unity types: Vector2, Vector3, Color, Sprite, Prefab
- Arrays: int[], float[], string[], bool[]
- Custom: enum (auto-generated from CSV)

### Not Yet Implemented
- Editor Window UI (planned for v0.2.0)
- Asset generation pipeline (planned for v0.2.0)
- Google Sheets HTTP reader (planned for v0.2.0)
- Validators (planned for v0.2.0)
- Recompile handler (planned for v0.2.0)
- SQLite support (planned for v0.5.0)

[0.1.0]: https://github.com/SBUplakankus/so-database-converter/releases/tag/v0.1.0
