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
        private SourceType _sourceType = SourceType.CSV;
        private string _sourceFilePath = "";
        private UnityEngine.Object _sourceFileObject;

        // Validation
        private SourceValidationResult _validationResult;
        private bool _isValidating;

        // SQLite-specific
        private readonly List<TableSelectionInfo> _tableSelections = new();

        // Google Sheets - cached downloaded CSV text
        private string _cachedGoogleSheetsCsv;

        // Generation settings
        private GenerationMode _generationMode = GenerationMode.GenerateNew;
        private readonly GenerationSettings _settings = new();

        // UI state
        private Vector2 _scrollPosition;
        private bool _showAdvancedOptions;

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
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawSourceSelection();

            if (_validationResult != null && _validationResult.IsValid)
            {
                if (_sourceType == SourceType.SQLite)
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

        #region Drawing

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
            _sourceType = (SourceType)EditorGUILayout.EnumPopup("Type", _sourceType);
            if (EditorGUI.EndChangeCheck())
            {
                _validationResult = null;
                _tableSelections.Clear();
                _cachedGoogleSheetsCsv = null;
            }

            EditorGUILayout.Space(5);

            switch (_sourceType)
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
            EditorGUILayout.HelpBox("CSV import - Select a CSV file to import.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _sourceFileObject = EditorGUILayout.ObjectField("CSV File", _sourceFileObject, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck() && _sourceFileObject != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(_sourceFileObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    _sourceFilePath = ConvertAssetPathToAbsolute(assetPath);
                    ValidateSource();
                }
            }
        }

        private void DrawGoogleSheetsSourceUI()
        {
            EditorGUILayout.HelpBox(
                "Google Sheets import - Paste a Google Sheets URL or Spreadsheet ID.\n" +
                "The sheet must be shared as 'Anyone with the link can view'.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _sourceFilePath = EditorGUILayout.TextField("Sheets URL", _sourceFilePath);
            if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(_sourceFilePath))
            {
                ValidateSource();
            }
        }

        private void DrawSQLiteSourceUI()
        {
            EditorGUILayout.HelpBox("SQLite database import - Select a .db, .sqlite, or .sqlite3 file.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _sourceFileObject = EditorGUILayout.ObjectField("Database File", _sourceFileObject, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck() && _sourceFileObject != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(_sourceFileObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    _sourceFilePath = ConvertAssetPathToAbsolute(assetPath);
                    ValidateSource();
                }
            }

            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFilePanel("Select SQLite Database", "", "db,sqlite,sqlite3,db3");
                if (!string.IsNullOrEmpty(path))
                {
                    _sourceFilePath = path;
                    ValidateSource();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_sourceFilePath))
            {
                EditorGUILayout.LabelField("Path", _sourceFilePath, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawSQLiteTableSelection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Table Selection", EditorStyles.boldLabel);

            if (_tableSelections.Count == 0)
            {
                EditorGUILayout.HelpBox("No tables found in database.", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Found {_tableSelections.Count} tables. Select which tables to import. " +
                "Tables will be processed in dependency order to resolve foreign keys.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            foreach (var tableInfo in _tableSelections)
            {
                EditorGUILayout.BeginHorizontal();
                tableInfo.selected = EditorGUILayout.Toggle(tableInfo.selected, GUILayout.Width(20));

                string details = $"({tableInfo.rowCount} rows, {tableInfo.columnCount} columns";
                if (tableInfo.foreignKeyCount > 0)
                    details += $", {tableInfo.foreignKeyCount} FK";
                details += ")";

                EditorGUILayout.LabelField(tableInfo.tableName, GUILayout.Width(150));
                EditorGUILayout.LabelField(details, EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSQLiteDependencyVisualization()
        {
            if (_sourceType != SourceType.SQLite || _tableSelections.Count == 0)
                return;

            var selectedTables = _tableSelections.Where(t => t.selected).ToList();
            if (selectedTables.Count == 0)
                return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Relationships", EditorStyles.boldLabel);

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
                EditorGUILayout.LabelField("No foreign key relationships.", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Generation Order", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("1. Tables with no dependencies -> 2. ... -> N. Last table",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawGenerationSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);

            _generationMode = (GenerationMode)EditorGUILayout.EnumPopup("Mode", _generationMode);

            if (_generationMode == GenerationMode.GenerateNew)
            {
                _settings.NamespaceName = EditorGUILayout.TextField("Namespace", _settings.NamespaceName);
                _settings.ScriptOutputPath = EditorGUILayout.TextField("Script Output", _settings.ScriptOutputPath);
            }

            _settings.AssetOutputPath = EditorGUILayout.TextField("Asset Output", _settings.AssetOutputPath);
            _settings.GenerateDatabaseContainer = EditorGUILayout.Toggle("Generate Database", _settings.GenerateDatabaseContainer);
        }

        private void DrawAdvancedOptions()
        {
            EditorGUILayout.Space(10);
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options", true);

            if (_showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                _settings.UseListInsteadOfArray = EditorGUILayout.Toggle("Use List Instead of Array", _settings.UseListInsteadOfArray);
                _settings.GenerateTooltips = EditorGUILayout.Toggle("Generate Tooltips", _settings.GenerateTooltips);
                _settings.OverwriteExistingAssets = EditorGUILayout.Toggle("Overwrite Existing", _settings.OverwriteExistingAssets);
                _settings.SanitizeFieldNames = EditorGUILayout.Toggle("Sanitize Field Names", _settings.SanitizeFieldNames);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawValidationStatus()
        {
            if (_isValidating)
            {
                EditorGUILayout.HelpBox("Validating...", MessageType.Info);
                return;
            }

            if (_validationResult == null)
                return;

            EditorGUILayout.Space(10);

            if (_validationResult.IsValid)
            {
                string message = "Valid source";
                if (_validationResult.TableCount > 0)
                    message += $": {_validationResult.TableCount} table(s), {_validationResult.TotalRowCount} rows";
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(_validationResult.ErrorMessage, MessageType.Error);
            }

            if (_validationResult.Warnings != null && _validationResult.Warnings.Count > 0)
            {
                foreach (var warning in _validationResult.Warnings)
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

            GUI.enabled = _validationResult != null && _validationResult.IsValid;

            if (_sourceType == SourceType.SQLite)
                GUI.enabled = GUI.enabled && _tableSelections.Any(t => t.selected);

            if (GUILayout.Button("Generate", GUILayout.Height(40)))
                GenerateAssets();

            GUI.enabled = true;
        }

        #endregion

        #region Validation

        private void ValidateSource()
        {
            _isValidating = true;
            _validationResult = null;
            _tableSelections.Clear();
            _cachedGoogleSheetsCsv = null;

            try
            {
                switch (_sourceType)
                {
                    case SourceType.CSV:
                        ValidateCSVSource();
                        break;
                    case SourceType.SQLite:
                        ValidateSQLiteSource();
                        break;
                    case SourceType.GoogleSheets:
                        ValidateGoogleSheetsSource();
                        break;
                }
            }
            catch (Exception ex)
            {
                _validationResult = new SourceValidationResult
                {
                    Status = SourceStatus.InvalidFormat,
                    IsValid = false,
                    ErrorMessage = $"Validation error: {ex.Message}"
                };
            }
            finally
            {
                _isValidating = false;
                Repaint();
            }
        }

        private void ValidateCSVSource()
        {
            var validator = new CSVValidator(_sourceFilePath, _settings);

            _validationResult = validator.ValidateQuick();
            if (!_validationResult.IsValid)
                return;

            validator.ValidateFull(result =>
            {
                _validationResult = result;
                Repaint();
            });
        }

        private void ValidateSQLiteSource()
        {
            var validator = new SQLiteValidator(_sourceFilePath, _settings);

            _validationResult = validator.ValidateQuick();
            if (!_validationResult.IsValid)
                return;

            validator.ValidateFull(result =>
            {
                _validationResult = result;

                if (result.IsValid && result.TableNames != null)
                {
                    _tableSelections.Clear();
                    for (int i = 0; i < result.TableNames.Length; i++)
                    {
                        _tableSelections.Add(new TableSelectionInfo
                        {
                            tableName = result.TableNames[i],
                            selected = true,
                            rowCount = 0,
                            columnCount = 0,
                            foreignKeyCount = 0
                        });
                    }
                }

                Repaint();
            });
        }

        private void ValidateGoogleSheetsSource()
        {
            var parseResult = GoogleSheetsURLParser.Parse(_sourceFilePath);
            if (!parseResult.IsValid)
            {
                _validationResult = new SourceValidationResult
                {
                    Status = SourceStatus.InvalidSource,
                    IsValid = false,
                    ErrorMessage = parseResult.Error
                };
                return;
            }

            string csvText;
            try
            {
                var exportUrl = GoogleSheetsURLParser.BuildExportURL(parseResult.SpreadsheetId, parseResult.Gid);
                using (var client = new System.Net.WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    csvText = client.DownloadString(exportUrl);
                }
            }
            catch (System.Net.WebException ex)
            {
                _validationResult = new SourceValidationResult
                {
                    Status = SourceStatus.NetworkError,
                    IsValid = false,
                    ErrorMessage = $"Failed to download Google Sheet. Ensure it is shared as " +
                                   $"'Anyone with the link can view'. Error: {ex.Message}"
                };
                return;
            }

            if (string.IsNullOrWhiteSpace(csvText))
            {
                _validationResult = new SourceValidationResult
                {
                    Status = SourceStatus.InvalidContent,
                    IsValid = false,
                    ErrorMessage = "Downloaded sheet is empty."
                };
                return;
            }

            _cachedGoogleSheetsCsv = csvText;

            var rawData = CSVReader.Parse(csvText, ",", _settings.CommentPrefix, _settings.HeaderRowCount);
            if (rawData == null || rawData.Headers == null || rawData.Headers.Length == 0)
            {
                _validationResult = new SourceValidationResult
                {
                    Status = SourceStatus.InvalidContent,
                    IsValid = false,
                    ErrorMessage = "Google Sheet has no headers or could not be parsed."
                };
                return;
            }

            TableSchema schema;
            try
            {
                schema = SchemaBuilder.Build(rawData, _settings);
            }
            catch (Exception ex)
            {
                _validationResult = new SourceValidationResult
                {
                    Status = SourceStatus.InvalidSchema,
                    IsValid = false,
                    ErrorMessage = $"Failed to build schema from Google Sheet: {ex.Message}"
                };
                return;
            }

            _validationResult = new SourceValidationResult
            {
                Status = SourceStatus.Valid,
                IsValid = true,
                TableCount = 1,
                TotalRowCount = rawData.DataRows != null ? rawData.DataRows.Length : 0,
                TotalColumnCount = rawData.Headers.Length,
                TableNames = new[] { schema.ClassName }
            };
        }

        #endregion

        #region Generation

        private void GenerateAssets()
        {
            try
            {
                switch (_sourceType)
                {
                    case SourceType.CSV:
                        GenerateCSVAssets();
                        break;
                    case SourceType.GoogleSheets:
                        GenerateGoogleSheetsAssets();
                        break;
                    case SourceType.SQLite:
                        GenerateSQLiteAssets();
                        break;
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Generation Failed",
                    $"Failed to generate assets:\n\n{ex.Message}", "OK");
                Debug.LogError($"Asset generation failed: {ex}");
            }
        }

        private void GenerateCSVAssets()
        {
            var validator = new CSVValidator(_sourceFilePath, _settings);
            var schemas = validator.ExtractSchemas();

            if (schemas == null || schemas.Length == 0)
            {
                EditorUtility.DisplayDialog("Generation Failed",
                    "Failed to extract schema from CSV file.", "OK");
                return;
            }

            var schema = schemas[0];

            if (string.IsNullOrEmpty(schema.ClassName))
                schema.ClassName = Path.GetFileNameWithoutExtension(_sourceFilePath);
            if (string.IsNullOrEmpty(schema.DatabaseName))
                schema.DatabaseName = schema.ClassName + "Database";

            var result = CodeGenerator.Generate(schema, _settings);

            Debug.Log($"Generated scripts for '{schema.ClassName}' ({schema.Columns.Length} columns, {schema.Rows.Count} rows)");
            EditorUtility.DisplayDialog("Generation Complete",
                $"Generated scripts for '{schema.ClassName}':\n\n" +
                $"{schema.Columns.Length} columns, {schema.Rows.Count} rows\n" +
                $"Entry: {result.EntryClassPath}\n" +
                $"Database: {result.DatabaseClassPath}", "OK");
        }

        private void GenerateGoogleSheetsAssets()
        {
            if (string.IsNullOrEmpty(_cachedGoogleSheetsCsv))
            {
                EditorUtility.DisplayDialog("Generation Failed",
                    "No cached Google Sheets data. Please re-validate the source.", "OK");
                return;
            }

            var rawData = CSVReader.Parse(_cachedGoogleSheetsCsv, ",", _settings.CommentPrefix, _settings.HeaderRowCount);
            if (rawData == null || rawData.Headers == null || rawData.Headers.Length == 0)
            {
                EditorUtility.DisplayDialog("Generation Failed",
                    "Failed to parse Google Sheets data.", "OK");
                return;
            }

            var schema = SchemaBuilder.Build(rawData, _settings);

            if (string.IsNullOrEmpty(schema.ClassName))
                schema.ClassName = "GoogleSheetsEntry";
            if (string.IsNullOrEmpty(schema.DatabaseName))
                schema.DatabaseName = schema.ClassName + "Database";

            var result = CodeGenerator.Generate(schema, _settings);

            Debug.Log($"Generated scripts from Google Sheet '{schema.ClassName}' ({schema.Columns.Length} columns, {schema.Rows.Count} rows)");
            EditorUtility.DisplayDialog("Generation Complete",
                $"Generated scripts from Google Sheet '{schema.ClassName}':\n\n" +
                $"{schema.Columns.Length} columns, {schema.Rows.Count} rows\n" +
                $"Entry: {result.EntryClassPath}\n" +
                $"Database: {result.DatabaseClassPath}", "OK");
        }

        private void GenerateSQLiteAssets()
        {
            var selectedTableNames = _tableSelections.Where(t => t.selected).Select(t => t.tableName).ToArray();
            if (selectedTableNames.Length == 0)
            {
                EditorUtility.DisplayDialog("No Tables Selected",
                    "Please select at least one table to import.", "OK");
                return;
            }

            var validator = new SQLiteValidator(_sourceFilePath, _settings);
            var schemas = validator.ExtractSchemas();

            if (schemas == null || schemas.Length == 0)
            {
                EditorUtility.DisplayDialog("Generation Failed",
                    "Failed to extract schemas from SQLite database.", "OK");
                return;
            }

            var selectedSchemas = schemas.Where(s => selectedTableNames.Contains(s.SourceTableName)).ToArray();

            foreach (var schema in selectedSchemas)
            {
                CodeGenerator.Generate(schema, _settings);
                Debug.Log($"  Generated: {schema.ClassName} ({schema.Columns.Length} columns, {schema.Rows.Count} rows)");
            }

            Debug.Log($"Successfully generated scripts for {selectedSchemas.Length} table(s)");
            EditorUtility.DisplayDialog("Generation Complete",
                $"Generated scripts for {selectedSchemas.Length} table(s):\n\n" +
                string.Join("\n", selectedSchemas.Select(s =>
                    $"{s.ClassName}: {s.Columns.Length} columns, {s.Rows.Count} rows")), "OK");
        }

        #endregion

        #region Utilities

        private void LoadSettings()
        {
            _settings.ScriptOutputPath = EditorPrefs.GetString("D2SO_ScriptOutputPath", "Assets/Scripts/Generated/");
            _settings.AssetOutputPath = EditorPrefs.GetString("D2SO_AssetOutputPath", "Assets/Data/");
            _settings.NamespaceName = EditorPrefs.GetString("D2SO_Namespace", "");
            _settings.GenerateTooltips = EditorPrefs.GetBool("D2SO_GenerateTooltips", false);
            _settings.SanitizeFieldNames = EditorPrefs.GetBool("D2SO_SanitizeFieldNames", true);
            _settings.UseListInsteadOfArray = EditorPrefs.GetBool("D2SO_UseList", true);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString("D2SO_ScriptOutputPath", _settings.ScriptOutputPath);
            EditorPrefs.SetString("D2SO_AssetOutputPath", _settings.AssetOutputPath);
            EditorPrefs.SetString("D2SO_Namespace", _settings.NamespaceName ?? "");
            EditorPrefs.SetBool("D2SO_GenerateTooltips", _settings.GenerateTooltips);
            EditorPrefs.SetBool("D2SO_SanitizeFieldNames", _settings.SanitizeFieldNames);
            EditorPrefs.SetBool("D2SO_UseList", _settings.UseListInsteadOfArray);
        }

        private static string ConvertAssetPathToAbsolute(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages/"))
                return Path.GetFullPath(assetPath);
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        #endregion

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
