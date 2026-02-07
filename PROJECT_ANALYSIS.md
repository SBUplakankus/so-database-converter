# Data to ScriptableObject — Full Project Analysis & Student Dev Guide

## Token Budget Strategy (Student Pro Plan)

You have limited premium requests. Here's how to spend them wisely.

### What Uses Premium Requests

| Action | Model | Premium Request? |
|--------|-------|-----------------|
| Chat with Opus (model picker) | Opus | YES — 1 per message |
| Chat with Sonnet (model picker) | Sonnet | NO (or minimal) |
| Coding agent PR (default) | Sonnet | YES — 1 per session |
| Coding agent PR (Opus override) | Opus | YES — costs more |
| Copilot autocomplete in Rider | Sonnet | NO |

### Optimal Spend Strategy

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  ALREADY SPENT (this conversation):                          │
│  ✅ Architecture design                                      │
│  ✅ CSV format spec                                          │
│  ✅ SQLite integration design                                │
│  ✅ Editor window layout                                     │
│  ✅ Validation chains                                        │
│  ✅ Full agent guide docs (V1 + V2)                          │
│  ✅ Project setup instructions                               │
│  ✅ This analysis doc                                        │
│                                                              │
│  REMAINING OPUS BUDGET — use for:                            │
│  • Debugging complex multi-file issues                       │
│  • Redesigning a system that isn't working                   │
│  • Reviewing agent PRs when the diff is large                │
│  • Planning V2 SQLite if architecture needs change           │
│                                                              │
│  USE SONNET (free/cheap) FOR:                                │
│  • All agent PR sessions                                     │
│  • "How do I do X in Unity" questions                        │
│  • "Fix this compiler error" questions                       │
│  • Code review of small changes                              │
│  • Writing README sections                                   │
│  • Generating test cases                                     │
│                                                              │
│  USE RIDER COPILOT AUTOCOMPLETE (free) FOR:                  │
│  • Boilerplate code while editing                            │
│  • Filling in method bodies                                  │
│  • Writing repetitive test cases                             │
│  • Documentation comments                                    │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## Project Scope Estimate

### V1 (CSV + Google Sheets)

| Component | Files | Estimated Lines of Code | Complexity |
|-----------|-------|------------------------|------------|
| Models | 10 | ~350 | Low — data classes |
| CSV Reader | 1 | ~250 | Medium — RFC 4180 parsing |
| Schema Builder | 1 | ~300 | Medium — type/flag parsing |
| Type Inference | 1 | ~80 | Low |
| Type Converters | 1 | ~250 | Low — repetitive conversions |
| Name Sanitizer | 1 | ~100 | Low |
| Code Generator | 1 | ~350 | Medium — StringBuilder templates |
| Reflection Mapper | 1 | ~120 | Medium |
| Asset Populator | 1 | ~250 | Medium — AssetDatabase calls |
| Recompile Handler | 1 | ~80 | Medium — domain reload edge cases |
| Pending Import State | 1 | ~60 | Low |
| Google Sheets URL Parser | 1 | ~60 | Low — regex |
| Google Sheets Reader | 1 | ~100 | Medium — HTTP + error handling |
| CSV Validator | 1 | ~80 | Low |
| Google Sheets Validator | 1 | ~120 | Medium — async HTTP |
| ISourceReader Interface | 1 | ~15 | Low |
| Editor Window (ImporterWindow) | 1 | ~500 | High — lots of EditorGUILayout |
| Preview Table UI | 1 | ~150 | Medium |
| Column Mapping UI | 1 | ~120 | Medium |
| Settings Panel UI | 1 | ~100 | Low — toggles |
| Import Reporter UI | 1 | ~80 | Low |
| Tests | 8 | ~800 | Low — mechanical |
| Config files | 4 | ~80 | Low — JSON |
| Samples + docs | 4 | ~100 | Low |
| **TOTAL V1** | **~45 files** | **~4,500 LOC** | |

### V2 (SQLite Addon)

