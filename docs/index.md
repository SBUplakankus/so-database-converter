# Data to ScriptableObject

A Unity Editor tool that converts CSV files, Google Sheets, and SQLite databases into typed ScriptableObject assets with automatic C# code generation.

## Overview

Data to ScriptableObject provides a complete pipeline for importing structured data into Unity projects. It supports three source formats and generates production-ready ScriptableObject classes with proper type safety, validation attributes, and database containers.

### Supported Sources

| Source | Description |
|--------|-------------|
| **CSV** | Local CSV files with an RFC 4180 compliant parser and multi-row header convention |
| **Google Sheets** | Import directly from published Google Sheets via URL or Spreadsheet ID |
| **SQLite** | Import relational databases with automatic foreign key resolution and dependency ordering |

### Key Features

- **Automatic Code Generation** -- Generates ScriptableObject entry and database classes from your data schema.
- **Type System** -- Supports int, float, double, long, bool, string, Vector2, Vector3, Color, Sprite, Prefab, enums, and arrays.
- **Smart Type Inference** -- Automatically detects column types from CSV data when type hints are not provided.
- **Foreign Key Support** -- SQLite foreign keys become direct ScriptableObject references.
- **Validation Attributes** -- Range constraints, tooltips, multiline, and header attributes flow through to generated code.
- **Flexible Configuration** -- Namespace, output paths, naming conventions, and serialization options are all configurable.

## Requirements

- Unity 2021.3 or later

## License

MIT License. See [LICENSE](https://github.com/SBUplakankus/so-database-converter/blob/main/LICENSE) for details.
