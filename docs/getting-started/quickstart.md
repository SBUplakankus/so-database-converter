# Quick Start

This guide walks through importing a CSV file and generating ScriptableObject assets.

## Step 1: Open the Importer

Navigate to **Tools > Data to ScriptableObject** in the Unity menu bar.

## Step 2: Select a Source

Choose your data source from the **Type** dropdown:

- **CSV** -- Select a local `.csv` file from your project.
- **Google Sheets** -- Paste a Google Sheets URL (the sheet must be publicly shared).
- **SQLite** -- Select a `.db`, `.sqlite`, or `.sqlite3` file.

## Step 3: Validate

The tool automatically validates your source when selected. A green status message confirms the source is valid, showing table and row counts.

## Step 4: Configure

Set generation options:

- **Namespace** -- C# namespace for generated classes.
- **Script Output** -- Directory for generated `.cs` files.
- **Asset Output** -- Directory for generated `.asset` files.
- **Generate Database** -- Whether to generate a database container class.

## Step 5: Generate

Click **Generate** to create the ScriptableObject classes.

## CSV Format Example

The CSV format uses a multi-row header convention:

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

- **Row 1**: Column headers
- **Row 2**: Type hints
- **Row 3**: Flags (key, name, optional, range, tooltip, etc.) and enum values
- **Rows 4+**: Data

Directives (`#class:`, `#database:`, `#namespace:`) can appear before the header row to control class naming.
