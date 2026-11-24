# CsvHelper Usage Analysis in RDMP

This describes how RDMP uses [CsvHelper](https://github.com/JoshClose/CsvHelper) as of version 34.1.0.  Version 34 is a
rewrite of the library, so anything written about RDMP's use of version 33 and earlier (including the earlier version of
this document, which planned a move to CsvHandler against the version 33 API) no longer applies.

## Summary

RDMP uses CsvHelper in two ways:

- **Reading** delimited flat files in the data load engine.  This is the complicated use: it depends on line numbers,
  the raw text of each record, records of differing lengths and reports of badly quoted cells.
- **Writing** small CSV files, one field at a time.

RDMP does not use record mapping (reading or writing whole objects, with or without converters), dynamic records, the
data reader wrapper or the async methods, so the source generator that comes in the CsvHelper package has nothing to generate.

## Where it is used

| File | Use |
|---|---|
| `Rdmp.Core/DataLoad/Modules/DataFlowSources/DelimitedFlatFileDataFlowSource.cs` | Opens the file and reads its records in batches |
| `Rdmp.Core/DataLoad/Modules/DataFlowSources/SubComponents/FlatFileColumnCollection.cs` | Reads the headers |
| `Rdmp.Core/DataLoad/Modules/DataFlowSources/SubComponents/FlatFileLine.cs` | Copy of one record: line number, cells and raw text |
| `Rdmp.Core/DataLoad/Modules/DataFlowSources/SubComponents/FlatFileToDataTablePusher.cs` | Reads ahead to join records broken by line breaks |
| `Rdmp.Core/ReusableLibraryCode/Extensions/DataTableExtensions.cs` | `SaveAsCsv`, used by ExcelToCSVFilesConverter and to save the data of an aggregate graph |
| `Rdmp.Core/ReusableLibraryCode/UsefulStuff.cs` | `DataTableToCsv`, used to copy the data of an aggregate graph |
| `Rdmp.Core/Reports/GovernanceReport.cs` | Writes the governance report |
| `Tools/rdmp/CommandLine/Gui/ConsoleGuiSqlEditor.cs` | Saves query results |
| `RdmpDicom/Rdmp.Dicom/Cache/Pipeline/CFindSource.cs` | Writes C-FIND results |

`FlatFileEventHandlers.cs` decides what happens to bad records but does not call CsvHelper itself.  The extraction
destination (`CSVOutputFormat`) and `DicomTagToCSV` write their files by hand and do not use CsvHelper.

## Reading

### Settings

`DelimitedFlatFileDataFlowSource.OpenFile` creates the reader with these options:

| RDMP setting | CsvHelper option |
|---|---|
| Separator | `Separator` when it is a single printable ASCII character or tab, otherwise a `CsvSeparatorStream` (see below) |
| ForceHeaders | `HasHeader` is false when headers are forced |
| ForceHeadersReplacesFirstLineInFile | `ShouldSkipRecord`, which skips the first record it is offered |
| IgnoreBlankLines | `SkipEmptyLines` |
| IgnoreQuotes | `Mode` is `NoEscape`, otherwise `Rfc4180` |
| IgnoreBadReads, BadDataHandlingStrategy | `OnMalformedRecord` passes each badly quoted record to `FlatFileEventHandlers.BadDataFound` |

`TrackLineNumbers` is always on, because every warning and error names a line, and `DetectSeparatorHint` is always off,
because the separator is the one the user chose.  Everything else is left at its default: in particular the reader
recovers from malformed data and allows records whose field count differs from the header, so it gives RDMP every
record, however long, and RDMP decides what is bad.

The culture and date format settings of the source are not given to CsvHelper.  It only supplies strings; TypeGuesser
turns them into typed values.

### Separators

CsvHelper splits on one printable ASCII character or tab.  For any other separator (several characters such as `||`, or
a character that is not ASCII such as `¬`) the file is read through a `CsvSeparatorStream`, which puts a stand in
character (U+001F) where each separator was and leaves the separators inside quoted cells alone.  The stream is only
created when the separator needs it.  Three things follow from using it:

- The raw text of a record has the stand in character where the separator was.  `FlatFileLine` puts the separator back
  (`FlatFileToDataTablePusher.RestoreRawRecord`) so that diverted rows look as they did in the file.
- The stream reads UTF-8 only, so a file that starts with a UTF-16 or UTF-32 byte order mark is converted to UTF-8 as
  it is read.
- A file that has the stand in character outside a quoted cell cannot be read, and the load fails.

### What RDMP relies on

- `Headers` is available before the first `Read` and gives the header cells as they are in the file (padding, empty and
  duplicate names included).  It is empty when the file is empty.  `FlatFileColumnCollection` trims and renames them.
- `Read` returns false for a file that has only a header.
- `LineNumber` is the line a record starts on.  The lines that could not be loaded are remembered by this number.
- `Current.ToArray()` gives the cells of a record and `Current.ToString()` its raw text, without the line break that
  ends it.  `CsvRow` cannot be kept (it is a ref struct), which is why `FlatFileLine` copies both.
- A record may have fewer or more cells than the header.  `FlatFileToDataTablePusher` reads further records to complete
  a short one when AttemptToResolveNewLinesInRecords is on.
- `OnMalformedRecord` is called before `Read` returns the record it is about, with the same line number.
- `ShouldSkipRecord` is offered every record of a file that has no header, including the first.  Records it skips are
  not counted in the reader's row number, so the source keeps its own flag for the first record.

### Errors

Badly quoted cells do not throw: CsvHelper recovers and reports them, and RDMP ignores the report or applies the
BadDataHandlingStrategy.  A `CsvFormatException` from `Read` or `Headers` is therefore something CsvHelper could not
carry on from, and the load fails with a `FlatFileLoadException` whatever the strategy.

## Writing

Every writer is created with `CsvWriter.Create` and writes with `WriteField` and `EndRecord`.  The output does not depend
on the regional settings of the computer: the separator is a comma, numbers use `.` and dates are ISO 8601.

Things to keep in mind when changing these:

- `EndRecord` throws when the record has no fields, so tables with no columns are not written at all.
- The writer checks that every record has as many fields as the first.  The governance report has sections of differing
  widths, so it turns that off (`ValidateFieldCount`).
- An empty field is written as `""`, so the blank lines between the sections of the governance report are written to the
  underlying stream after a `Flush`.
- Nothing reaches the underlying writer until `Flush` or `Dispose`.  `DataTableToCsv` flushes before it reads the text.

## Tests

- Reading: the `DelimitedFileSourceTests*` classes in
  `Rdmp.Core.Tests/DataLoad/Engine/Integration/PipelineTests/Sources/`, and the attacher tests that need a database
  (`Test_CSV_Attachment`, `Test_FlatFileAttacher_AmbiguousDates`, `TabTestWithOverrideHeaders`).
- Writing: `DataTableExtensionsTests` and `ExcelConversionTest`.  The governance report has no tests.

## Replacing CsvHelper

A library that replaced CsvHelper would have to do everything under "What RDMP relies on": report the line a record
starts on, give its raw text, allow records of differing lengths, recover from bad quoting and say that it did, and read
separators that are not a single character.  The writers need nothing unusual.

[Pipeline]: ../../Documentation/CodeTutorials/Glossary.md#Pipeline
