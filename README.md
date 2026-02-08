# Data to ScriptableObject

Unity Editor tool that converts CSV files, Google Sheets, and SQLite databases into typed ScriptableObject assets with automatic code generation.

## Features

- **CSV Import** - RFC 4180 compliant parser with multi-row header convention
- **Google Sheets Integration** - Import directly from published Google Sheets URLs
- **SQLite Database Import** - Import relational databases with automatic foreign key resolution
- **Automatic Code Generation** - Generate ScriptableObject classes from your data
- **Type System** - Support for Unity types: int, float, bool, string, Vector2/3, Color, Sprite, Prefab, arrays, enums
- **Smart Type Inference** - Automatically detect types from CSV data
- **Foreign Key Support** - SQLite foreign keys become ScriptableObject references
- **Validation** - Range constraints, required fields, tooltips
- **Flexible Mapping** - Use existing classes or generate new ones

## Installation

Install via Unity Package Manager using Git URL:

```
https://github.com/SBUplakankus/so-database-converter.git
```

**Requirements**: Unity 2021.3 or later

## Quick Start

1. Open `Tools > Data to ScriptableObject`
2. Select a source type (CSV, Google Sheets, or SQLite)
3. Choose your data file or enter URL
4. For SQLite: Select which tables to import
5. Configure generation settings
6. Click Generate to create ScriptableObject class and assets

See the [Sample CSV files](Samples~/CSV/) and [Sample SQLite database](Samples~/SQLite/) for format examples.

## SQLite Features (New in v0.2.0)

- Import tables with foreign key relationships
- Automatic dependency resolution and topological sorting
- Foreign keys become direct ScriptableObject references
- CHECK constraints mapped to Range attributes
- CHECK IN clauses generate enum types
- Multi-table import with relationship visualization

## Development Status

**Current Version**: 0.2.0 (SQLite Phase Complete)

Phase 1 (CSV/Foundation) and Phase 2 (SQLite) are complete. The tool now supports CSV and SQLite database import with full foreign key relationship support.

## License

MIT License - see [LICENSE](LICENSE) for details.
