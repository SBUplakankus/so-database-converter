# CSV Format Reference

## Structure

A CSV file consists of an optional prelude followed by header rows and data rows.

### Prelude (Optional)

Lines before the first non-comment, non-empty line can contain directives:

```
#class:ClassName
#database:DatabaseName
#namespace:My.Namespace
```

### Header Rows

The number of header rows is configurable (default: 3).

**1-row header**: Column names only. Types are inferred from data.

```csv
id,name,value
1,Sword,100
```

**2-row header**: Column names and type hints.

```csv
id,name,value
int,string,float
1,Sword,100.5
```

**3-row header**: Column names, type hints, and flags.

```csv
id,name,value
int,string,float
key,name,range(0;1000)
1,Sword,100.5
```

### Data Rows

All rows after the header rows are data. Empty rows and lines starting with the comment prefix (default: `#`) are skipped.

## Quoting Rules

The parser follows RFC 4180:

- Fields containing commas, newlines, or double quotes must be enclosed in double quotes.
- Double quotes within a quoted field are escaped by doubling: `""`.
- Leading/trailing whitespace in unquoted fields is preserved.

## Delimiter

The default delimiter is a comma (`,`). Tab-delimited and semicolon-delimited files are auto-detected when using `auto` mode.

## BOM Handling

UTF-8 BOM characters (`\uFEFF`) at the start of the file are automatically stripped.
