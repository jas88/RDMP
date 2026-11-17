// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ExcelDataReader;
using FAnsi.Discovery;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.DataLoad.Modules.Exceptions;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Rdmp.Core.DataLoad.Modules.DataFlowSources;

/// <summary>
/// Pipeline component for reading from Microsoft Excel files.  Reads only from a single worksheet (by default the first one in the workbook).  Data read
/// is returned as a DataTable all read at once in one big batch.  Both .xls and .xlsx are supported (via ExcelDataReader, Microsoft Office is not required).
/// </summary>
public class ExcelDataFlowSource : IPluginDataFlowSource<DataTable>, IPipelineRequirement<FlatFileToLoad>
{
    public const string WorkSheetName_DemandDescription =
        "Name of the worksheet to load data from (single sheet name only).  If this is empty then the first sheet in the spreadsheet will be loaded instead";

    public const string AddFilenameColumnNamed_DemandDescription =
        "Optional - Set to the name of a column in your RAW database (e.g. Filename).  If set this named column will be populated with the path to the file being read (e.g. c:\\myproj\\Data\\ForLoading\\MyFile.csv)";

    [DemandsInitialization(WorkSheetName_DemandDescription)]
    public string WorkSheetName { get; set; }

    [DemandsInitialization(DelimitedFlatFileDataFlowSource.MakeHeaderNamesSane_DemandDescription,
        DemandType.Unspecified, true)]
    public bool MakeHeaderNamesSane { get; set; }

    [DemandsInitialization(AddFilenameColumnNamed_DemandDescription)]
    public string AddFilenameColumnNamed { get; set; }

    private FlatFileToLoad _fileToLoad;

    private DataTable dataReadFromFile;
    private bool haveDispatchedDataTable;