| Component | Files | Estimated Lines of Code | Complexity |
|-----------|-------|------------------------|------------|
| SQLite Reader | 1 | ~200 | Medium |
| SQLite Type Mapper | 1 | ~80 | Low |
| SQLite Schema Parser | 1 | ~120 | Medium — regex on DDL |
| Dependency Resolver | 1 | ~120 | Medium — topological sort |
| Foreign Key Resolver | 1 | ~80 | Low |
| SQLite Validator | 1 | ~150 | Medium |
| sqlite-net dependency | 1 | ~3000 (embedded, not yours) | N/A |
| Editor Window modifications | 0 (modify existing) | ~200 added | Medium |
| Asset Populator modifications | 0 (modify existing) | ~100 added | Medium |
| Tests | 3 | ~250 | Low |
| Samples + docs | 2 | ~100 | Low |
| **TOTAL V2** | **~12 new files** | **~1,400 LOC** (excluding sqlite-net) | |

### Grand Total

```
V1: ~4,500 lines across ~45 files
V2: ~1,400 lines across ~12 new files
───────────────────────────────────
Total: ~5,900 lines across ~57 files
```

This is a medium-sized Unity package. Comparable in scope to popular open-source tools like Odin's serialization subset or NaughtyAttributes.

---

## Risk Assessment

### High Risk (Things Most Likely to Need Fixing)

| Risk | Why | Mitigation |
|------|-----|------------|
| Editor Window layout | EditorGUILayout is trial-and-error. Spacing, scrolling, foldout state, and repainting are fiddly. Agent can't see the UI. | Expect to spend 1-2 hours manually tweaking the window in Unity after the agent PR. |
| Recompile handler resuming | Domain reload in Unity destroys all editor state. Surviving it via EditorPrefs + InitializeOnLoad is the #1 source of subtle bugs in editor tools. | Test this manually with a real CSV. If it fails, this is worth an Opus chat to debug. |
| AssetDatabase timing | `CreateAsset`, `Refresh`, `SaveAssets` have ordering sensitivities. Sometimes you need `Refresh` before `LoadAssetAtPath` works. | Test the full generate pipeline manually. If assets are missing or null, check call ordering. |
| RFC 4180 CSV edge cases | Quoted fields with newlines, escaped quotes inside quotes, mixed line endings — the CSV "standard" has many edge cases. | The test suite covers the main cases. Test with a real Google Sheets export to catch encoding issues. |

### Medium Risk

| Risk | Why | Mitigation |
|------|-----|------------|
| Google Sheets HTTP error handling | Can't test network calls in agent session. The HTML response trap (200 OK with HTML body) is a known gotcha. | Test manually with a real Sheets URL. Test with a private sheet to verify 403 handling. |
| Type conversion edge cases | Locale-dependent float parsing (`3,14` vs `3.14`), Color parsing from hex, Vector parsing with parentheses. | InvariantCulture is specified in the guide. Test on a machine with a non-English locale if possible. |
| Enum code generation | Generated enum names must be valid C# identifiers. Values like "Fire Bolt" or "3-star" need sanitization. | NameSanitizer handles this, but test with adversarial enum values. |

### Low Risk

| Risk | Why | Mitigation |
|------|-----|------------|
| Model classes | Pure data structures, no logic. | Almost impossible to get wrong. |
| Name sanitizer | Well-defined string transformation rules. | Comprehensive tests in the guide. |
| Package.json / asmdef | Static JSON files. | Validated by Unity on load — errors are immediately visible. |
| Test infrastructure | NUnit on Unity is well-documented. | Tests either run or they don't — easy to debug. |

---

## Agent Session Strategy

### Recommended: Two PRs, Not One

Splitting into two agent PRs gives better results than one massive PR:

**PR 1: Foundation (Models + Core Logic + Tests)**
```
Scope:
- All model classes (Phase 1)
- CSVReader, SchemaBuilder, TypeInference (Phase 2)
- NameSanitizer
- TypeConverters
- GoogleSheetsURLParser
- CodeGenerator (Phase 3)
- ReflectionMapper (Phase 5)
- All tests for the above
- package.json, .asmdef files, .gitignore
- Sample CSV files

NOT included:
- Editor Window UI
- AssetPopulator (needs Unity to test)
- Recompile Handler
- Validators (Google Sheets needs network)
```

Why this split: Everything in PR 1 is **pure C# logic** with no Unity Editor dependencies beyond basic types. The agent can write and test this confidently. It's the foundation that everything else builds on.

**PR 2: Editor Integration (UI + Asset Pipeline + Validators)**
```
Scope:
- ImporterWindow.cs (Editor Window)
- PreviewTable, ColumnMappingUI, SettingsPanel, ImportReporter
- AssetPopulator
- RecompileHandler + PendingImportState
- CSVValidator, GoogleSheetsValidator
- GoogleSheetsReader
- ISourceReader interface implementation on validators

Depends on:
- PR 1 must be merged first
```

