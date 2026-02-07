# CSV Parser Fix Summary

## What Was Done

### Problem
The custom CSV parser in `Editor/Core/CSVReader.cs` had multiple bugs causing 42 test failures:
- First character being removed from headers ("first" → "irst", "a" → "")
- Directive parsing failures (directives not being extracted)
- Issues with quoted fields and escape sequences

### Solution
Replaced the entire custom CSV parsing implementation with **Microsoft.VisualBasic.FileIO.TextFieldParser**, a robust, industry-standard CSV parser from the .NET BCL.

## Key Changes

### 1. CSVReader.cs Refactoring
**Before**: ~260 lines with custom ParseLine() method handling:
- Manual character-by-character parsing
- Quote handling logic
- Delimiter detection
- Multi-line field support

**After**: ~230 lines leveraging TextFieldParser:
- Simplified directive extraction (prelude scanning)
- All CSV complexity handled by TextFieldParser
- Cleaner, more maintainable code

### 2. Assembly Definition Update
**File**: `Editor/DataToScriptableObject.Editor.asmdef`
```json
{
    "overrideReferences": true,
    "precompiledReferences": [
        "Microsoft.VisualBasic.dll"
    ]
}
```

## Why TextFieldParser?

1. **Industry Standard**: Part of .NET BCL, used in production worldwide
2. **RFC 4180 Compliant**: Properly handles CSV escaping and quoting rules
3. **Well-Tested**: Decades of testing by Microsoft and the community
4. **Feature-Rich**: Handles all edge cases:
   - Quoted fields with embedded delimiters
   - Escaped quotes (double-quote escaping)
   - Multi-line fields
   - Various delimiters (comma, tab, semicolon, pipe, etc.)
   - Comment lines
5. **No External Dependencies**: Already part of .NET Framework

## Architecture

```
Parse(csvText) flow:
1. Normalize line endings
2. Extract directives from prelude (manual scan)
   - Directives (#class:, #database:, #namespace:)
   - Must appear before any CSV data
   - Comments and empty lines allowed in prelude
3. Pass remaining CSV data to TextFieldParser
   - Configure delimiter
   - Set comment tokens for data section
   - Handle quotes automatically
4. Post-process results
   - Separate headers, type hints, flags, data rows
   - Normalize row lengths
   - Apply padding/truncation as needed
```

## Benefits

- **Correctness**: TextFieldParser handles all CSV edge cases correctly
- **Maintainability**: Less custom code = fewer bugs
- **Performance**: Optimized native implementation
- **Reliability**: Battle-tested component
- **Standards Compliance**: Follows CSV RFC specifications

## Testing

Standalone tests confirm:
- ✓ 'a,b,c' correctly parsed as ['a', 'b', 'c'] (not missing first char)
- ✓ Tab delimiters work correctly
- ✓ Quoted fields with escapes handled properly
- ✓ Directives extracted from prelude correctly

**Next**: Run Unity Test Runner to confirm all 42 failing tests now pass.

## For Future Developers

**DO**:
- Continue using TextFieldParser for CSV parsing
- Let TextFieldParser handle quotes, escapes, delimiters
- Keep directive extraction logic separate (in prelude scan)

**DON'T**:
- Try to implement custom CSV parsing from scratch
- Modify TextFieldParser configuration unless necessary
- Mix directive handling into TextFieldParser logic

## Recommendation from StackOverflow

This solution follows the StackOverflow recommendation:
> "Don't reinvent the wheel. Take advantage of what's already in .NET BCL. Add a reference to the Microsoft.VisualBasic (yes, it says VisualBasic but it works in C# just as well - remember that at the end it is all just IL) use the Microsoft.VisualBasic.FileIO.TextFieldParser class to parse CSV file."

Source: Multiple StackOverflow answers on C# CSV parsing
