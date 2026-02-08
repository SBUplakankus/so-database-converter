# SQLite Examples

The `game_data_example.db` file contains a sample game database with relational tables.

## Tables

### elements
| Column | Type    | Constraints |
|--------|---------|-------------|
| id     | INTEGER | PRIMARY KEY |
| name   | TEXT    | NOT NULL    |
| color  | TEXT    |             |

**Sample Data:** Fire, Water, Earth, Air, Lightning elements with color codes

### enemies
| Column | Type    | Constraints         |
|--------|---------|---------------------|
| id     | INTEGER | PRIMARY KEY         |
| name   | TEXT    | NOT NULL            |
| health | REAL    | CHECK(health >= 0)  |

**Sample Data:** Goblin, Orc, Dragon, Skeleton, Troll with varying health values

### items
| Column     | Type    | Constraints                  |
|------------|---------|------------------------------|
| id         | INTEGER | PRIMARY KEY                  |
| name       | TEXT    | NOT NULL                     |
| damage     | REAL    | CHECK(damage >= 0 AND damage <= 100) |
| element_id | INTEGER | REFERENCES elements(id)      |

**Sample Data:** Fire Sword, Water Staff, Stone Hammer, Wind Dagger, Lightning Spear

### loot_drops
| Column    | Type    | Constraints              |
|-----------|---------|--------------------------|
| id        | INTEGER | PRIMARY KEY              |
| item_id   | INTEGER | REFERENCES items(id)     |
| enemy_id  | INTEGER | REFERENCES enemies(id)   |
| drop_rate | REAL    | CHECK(drop_rate >= 0 AND drop_rate <= 1) |

**Sample Data:** Defines which enemies drop which items and at what rate

## Relationships

```
elements ◄── items.element_id
items    ◄── loot_drops.item_id
enemies  ◄── loot_drops.enemy_id
```

**Generation Order:** 1. elements  2. enemies  3. items  4. loot_drops

## Usage

1. Open `Tools > Data to ScriptableObject`
2. Select `SQLite Database` as the source type
3. Drag `game_data_example.db` into the file field
4. Select which tables to import
5. Click Generate

The tool will:
- Generate ScriptableObject classes for each table
- Create assets for each row
- Automatically resolve foreign keys as ScriptableObject references
- Apply range constraints from CHECK clauses

## Expected Output

After import, you'll have:
- **ElementsEntry.cs** and **ElementsDatabase.cs** with 5 element assets
- **EnemiesEntry.cs** and **EnemiesDatabase.cs** with 5 enemy assets  
- **ItemsEntry.cs** and **ItemsDatabase.cs** with 5 item assets (each referencing an ElementsEntry)
- **LootDropsEntry.cs** and **LootDropsDatabase.cs** with 5 loot drop assets (each referencing ItemsEntry and EnemiesEntry)

The foreign key relationships are automatically resolved, so each Item will have a direct reference to its Element ScriptableObject, and each LootDrop will reference both an Item and Enemy ScriptableObject.
