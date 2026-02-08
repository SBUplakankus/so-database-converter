# Google Sheets Import

## Overview

Google Sheets import downloads a published Google Sheet as CSV and processes it through the same pipeline as local CSV files. This allows teams to collaborate on game data in Google Sheets and import it directly into Unity.

## Setup

### Sharing the Sheet

The Google Sheet must be shared publicly for the tool to download it:

1. Open the Google Sheet in your browser.
2. Click **Share** in the top-right corner.
3. Under **General access**, select **Anyone with the link**.
4. Set the permission to **Viewer**.
5. Click **Done**.

### Entering the URL

In the importer window, select **Google Sheets** as the source type and paste one of the following:

- **Full URL**: `https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit#gid=0`
- **Share URL**: `https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit?usp=sharing`
- **Raw Spreadsheet ID**: `1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgVE2upms`

The tool automatically extracts the spreadsheet ID and GID (sheet tab index) from the URL.

## Sheet Format

The Google Sheet should follow the same multi-row header convention as CSV files:

| Row | Content |
|-----|---------|
| 1 | Column headers |
| 2 | Type hints (optional) |
| 3 | Flags and attributes (optional) |
| 4+ | Data rows |

Directives (`#class:`, `#database:`, `#namespace:`) can be placed in rows before the header row.

## Multi-Sheet Workbooks

To import a specific sheet tab from a workbook, ensure the URL contains the `gid` parameter:

```
https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit#gid=123456
```

If no `gid` is specified, the first sheet (gid=0) is imported.

## Limitations

- The sheet must be publicly accessible (no authentication support).
- Requires an active internet connection during validation and generation.
- Very large sheets may take time to download.
