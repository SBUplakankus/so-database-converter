using System.Linq;
using DataToScriptableObject;
using DataToScriptableObject.Editor;
using NUnit.Framework;

namespace DataToScriptableObject.Tests.Editor
{
    /// <summary>
    /// Integration tests for complete workflows that users would perform
    /// when creating ScriptableObject databases from CSV files.
    /// These simulate real-world scenarios end-to-end.
    /// </summary>
    public class CSVReaderIntegrationTests
    {
        #region Game Item Database Scenarios

        [Test]
        public void TestWeaponDatabaseWithComplexStats()
        {
            // Realistic weapon database with multiple stat types
            string csv = @"#class:Weapon
#namespace:Game.Items
name,damage,attackSpeed,range,rarity,description
string,int,float,float,string,string
Sword of Light,75,1.2,2.5,Legendary,""A sword that glows with holy power. Deals bonus damage to undead enemies.""
Iron Dagger,15,2.5,1.0,Common,""A simple iron dagger. Light and fast.""
Mystic Staff,100,0.8,5.0,Epic,""Ancient staff imbued with arcane energy.""";
            
            var result = CSVReader.Parse(csv, ",", "#", 2);
            
            Assert.AreEqual(2, result.Directives.Length);
            Assert.IsTrue(result.Directives[0].Contains("Weapon"));
            Assert.AreEqual(6, result.Headers.Length);
            Assert.IsNotNull(result.TypeHints);
            Assert.AreEqual(3, result.DataRows.Length);
            
            // Verify specific weapon data
            Assert.AreEqual("Sword of Light", result.DataRows[0][0]);
            Assert.AreEqual("75", result.DataRows[0][1]);
            Assert.IsTrue(result.DataRows[0][5].Contains("holy power"));
        }

        [Test]
        public void TestCharacterDatabaseWithEnums()
        {
            // Character database with enum types
            string csv = @"#class:Character
id,name,class,faction,level
int,string,enum,enum,int
,,,Ally|Enemy|Neutral,
1,Hero,Warrior,Ally,10
2,Mage,Wizard,Ally,12
3,Goblin,Fighter,Enemy,5";
            
            var result = CSVReader.Parse(csv, ",", "#", 3);
            
            Assert.AreEqual(1, result.Directives.Length);
            Assert.AreEqual(5, result.Headers.Length);
            Assert.IsNotNull(result.TypeHints);
            Assert.IsNotNull(result.Flags);
            Assert.AreEqual(3, result.DataRows.Length);
        }

        [Test]
        public void TestQuestDatabaseWithMultilineText()
        {
            // Quest database with multiline descriptions and objectives
            string csv = "id,name,description,objectives\n1,The Lost Sword,\"Find the legendary sword that was lost in the ancient ruins.\n\nRewards:\n- 1000 Gold\n- Legendary Weapon\",\"Search the ruins\nDefeat the guardian\nRetrieve the sword\"\n2,Save the Village,\"Goblins are attacking! Help defend the village.\",\"Kill 10 goblins\"";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][2].Contains("\n"));
            Assert.IsTrue(result.DataRows[0][2].Contains("Rewards:"));
            Assert.IsTrue(result.DataRows[0][3].Contains("ruins"));
        }

        [Test]
        public void TestInventoryDatabaseWithPrices()
        {
            // Inventory items with various price formats
            string csv = "item,price_coins,price_gems,stackable,weight\nHealth Potion,50,0,true,0.5\nMana Potion,75,0,true,0.5\nPhoenix Feather,0,25,false,0.1\nDragon Scale,1000,100,false,2.5";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(4, result.DataRows.Length);
            Assert.AreEqual("Health Potion", result.DataRows[0][0]);
            Assert.AreEqual("50", result.DataRows[0][1]);
            Assert.AreEqual("true", result.DataRows[0][3]);
        }

        [Test]
        public void TestSkillTreeDatabase()
        {
            // Skill tree with prerequisites and requirements
            string csv = "id,name,level_required,prerequisites,effect,cooldown\n1,Fireball,1,,\"Launches a fireball dealing 50-100 damage\",5.0\n2,Greater Fireball,5,Fireball,\"Improved fireball dealing 100-200 damage\",8.0\n3,Inferno,10,\"Greater Fireball\",\"Massive fire AoE dealing 200-400 damage\",20.0";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("Fireball", result.DataRows[0][1]);
            Assert.AreEqual("", result.DataRows[0][3]); // No prerequisites for first skill
            Assert.AreEqual("Greater Fireball", result.DataRows[2][3]); // Quoted prerequisite
        }

        #endregion

        #region Localization and Language Scenarios

        [Test]
        public void TestLocalizationDatabase()
        {
            // Multi-language localization database
            string csv = "key,en,es,fr,de,ja\nHELLO,Hello,Hola,Bonjour,Hallo,こんにちは\nGOODBYE,Goodbye,Adiós,Au revoir,Auf Wiedersehen,さようなら\nTHANKS,\"Thank you!\",\"¡Gracias!\",\"Merci!\",\"Danke!\",\"ありがとう！\"";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual(6, result.Headers.Length);
            Assert.AreEqual("こんにちは", result.DataRows[0][5]);
            Assert.AreEqual("さようなら", result.DataRows[1][5]);
        }

        [Test]
        public void TestDialogueDatabase()
        {
            // NPC dialogue with choices and branches
            string csv = "id,speaker,text,choices\n1,Guard,\"Halt! What is your business here?\",\"I'm just passing through.|I have a delivery.|[Attack]\"\n2,Merchant,\"Welcome to my shop! What can I get for you?\",\"Show me your wares.|Not interested.\"";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][3].Contains("|"));
            Assert.IsTrue(result.DataRows[0][3].Contains("[Attack]"));
        }

        #endregion

        #region Configuration and Settings Scenarios

        [Test]
        public void TestGameConfigDatabase()
        {
            // Game configuration with various data types
            string csv = "setting,value,type,description\nMAX_PLAYERS,100,int,\"Maximum number of players in server\"\nSERVER_NAME,\"My Game Server\",string,\"Name displayed in server list\"\nENABLE_PVP,true,bool,\"Allow player vs player combat\"\nDROP_RATE,1.5,float,\"Multiplier for item drop rates\"";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(4, result.DataRows.Length);
            Assert.AreEqual("MAX_PLAYERS", result.DataRows[0][0]);
            Assert.AreEqual("100", result.DataRows[0][1]);
            Assert.AreEqual("My Game Server", result.DataRows[1][1]);
        }

        [Test]
        public void TestDifficultySettingsDatabase()
        {
            // Difficulty presets with multiple parameters
            string csv = "difficulty,enemy_health_multiplier,enemy_damage_multiplier,player_damage_taken,xp_multiplier,loot_quality\nEasy,0.5,0.75,0.5,1.0,Normal\nNormal,1.0,1.0,1.0,1.0,Normal\nHard,1.5,1.25,1.5,1.25,Good\nNightmare,2.0,2.0,2.0,1.5,Excellent";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(4, result.DataRows.Length);
            Assert.AreEqual("Easy", result.DataRows[0][0]);
            Assert.AreEqual("2.0", result.DataRows[3][1]);
        }

        #endregion

        #region Asset Reference Scenarios

        [Test]
        public void TestAssetReferenceDatabase()
        {
            // Database with Unity asset paths
            string csv = "id,name,prefab_path,icon_path,audio_path\n1,Sword,Assets/Prefabs/Weapons/Sword.prefab,Assets/Icons/Weapons/sword_icon.png,Assets/Audio/SFX/sword_swing.wav\n2,Shield,Assets/Prefabs/Armor/Shield.prefab,Assets/Icons/Armor/shield_icon.png,Assets/Audio/SFX/shield_block.wav";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][2].Contains("Assets/Prefabs"));
            Assert.IsTrue(result.DataRows[1][4].Contains(".wav"));
        }

        [Test]
        public void TestSpriteAtlasDatabase()
        {
            // Sprite atlas configuration
            string csv = "sprite_name,atlas,x,y,width,height\nplayer_idle_0,characters,0,0,32,32\nplayer_idle_1,characters,32,0,32,32\nplayer_walk_0,characters,64,0,32,32";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("player_idle_0", result.DataRows[0][0]);
            Assert.AreEqual("32", result.DataRows[0][4]);
        }

        #endregion

        #region Analytics and Statistics Scenarios

        [Test]
        public void TestPlayerStatsDatabase()
        {
            // Player statistics tracking
            string csv = "player_id,username,total_playtime_hours,kills,deaths,kd_ratio,level,experience,last_login\n1001,PlayerOne,152.5,1250,320,3.91,45,125000,2024-02-07T10:30:00\n1002,PlayerTwo,89.2,780,410,1.90,32,85000,2024-02-07T15:45:00";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("PlayerOne", result.DataRows[0][1]);
            Assert.AreEqual("3.91", result.DataRows[0][5]);
        }

        [Test]
        public void TestLeaderboardDatabase()
        {
            // Leaderboard with rankings
            string csv = "rank,player,score,time,date\n1,SpeedRunner123,9999,\"2:34.567\",2024-02-01\n2,ProGamer,9850,\"2:35.123\",2024-02-02\n3,Champion,9800,\"2:36.789\",2024-02-03";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
            Assert.AreEqual("2:34.567", result.DataRows[0][3]);
        }

        #endregion

        #region Procedural Generation Scenarios

        [Test]
        public void TestBiomeGenerationDatabase()
        {
            // Biome generation parameters
            string csv = "biome,temperature,humidity,elevation,vegetation_density,spawn_enemies,spawn_resources\nDesert,0.8,0.1,0.3,0.2,\"Scorpion,Snake,Vulture\",\"Sand,Cactus,Bones\"\nForest,0.5,0.7,0.5,0.8,\"Wolf,Bear,Deer\",\"Wood,Berries,Mushrooms\"\nMountain,0.2,0.4,0.9,0.3,\"Eagle,Goat,Yeti\",\"Stone,Iron,Gold\"";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][5].Contains("Scorpion"));
            Assert.IsTrue(result.DataRows[1][6].Contains("Mushrooms"));
        }

        [Test]
        public void TestLootTableDatabase()
        {
            // Loot table with weighted probabilities
            string csv = "container,item,min_quantity,max_quantity,drop_chance\nWooden Chest,Gold,10,50,0.8\nWooden Chest,Health Potion,1,3,0.6\nWooden Chest,Iron Sword,1,1,0.1\nGolden Chest,Gold,100,500,1.0\nGolden Chest,Rare Gem,1,5,0.5\nGolden Chest,Legendary Item,1,1,0.05";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(6, result.DataRows.Length);
            Assert.AreEqual("Wooden Chest", result.DataRows[0][0]);
            Assert.AreEqual("0.05", result.DataRows[5][4]);
        }

        #endregion

        #region Recipe and Crafting Scenarios

        [Test]
        public void TestCraftingRecipeDatabase()
        {
            // Crafting recipes with multiple ingredients
            string csv = "recipe,result,ingredients,crafting_time,required_station\nIron Sword,\"Iron Sword x1\",\"Iron Ingot x3, Wood x1, Leather x1\",5.0,Forge\nHealth Potion,\"Health Potion x3\",\"Red Herb x2, Water x1\",2.0,Alchemy Table\nSteel Armor,\"Steel Armor x1\",\"Steel Ingot x5, Leather x3, Thread x10\",15.0,Forge";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][2].Contains("Iron Ingot"));
            Assert.IsTrue(result.DataRows[1][4].Contains("Alchemy"));
        }

        [Test]
        public void TestUpgradePathDatabase()
        {
            // Item upgrade paths
            string csv = "item,level,stats,upgrade_cost,next_level\nIron Sword,1,\"Damage: 10\",\"Gold: 100\",2\nIron Sword,2,\"Damage: 15\",\"Gold: 250, Iron: 5\",3\nIron Sword,3,\"Damage: 25\",\"Gold: 500, Steel: 3\",MAX";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[1][3].Contains("Iron: 5"));
            Assert.AreEqual("MAX", result.DataRows[2][4]);
        }

        #endregion

        #region AI and Behavior Scenarios

        [Test]
        public void TestAIBehaviorDatabase()
        {
            // AI behavior trees
            string csv = "enemy_type,aggro_range,chase_range,attack_range,attack_cooldown,patrol_points,ai_type\nGoblin,10.0,15.0,2.0,1.5,\"Point1,Point2,Point3\",Aggressive\nGuard,5.0,8.0,2.5,2.0,\"Gate,Tower,Wall\",Defensive\nBoss,20.0,30.0,5.0,0.5,\"Center\",VeryAggressive";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("10.0", result.DataRows[0][1]);
            Assert.IsTrue(result.DataRows[1][5].Contains("Gate"));
        }

        [Test]
        public void TestDialogueTreeDatabase()
        {
            // Dialogue tree with branching
            string csv = "node_id,speaker,text,next_nodes,requirements\n1,NPC,\"Hello traveler!\",\"2,3\",\n2,Player,\"I need your help.\",4,\n3,Player,\"Just passing through.\",5,\n4,NPC,\"What can I do for you?\",\"6,7\",\n5,NPC,\"Safe travels!\",END,";
            
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(5, result.DataRows.Length);
            Assert.IsTrue(result.DataRows[0][3].Contains("2,3"));
            Assert.AreEqual("END", result.DataRows[4][3]);
        }

        #endregion

        #region Validation Edge Cases

        [Test]
        public void TestLargeCompleteDatabase()
        {
            // Large, complete database with all features
            string csv = @"#class:GameItem
#database:ItemDatabase
#namespace:MyGame.Items
id,name,type,rarity,level_required,stats,description,icon_path,prefab_path
int,string,enum,enum,int,string,string,string,string
,,,Common|Rare|Epic|Legendary,,,,,
1,Rusty Sword,Weapon,Common,1,""Damage: 5, Speed: 1.0"",""An old, rusty sword. Better than nothing."",Assets/Icons/rusty_sword.png,Assets/Prefabs/rusty_sword.prefab
2,Steel Sword,Weapon,Rare,10,""Damage: 25, Speed: 1.2"",""A well-crafted steel sword. Sharp and reliable."",Assets/Icons/steel_sword.png,Assets/Prefabs/steel_sword.prefab
3,Legendary Blade,Weapon,Legendary,50,""Damage: 100, Speed: 1.5, Crit: 0.25"",""A legendary weapon forged by ancient smiths. Glows with mystical power."",Assets/Icons/legendary_blade.png,Assets/Prefabs/legendary_blade.prefab";
            
            var result = CSVReader.Parse(csv, ",", "#", 3);
            
            // Verify all components
            Assert.AreEqual(3, result.Directives.Length);
            Assert.IsTrue(result.Directives[0].Contains("GameItem"));
            Assert.IsTrue(result.Directives[1].Contains("ItemDatabase"));
            Assert.IsTrue(result.Directives[2].Contains("MyGame.Items"));
            
            Assert.AreEqual(9, result.Headers.Length);
            Assert.IsNotNull(result.TypeHints);
            Assert.IsNotNull(result.Flags);
            
            Assert.AreEqual(3, result.DataRows.Length);
            Assert.AreEqual("Steel Sword", result.DataRows[1][1]);
            Assert.IsTrue(result.DataRows[2][5].Contains("Crit"));
        }

        [Test]
        public void TestMinimalValidDatabase()
        {
            // Minimal valid database
            string csv = "id\n1\n2";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.Headers.Length);
            Assert.AreEqual(2, result.DataRows.Length);
        }

        [Test]
        public void TestEmptyDataWithHeaders()
        {
            // Headers but no data rows
            string csv = "a,b,c";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(3, result.Headers.Length);
            Assert.AreEqual(0, result.DataRows.Length);
        }

        [Test]
        public void TestOnlyDirectives()
        {
            // Only directives, no data
            string csv = "#class:Test\n#namespace:MyNS";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.Directives.Length);
            Assert.AreEqual(0, result.Headers.Length);
        }

        #endregion

        #region Comment Handling in Data

        [Test]
        public void TestCommentsInDataSection()
        {
            // Comments after headers should be skipped
            string csv = "a,b,c\n# This is a comment\n1,2,3\n// Another comment\n4,5,6";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(2, result.DataRows.Length);
            Assert.AreEqual("1", result.DataRows[0][0]);
            Assert.AreEqual("4", result.DataRows[1][0]);
        }

        [Test]
        public void TestHashInQuotedFieldNotComment()
        {
            // # inside quoted field should not be treated as comment
            string csv = "a,b\n\"#hashtag\",\"# note\"";
            var result = CSVReader.Parse(csv, ",", "#", 1);
            
            Assert.AreEqual(1, result.DataRows.Length);
            Assert.AreEqual("#hashtag", result.DataRows[0][0]);
            Assert.AreEqual("# note", result.DataRows[0][1]);
        }

        #endregion
    }
}
