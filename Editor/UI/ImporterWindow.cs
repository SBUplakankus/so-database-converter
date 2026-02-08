using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataToScriptableObject;
using DataToScriptableObject.Editor;

namespace DataToScriptableObject.Editor.UI
{
    public class ImporterWindow : EditorWindow
    {
        private enum SourceType { CSV, GoogleSheets, SQLite }
        private enum GenerationMode { GenerateNew, UseExisting }

        // Source selection
        private SourceType sourceType = SourceType.CSV;
        private string sourceFilePath = "";
        private UnityEngine.Object sourceFileObject;
        
        // Validation
        private SourceValidationResult validationResult;
        private bool isValidating = false;
        
        // SQLite-specific
        private List<TableSelectionInfo> tableSelections = new List<TableSelectionInfo>();
        
        // Generation settings
        private GenerationMode generationMode = GenerationMode.GenerateNew;
        private GenerationSettings settings = new GenerationSettings();
        
        // UI state
        private Vector2 scrollPosition;
        private bool showAdvancedOptions = false;

        [MenuItem("Tools/Data to ScriptableObject")]
        public static void ShowWindow()
        {
            var window = GetWindow<ImporterWindow>("Data to ScriptableObject");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawSourceSelection();
            
            if (validationResult != null && validationResult.IsValid)
            {
                if (sourceType == SourceType.SQLite)
                {
                    DrawSQLiteTableSelection();
                    DrawSQLiteDependencyVisualization();
                }
                
                DrawGenerationSettings();
                DrawAdvancedOptions();
            }
            
            DrawValidationStatus();
            DrawGenerateButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Data to ScriptableObject Converter", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Convert CSV, Google Sheets, or SQLite databases to ScriptableObjects", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);
        }

        private void DrawSourceSelection()
        {
            EditorGUILayout.LabelField("Source Type", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            sourceType = (SourceType)EditorGUILayout.EnumPopup("Type", sourceType);
            if (EditorGUI.EndChangeCheck())
            {
                validationResult = null;
                tableSelections.Clear();
            }

            EditorGUILayout.Space(5);

            switch (sourceType)
            {
                case SourceType.CSV:
                    DrawCSVSourceUI();
                    break;
                case SourceType.GoogleSheets:
                    DrawGoogleSheetsSourceUI();
                    break;
                case SourceType.SQLite:
                    DrawSQLiteSourceUI();
                    break;
            }

            EditorGUILayout.Space(10);
        }

        private void DrawCSVSourceUI()
        {
            EditorGUILayout.HelpBox("CSV import support - Select a CSV file to import", MessageType.Info);
            
            EditorGUI.BeginChangeCheck();
            sourceFileObject = EditorGUILayout.ObjectField("CSV File", sourceFileObject, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck() && sourceFileObject != null)
            {
                sourceFilePath = AssetDatabase.GetAssetPath(sourceFileObject);
                ValidateSource();
            }
        }

        private void DrawGoogleSheetsSourceUI()
        {
            EditorGUILayout.HelpBox("Google Sheets import support - Paste a Google Sheets URL", MessageType.Info);
            
            EditorGUI.BeginChangeCheck();
            sourceFilePath = EditorGUILayout.TextField("Sheets URL", sourceFilePath);
            if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(sourceFilePath))
            {
                // TODO: Validate Google Sheets URL
            }
        }