Why this split: PR 2 is all Unity Editor integration. It references `EditorGUILayout`, `AssetDatabase`, `UnityWebRequest`, `EditorPrefs`, `InitializeOnLoad`. These are harder to get perfect without running Unity, so you'll likely need to tweak after merging. Keeping it separate means PR 1 is clean and stable as a foundation.

### Problem Statement for PR 1

When you create the PR, use this as the problem statement:

```
Implement the foundation of the Data to ScriptableObject Unity package
as described in AGENT_GUIDE_V1.md.

Implement Phases 1 through 3, Phase 5, and Phase 9 (tests for those phases):

Phase 1: All model classes in Models/ folder
Phase 2: CSVReader, SchemaBuilder, TypeInference, NameSanitizer in Editor/Core/
Phase 3: CodeGenerator in Editor/Core/
Phase 5: ReflectionMapper in Editor/Core/
Also: TypeConverters, GoogleSheetsURLParser, ISourceReader interface

Plus all related tests, package.json, all .asmdef files, .gitignore,
and sample CSV files.

Do NOT implement the Editor Window UI, AssetPopulator, RecompileHandler,
PendingImportState, or Validators yet — those come in a follow-up PR.

Follow the specifications in AGENT_GUIDE_V1.md exactly. Use the namespace
DataToScriptableObject for Models and DataToScriptableObject.Editor for
Editor code.
```

### Problem Statement for PR 2

```
Implement the Editor integration layer for the Data to ScriptableObject
Unity package as described in AGENT_GUIDE_V1.md.

This builds on the foundation from the previous PR. Implement:

Phase 4: RecompileHandler + PendingImportState in Editor/Utility/
Phase 6: AssetPopulator in Editor/Core/
Phase 7: CSVValidator, GoogleSheetsValidator, GoogleSheetsReader in
         Editor/Validation/ and Editor/Core/
Phase 8: Full Editor Window UI — ImporterWindow, PreviewTable,
         ColumnMappingUI, SettingsPanel, ImportReporter in Editor/UI/

Follow the specifications in AGENT_GUIDE_V1.md exactly.
```

---

## Post-Merge Manual Work

After each agent PR is merged, you need to do some work locally that the agent cannot do.

### After PR 1 Merge

```
Time estimate: 30-60 minutes

☐ Create Unity dev project (Unity Hub → New Project → 2021.3 LTS)
☐ Clone repo into Packages/com.data-to-scriptableobject/
☐ Open Unity → wait for compilation
☐ Fix any compile errors (likely 0-2 minor issues)
☐ Commit all generated .meta files
☐ Open Test Runner (Window > General > Test Runner)
☐ Run all tests → fix any failures
☐ Commit test fixes if needed
☐ Push .meta files + any fixes
```

### After PR 2 Merge

```
Time estimate: 1-3 hours (mostly UI tweaking)

☐ Pull latest, let Unity recompile
☐ Fix any compile errors
☐ Commit new .meta files
☐ Open Tools > Data to ScriptableObject
☐ Test with items_example.csv from Samples
☐ Tweak Editor Window layout:
  ☐ Scroll position behavior
  ☐ Foldout default states
  ☐ Field widths and spacing
  ☐ Status bar colors/icons
  ☐ Button enable/disable states
☐ Test full pipeline:
  ☐ Generate New Class mode → verify .cs file generated
  ☐ Wait for recompile → verify assets created
  ☐ Use Existing Class mode → verify reflection mapping
  ☐ Re-import with changes → verify overwrite works
  ☐ Test with Google Sheets URL (need a real published sheet)
☐ Fix issues found during testing
☐ Commit and push
```

### Creating a Test Google Sheet

For testing the Google Sheets integration, create a test sheet:

```
1. Go to sheets.google.com → create new sheet
2. Enter test data matching items_example.csv format:
   Row 1: id,name,description,damage,element
   Row 2: int,string,string,float,enum
   Row 3: key,name,optional,"range(0;100)","Fire,Water,Earth"
   Row 4+: data rows
3. File > Share > Publish to Web
   → Select "Comma-separated values (.csv)"
   → Click Publish
4. Copy the generated URL
5. Also copy the regular edit URL
6. Test both URL formats in your tool
```

---

## Unity Package Development — What You Need to Know

### Versioning

Follow semantic versioning: `MAJOR.MINOR.PATCH`

