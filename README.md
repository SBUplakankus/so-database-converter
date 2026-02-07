# Data to ScriptableObject

Unity Editor tool that converts CSV files and Google Sheets into typed ScriptableObject assets with automatic code generation.

## Features

- **CSV Import** - RFC 4180 compliant parser with multi-row header convention
- **Google Sheets Integration** - Import directly from published Google Sheets URLs
- **Automatic Code Generation** - Generate ScriptableObject classes from your data
- **Type System** - Support for Unity types: int, float, bool, string, Vector2/3, Color, Sprite, Prefab, arrays, enums
- **Smart Type Inference** - Automatically detect types from CSV data
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
2. Select a CSV file or paste a Google Sheets URL
3. Configure generation settings
4. Click Import to generate ScriptableObject class and assets

See the [Sample CSV files](Samples~/CSV/) for format examples.

## Development Status

**Current Version**: 0.1.0 (Foundation Phase)

This package is under active development. The foundation (Models, Core Logic, Tests) is complete. Editor UI and additional features are coming in future updates.

## License

MIT License - see [LICENSE](LICENSE) for details.
