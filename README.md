# Data to ScriptableObject

Unity Editor tool that converts CSV files, Google Sheets, and SQLite databases into typed ScriptableObject assets with automatic C# code generation.

## Features

- **CSV Import** -- RFC 4180 compliant parser with multi-row header convention for type hints, flags, and attributes.
- **Google Sheets Import** -- Import directly from published Google Sheets via URL or Spreadsheet ID.
- **SQLite Import** -- Import relational databases with automatic foreign key resolution and dependency ordering.
- **Code Generation** -- Generates ScriptableObject entry classes, database containers, and enum types.
- **Type System** -- Supports int, float, double, long, bool, string, Vector2/3, Color, Sprite, Prefab, arrays, and enums.
- **Type Inference** -- Automatically detects column types from data when type hints are not provided.
- **Validation** -- Range constraints, tooltips, multiline, and header attributes flow through to generated code.

## Installation

Install via Unity Package Manager using the Git URL:

```
https://github.com/SBUplakankus/so-database-converter.git
```

**Requirements**: Unity 2021.3 or later.

## Quick Start

1. Open **Tools > Data to ScriptableObject** in the Unity menu bar.
2. Select a source type: CSV, Google Sheets, or SQLite.
3. Choose your data file, enter a URL, or browse for a database.
4. Configure generation settings (namespace, output paths, options).
5. Click **Generate** to create ScriptableObject classes.

## CSV Format

```csv
#class:Weapon
#database:WeaponDatabase
id,name,damage,rarity
int,string,float,enum
key,name,range(0;200),Common,Rare,Epic,Legendary
1,Iron Sword,25.0,Common
2,Fire Staff,80.0,Epic
3,Excalibur,150.0,Legendary
```

See the [documentation](docs/index.md) for the full CSV format specification, Google Sheets setup, and SQLite import details.

## Documentation

Full documentation is available in the `docs/` directory and can be built with [MkDocs Material](https://squidfunk.github.io/mkdocs-material/):

```bash
pip install mkdocs-material
mkdocs serve
```

## Samples

- [CSV examples](Samples~/CSV/) -- Demonstrates the multi-row header convention.
- [SQLite example](Samples~/SQLite/) -- A relational database with foreign keys.

## License

MIT License. See [LICENSE](LICENSE) for details.
