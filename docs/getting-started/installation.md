# Installation

## Unity Package Manager (Recommended)

1. Open your Unity project.
2. Navigate to **Window > Package Manager**.
3. Click the **+** button in the top-left corner and select **Add package from git URL**.
4. Enter the following URL:

```
https://github.com/SBUplakankus/so-database-converter.git
```

5. Click **Add** and wait for the package to install.

## Manual Installation

1. Download or clone the repository.
2. Copy the entire folder into your Unity project's `Packages/` directory.
3. Unity will detect the package automatically on the next editor refresh.

## Verifying Installation

After installation, verify the tool is available by navigating to **Tools > Data to ScriptableObject** in the Unity menu bar. The importer window should open.

## Requirements

- Unity 2021.3 or later
- No additional dependencies required (SQLite library is embedded)