```
0.1.0  → First agent PR merged, foundation working
0.2.0  → Editor Window + full pipeline working
0.3.0  → Google Sheets integration tested and polished
0.4.0  → Bug fixes from community feedback
0.5.0  → SQLite support (V2)
1.0.0  → Stable, tested, documented — first official release
```

Update `version` in `package.json` AND create a git tag:
```bash
git tag v0.1.0
git push origin v0.1.0
```

Users can then install a specific version:
```
https://github.com/SBUplakankus/DataToScriptableObject.git#v0.1.0
```

### CHANGELOG.md

Start maintaining this from day one:

```markdown
# Changelog

## [0.2.0] - 2026-XX-XX
### Added
- Editor Window UI with full import pipeline
- Asset generation from CSV data
- Google Sheets URL import support
- Recompile handler for code generation mode
- Use Existing Class mode with reflection mapping

## [0.1.0] - 2026-XX-XX
### Added
- Initial package structure
- CSV parser with RFC 4180 support
- Schema builder with multi-row header convention
- Type inference engine
- Code generator for ScriptableObject classes
- Type converters for all supported types
- Name sanitizer for C# fields and file names
- Google Sheets URL parser
- Sample CSV files
- Unit tests for all core components
```

### Documentation

Your README.md should cover:

```markdown
# Sections needed:

1. What is this? (one paragraph)
2. Features list (bullet points)
3. Installation (UPM git URL)
4. Quick Start (5-step workflow with screenshots)
5. CSV Format Reference (the multi-row header spec)
6. Editor Window Reference (all options explained)
7. Google Sheets Setup (how to publish a sheet)
8. Supported Types (table of all types)
9. FAQ / Troubleshooting
10. Contributing
11. License
```

Screenshots are critical for a Unity tool. After the Editor Window is working, take screenshots of:
- The full Editor Window
- The preview table
- Generated ScriptableObject in the Unity Inspector
- The generated C# class in Rider
- Before/after (CSV file → SO assets in Project window)

### Testing on Multiple Unity Versions

Before tagging v1.0.0, test on:
- Unity 2021.3 LTS (minimum supported)
- Unity 2022.3 LTS
- Unity 6 (2023.x/6000.x)

The main risk is API deprecations. `EditorGUILayout` is stable, but some methods get deprecated between major versions. If you find issues, use `#if UNITY_2022_1_OR_NEWER` preprocessor directives.

### OpenUPM (Optional, Later)

For maximum discoverability, register your package on [OpenUPM](https://openupm.com/) after v1.0.0. This lets users install via:
```bash
openupm add com.data-to-scriptableobject
```

This is optional and only worth doing once the package is stable.

---

## Development Timeline Estimate

Assuming part-time student schedule:

| Phase | Work | Time Estimate | Premium Requests |
|-------|------|---------------|-----------------|
| Create repo + agent guide | Push AGENT_GUIDE_V1.md to repo | 15 minutes | 0 |
| Agent PR 1 | Foundation code | 1 agent session | 1 |
| Post PR 1 | Unity setup, .meta files, test fixes | 1 hour | 0 |
| Agent PR 2 | Editor integration | 1 agent session | 1 |
| Post PR 2 | UI tweaking, manual testing | 2-3 hours | 0 |
| Polish | Bug fixes, README, screenshots | 2-3 hours | 0-2 (Sonnet chat) |
| **V1 Total** | | **~8 hours of your time** | **2-4 premium requests** |
| Agent PR 3 | SQLite V2 (push AGENT_GUIDE_V2_SQLITE.md) | 1 agent session | 1 |
| Post PR 3 | Testing, UI additions | 2-3 hours | 0 |
| **V2 Total** | | **~4 additional hours** | **1 premium request** |

**Total project: ~12 hours of your time + 3-5 premium requests.**

That's extremely efficient for a ~6,000 LOC open source tool.

---

## Quick Reference: Files From This Chat to Put in Your Repo

| File | Purpose | When to Add |
|------|---------|-------------|
| `AGENT_GUIDE_V1.md` | Agent instructions for CSV + Sheets | Before first agent PR |
| `AGENT_GUIDE_V2_SQLITE.md` | Agent instructions for SQLite | Before V2 agent PR |
| `PROJECT_ANALYSIS.md` | This document — your personal reference | Now (optional, can keep private) |

The agent guide files go IN the repo so the coding agent can read them as context when creating the PR. This analysis doc is for your own reference — commit it or keep it local, your choice.