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
4. Click Import to generate ScriptableObject class and assets

## Examples

### items_example.csv
Demonstrates a game item database with:
- Multiple types (int, string, float, bool, enum, sprite, array)
- Enum values for element types
- Range validation on numeric fields
- List vs Array usage
- Key and name field designation
- Optional fields (description)

### enemies_example.csv
A simpler example showing enemy data with:
- Basic numeric types
- Range constraints
- Boolean flags