    public DataTable GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken, int rowOffset = 0, int columnOffset = 0, string[] replacementHeadersSplit = null)
    {
        dataReadFromFile ??= GetAllData(listener, cancellationToken, rowOffset, columnOffset, replacementHeadersSplit);

        if (haveDispatchedDataTable)
            return null;

        haveDispatchedDataTable = true;

        return dataReadFromFile;
    }

    private DataTable GetAllData(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken, int rowOffset = 0, int columnOffset = 0, string[] replacementHeadersSplit = null)
    {
        if (_fileToLoad == null)
            throw new Exception("_fileToLoad has not been set yet, possibly component has not been Initialized yet");

        if (!IsAcceptableFileExtension())
            throw new Exception($"FileToLoad ({_fileToLoad.File.FullName}) extension was not XLS or XLSX, dubious");

        using var workbook = ExcelWorkbook.Open(_fileToLoad.File);

        //if the user hasn't picked one, use the first
        if (!string.IsNullOrWhiteSpace(WorkSheetName) && !MoveToWorksheet(workbook.Reader, WorkSheetName))
            throw new FlatFileLoadException(
                $"The Excel sheet '{WorkSheetName}' was not found in workbook '{_fileToLoad.File.Name}'");

        var sheetName = workbook.Reader.Name;
        var toReturn = GetAllData(workbook, listener, rowOffset, columnOffset, replacementHeadersSplit);

        try
        {
            //set the table name the file name
            toReturn.TableName =
                QuerySyntaxHelper.MakeHeaderNameSensible(Path.GetFileNameWithoutExtension(_fileToLoad.File.Name));

            if (toReturn.Columns.Count == 0)
                throw new FlatFileLoadException(
                    $"The Excel sheet '{sheetName}' in workbook '{_fileToLoad.File.Name}' is empty");

            //if the user wants a column in the DataTable storing the filename loaded add it
            if (!string.IsNullOrWhiteSpace(AddFilenameColumnNamed))
            {
                toReturn.Columns.Add(AddFilenameColumnNamed);
                foreach (DataRow dataRow in toReturn.Rows)
                    dataRow[AddFilenameColumnNamed] = _fileToLoad.File.FullName;
            }
        }
        finally
        {
            toReturn.EndLoadData();
        }

        return toReturn;
    }

    /// <summary>
    /// Advances <paramref name="reader"/> to the worksheet called <paramref name="worksheetName"/> (case insensitive).
    /// Returns false if there is no such worksheet.
    /// </summary>
    private static bool MoveToWorksheet(IExcelDataReader reader, string worksheetName)
    {
        do
        {
            if (string.Equals(reader.Name, worksheetName, StringComparison.OrdinalIgnoreCase))
                return true;
        } while (reader.NextResult());

        return false;
    }

    /// <summary>
    /// Returns all data held in the current worksheet of <paramref name="workbook"/> (it is not advanced to the next worksheet).
    /// The first row of data becomes the headers.  Throws away fully blank columns/rows.
    /// </summary>
    /// <param name="workbook">Workbook whose reader is positioned at the worksheet to read</param>
    /// <param name="listener"></param>
    /// <param name="rowOffset">1 based row number of the first row which may contain the headers (rows above it are ignored)</param>
    /// <param name="columnOffset">0 based index of the first column which may contain a header</param>
    /// <param name="replacementHeadersSplit"></param>
    /// <returns></returns>
    public DataTable GetAllData(ExcelWorkbook workbook, IDataLoadEventListener listener, int rowOffset = 0, int columnOffset = 0, string[] replacementHeadersSplit = null)
    {
        var reader = workbook.Reader;
        var toReturn = new DataTable();
        toReturn.BeginLoadData();

        var nColumns = -1;
        var rowIndex = -1; // 0 indexed, ExcelDataReader returns every row including physically absent ones

        var nonBlankColumns = new Dictionary<int, DataColumn>();

        while (reader.Read())
        {
            rowIndex++;
            if (rowOffset - 1 > rowIndex) continue;

            var cells = GetCells(reader);

            //if all the cells in the current row are blank skip it (eliminates top of file whitespace)
            if (cells.All(c => !ExcelCellReader.HasContent(c.RawValue)))
                continue;

            //first row (that has any data in it) - makes headers
            if (nColumns == -1)
            {
                nColumns = cells.Count;
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                    $"Excel sheet {reader.Name} contains {nColumns}"));

                var replacing = replacementHeadersSplit is not null && replacementHeadersSplit.Any();

                if (replacing && replacementHeadersSplit.Length != nColumns)
                    listener.OnNotify(this,
                        new NotifyEventArgs(ProgressEventType.Error,
                            $"ForceReplacementHeaders was set but it had {replacementHeadersSplit.Length} column header names while the file had {nColumns} (there must be the same number of replacement headers as headers in the excel file)"));

                var originalHeaders = new string[nColumns];
                for (var i = 0; i < nColumns; i++)
                {
                    var (columnIndex, rawValue) = cells[i];
                    if (columnIndex < columnOffset) continue;

                    var h = ExcelCellReader.GetHeaderText(rawValue, workbook.IsDate1904);
                    if (replacing && replacementHeadersSplit.Length == nColumns)
                    {
                        originalHeaders[i] = h;
                        h = replacementHeadersSplit[i];
                    }

                    //if the cell header is blank
                    if (string.IsNullOrWhiteSpace(h))
                        continue;

                    nonBlankColumns.Add(columnIndex, toReturn.Columns.Add(h));
                }

                if (replacing)
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                    $"Force headers will make the following header changes:{GenerateASCIIArtOfSubstitutions(originalHeaders, replacementHeadersSplit)}"));

                continue;
            }

            //the rest of the rows
            var r = toReturn.Rows.Add();

            var gotAtLeastOneGoodValue = false;

            foreach (var (columnIndex, _) in cells)
            {
                var value = workbook.GetCellValue(columnIndex);

                //if the cell is blank skip it
                if (IsNull(value))
                    continue;

                //were we expecting this to be blank?
                if (!nonBlankColumns.TryGetValue(columnIndex, out var column))
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                        $"Discarded the following data (that was found in unnamed columns):{value}"));
                    continue;
                }

                r[column] = value.ToString();
                gotAtLeastOneGoodValue = true;
            }

            //if we didn't get any values at all for the row throw it away
            if (!gotAtLeastOneGoodValue)
                toReturn.Rows.Remove(r);
        }

        return toReturn;
    }

    /// <summary>
    /// Returns the cells of the current row which have a value (empty cells and error cells are omitted), in column order.
    /// </summary>
    private static List<(int ColumnIndex, object RawValue)> GetCells(IExcelDataReader reader)
    {
        var cells = new List<(int, object)>();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var raw = reader.GetValue(i);
            if (raw != null)
                cells.Add((i, raw));
        }

        return cells;
    }

    private static string GenerateASCIIArtOfSubstitutions(string[] headers,
      string[] replacements)
    {
        var sb = new StringBuilder("");

        var max = Math.Max(replacements.Length, headers.Length);

        for (var i = 0; i < max; i++)
        {
            var replacement = i >= replacements.Length ? "???" : replacements[i];
            var original = i >= headers.Length ? "???" : headers[i];

            sb.Append($"{Environment.NewLine}[{i}]{original}>>>{replacement}");
        }

        return sb.ToString();
    }

    private string[] acceptedFileExtensions =
    {
        ".xlsx",
        ".xls"
    };

    private bool IsAcceptableFileExtension() => acceptedFileExtensions.Contains(_fileToLoad.File.Extension.ToLower());

    private static bool IsNull(object o) => o == null || o == DBNull.Value || string.IsNullOrWhiteSpace(o.ToString());

    public void Check(ICheckNotifier notifier)
    {
        if (_fileToLoad != null)
            if (!IsAcceptableFileExtension())
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        $"File extension {_fileToLoad.File} has an invalid extension:{_fileToLoad.File.Extension} (this class only accepts:{string.Join(",", acceptedFileExtensions)})",
                        CheckResult.Fail));
            else
                notifier.OnCheckPerformed(
                    new CheckEventArgs($"File extension of file {_fileToLoad.File.Name} is acceptable",
                        CheckResult.Success));
        else
            notifier.OnCheckPerformed(
                new CheckEventArgs(
                    "FlatFileToLoad (Pipeline Requirement) was not met (we weren't initialized with a file)",
                    CheckResult.Warning));
    }

    public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {
    }

    public void Abort(IDataLoadEventListener listener)
    {
    }

    public DataTable TryGetPreview()
    {
        var timeoutToken = new CancellationTokenSource();
        timeoutToken.CancelAfter(10000);

        var token = new GracefulCancellationToken(timeoutToken.Token, timeoutToken.Token);

        DataTable dt;
        try
        {
            dt = GetAllData(ThrowImmediatelyDataLoadEventListener.Quiet, token);
        }
        catch (Exception e)
        {
            if (timeoutToken.IsCancellationRequested)
                throw new Exception(
                    "Failed to generate preview in 10 seconds or less, giving up trying to load a preview (this doesn't mean that the source is broken, more likely you just have a big file or something)",
                    e);

            throw;
        }

        return dt;
    }

    public void PreInitialize(FlatFileToLoad value, IDataLoadEventListener listener)
    {
        _fileToLoad = value;
    }

    public DataTable GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        return GetChunk(listener, cancellationToken, 0, 0);
    }
}