        private void DrawSQLiteSourceUI()
        {
            EditorGUILayout.HelpBox("SQLite database import - Select a .db, .sqlite, or .sqlite3 file", MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            sourceFileObject = EditorGUILayout.ObjectField("Database File", sourceFileObject, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck() && sourceFileObject != null)
            {
                sourceFilePath = AssetDatabase.GetAssetPath(sourceFileObject);
                if (!string.IsNullOrEmpty(sourceFilePath))
                {
                    sourceFilePath = Path.GetFullPath(sourceFilePath);
                    ValidateSource();
                }
            }
            
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFilePanel("Select SQLite Database", "", "db,sqlite,sqlite3,db3");
                if (!string.IsNullOrEmpty(path))
                {
                    sourceFilePath = path;
                    ValidateSource();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(sourceFilePath))
            {
                EditorGUILayout.LabelField("Path", sourceFilePath, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawSQLiteTableSelection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Table Selection", EditorStyles.boldLabel);
            
            if (tableSelections.Count == 0)
            {
                EditorGUILayout.HelpBox("No tables found in database", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Found {tableSelections.Count} tables. Select which tables to import. " +
                "Tables will be processed in dependency order to resolve foreign keys.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            foreach (var tableInfo in tableSelections)
            {
                EditorGUILayout.BeginHorizontal();
                tableInfo.selected = EditorGUILayout.Toggle(tableInfo.selected, GUILayout.Width(20));
                
                string label = $"{tableInfo.tableName}";
                string details = $"({tableInfo.rowCount} rows, {tableInfo.columnCount} columns";
                if (tableInfo.foreignKeyCount > 0)
                {
                    details += $", {tableInfo.foreignKeyCount} FK";
                }
                details += ")";
                
                EditorGUILayout.LabelField(label, GUILayout.Width(150));
                EditorGUILayout.LabelField(details, EditorStyles.miniLabel);
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSQLiteDependencyVisualization()
        {
            if (sourceType != SourceType.SQLite || tableSelections.Count == 0)
                return;

            var selectedTables = tableSelections.Where(t => t.selected).ToList();
            if (selectedTables.Count == 0)
                return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Relationships", EditorStyles.boldLabel);
            
            // Show a simple text-based relationship view
            bool hasRelationships = false;
            foreach (var table in selectedTables)
            {
                if (!string.IsNullOrEmpty(table.foreignKeyInfo))
                {
                    EditorGUILayout.LabelField(table.foreignKeyInfo, EditorStyles.miniLabel);
                    hasRelationships = true;
                }
            }

            if (!hasRelationships)
            {
                EditorGUILayout.LabelField("No foreign key relationships", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);
            
            // Show generation order
            EditorGUILayout.LabelField("Generation Order", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("1. Tables with no dependencies → 2. ... → N. Last table", 
                EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawGenerationSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
            
            generationMode = (GenerationMode)EditorGUILayout.EnumPopup("Mode", generationMode);
            
            if (generationMode == GenerationMode.GenerateNew)
            {
                settings.NamespaceName = EditorGUILayout.TextField("Namespace", settings.NamespaceName);
                settings.ScriptOutputPath = EditorGUILayout.TextField("Script Output", settings.ScriptOutputPath);
            }
            
            settings.AssetOutputPath = EditorGUILayout.TextField("Asset Output", settings.AssetOutputPath);
            settings.GenerateDatabaseContainer = EditorGUILayout.Toggle("Generate Database", settings.GenerateDatabaseContainer);
        }

        private void DrawAdvancedOptions()
        {
            EditorGUILayout.Space(10);
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options", true);
            
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                settings.UseListInsteadOfArray = EditorGUILayout.Toggle("Use List Instead of Array", settings.UseListInsteadOfArray);
                settings.GenerateTooltips = EditorGUILayout.Toggle("Generate Tooltips", settings.GenerateTooltips);
                settings.OverwriteExistingAssets = EditorGUILayout.Toggle("Overwrite Existing", settings.OverwriteExistingAssets);
                settings.SanitizeFieldNames = EditorGUILayout.Toggle("Sanitize Field Names", settings.SanitizeFieldNames);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawValidationStatus()
        {
            if (isValidating)
            {
                EditorGUILayout.HelpBox("Validating...", MessageType.Info);
                return;
            }

            if (validationResult == null)
                return;

            EditorGUILayout.Space(10);
            
            if (validationResult.IsValid)
            {
                string message = $"✓ Valid source";
                if (sourceType == SourceType.SQLite)
                {
                    message += $": {validationResult.TableCount} tables, {validationResult.TotalRowCount} rows";
                }
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"✗ {validationResult.ErrorMessage}", MessageType.Error);
            }

            // Show warnings
            if (validationResult.Warnings != null && validationResult.Warnings.Count > 0)
            {
                foreach (var warning in validationResult.Warnings)
                {
                    MessageType msgType = warning.level == WarningLevel.Error ? MessageType.Error :
                                         warning.level == WarningLevel.Warning ? MessageType.Warning :
                                         MessageType.Info;
                    EditorGUILayout.HelpBox(warning.message, msgType);
                }
            }
        }

        private void DrawGenerateButton()
        {
            EditorGUILayout.Space(10);
            
            GUI.enabled = validationResult != null && validationResult.IsValid;
            
            if (sourceType == SourceType.SQLite)
            {
                GUI.enabled = GUI.enabled && tableSelections.Any(t => t.selected);
            }

            if (GUILayout.Button("Generate", GUILayout.Height(40)))
            {
                GenerateAssets();
            }
            
            GUI.enabled = true;
        }

        private void ValidateSource()
        {
            isValidating = true;
            validationResult = null;
            tableSelections.Clear();

            try
            {
                switch (sourceType)
                {
                    case SourceType.CSV:
                        ValidateCSVSource();
                        break;
                    case SourceType.SQLite:
                        ValidateSQLiteSource();
                        break;
                    case SourceType.GoogleSheets:
                        validationResult = new SourceValidationResult
                        {
                            Status = SourceStatus.NotLoaded,
                            IsValid = false,
                            ErrorMessage = "Google Sheets validation not yet implemented"
                        };
                        break;
                    default:
                        validationResult = new SourceValidationResult
                        {
                            Status = SourceStatus.NotLoaded,
                            IsValid = false,
                            ErrorMessage = "Validation not yet implemented for this source type"
                        };
                        break;
                }
            }
            catch (Exception ex)
            {
                validationResult = new SourceValidationResult
                {
                    Status = SourceStatus.InvalidFormat,
                    IsValid = false,
                    ErrorMessage = $"Validation error: {ex.Message}"
                };
            }
            finally
            {
                isValidating = false;
                Repaint();
            }
        }

        private void ValidateCSVSource()
        {
            var validator = new CSVValidator(sourceFilePath, settings);
            
            // Quick validation
            validationResult = validator.ValidateQuick();
            if (!validationResult.IsValid)
            {
                return;
            }

            // Full validation (synchronous for simplicity)
            validator.ValidateFull(result =>
            {
                validationResult = result;
                Repaint();
            });
        }

        private void ValidateSQLiteSource()
        {
            var validator = new SQLiteValidator(sourceFilePath, settings);
            
            // Quick validation
            validationResult = validator.ValidateQuick();
            if (!validationResult.IsValid)
            {
                return;
            }

            // Full validation (synchronous for simplicity)
            validator.ValidateFull(result =>
            {
                validationResult = result;
                
                if (result.IsValid && result.TableNames != null)
                {
                    // Populate table selection list
                    tableSelections.Clear();
                    for (int i = 0; i < result.TableNames.Length; i++)
                    {
                        tableSelections.Add(new TableSelectionInfo
                        {
                            tableName = result.TableNames[i],
                            selected = true,  // Select all by default
                            rowCount = 0,     // Will be populated by schema extraction
                            columnCount = 0,
                            foreignKeyCount = 0
                        });
                    }
                }
                
                Repaint();
            });
        }

        private void GenerateAssets()
        {
            if (sourceType != SourceType.SQLite)
            {
                EditorUtility.DisplayDialog("Not Implemented",
                    "Only SQLite import is currently implemented in Phase 2.",
                    "OK");
                return;
            }

            try
            {
                // Get selected tables
                var selectedTableNames = tableSelections.Where(t => t.selected).Select(t => t.tableName).ToArray();
                if (selectedTableNames.Length == 0)
                {
                    EditorUtility.DisplayDialog("No Tables Selected",
                        "Please select at least one table to import.",
                        "OK");
                    return;
                }

                // TODO: Implement full asset generation pipeline
                // This would involve:
                // 1. Extract schemas for selected tables
                // 2. Generate code for each table
                // 3. Trigger recompile
                // 4. After recompile, populate assets using AssetPopulator with FK resolution
                
                EditorUtility.DisplayDialog("Generation Started",
                    $"Generating assets for {selectedTableNames.Length} tables.\n\n" +
                    "Note: Full asset generation pipeline not yet implemented.\n" +
                    "Schemas will be extracted and validated.",
                    "OK");

                // For now, just validate we can extract schemas
                var validator = new SQLiteValidator(sourceFilePath, settings);
                var schemas = validator.ExtractSchemas();
                
                Debug.Log($"Successfully extracted {schemas.Length} table schemas in dependency order");
                foreach (var schema in schemas)
                {
                    Debug.Log($"  - {schema.SourceTableName}: {schema.Columns.Length} columns, {schema.Rows.Count} rows");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Generation Failed",
                    $"Failed to generate assets:\n\n{ex.Message}",
                    "OK");
                Debug.LogError($"Asset generation failed: {ex}");
            }
        }

        private void LoadSettings()
        {
            // Load settings from EditorPrefs
            // TODO: Implement settings persistence
        }

        private void SaveSettings()
        {
            // Save settings to EditorPrefs
            // TODO: Implement settings persistence
        }

        private class TableSelectionInfo
        {
            public string tableName;
            public bool selected;
            public int rowCount;
            public int columnCount;
            public int foreignKeyCount;
            public string foreignKeyInfo;
        }
    }
}
