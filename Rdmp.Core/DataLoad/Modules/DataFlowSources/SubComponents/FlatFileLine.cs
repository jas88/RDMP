// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using CsvHelper;

namespace Rdmp.Core.DataLoad.Modules.DataFlowSources.SubComponents;

/// <summary>
/// Point in time record of a line read from CsvHelper including its <see cref="LineNumber"/>.  Can include multiple lines
/// of the underlying file if there is proper qualifying quotes and newlines in the csv e.g. when including free text columns.
/// </summary>
public class FlatFileLine
{
    /// <summary>
    /// The RAW file line number that this line reflects.  Where a record spans multiple lines (e.g. when it has newlines in quote qualified fields) it
    /// is the line number the record starts on
    /// </summary>
    public int LineNumber { get; private set; }

    /// <summary>
    /// The values as interpreted by CsvHelper for the current line
    /// </summary>
    public string[] Cells { get; set; }

    /// <summary>
    /// The absolute text as it appears in the flat file being read for this 'line', without the line break that ends it
    /// </summary>
    public string RawRecord { get; set; }

    /// <summary>
    /// Records the line that <paramref name="reader"/> is currently on
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="restoreRawRecord">Optional function that undoes any change made to the raw text before CsvHelper read it (e.g. the
    /// stand in character used for a multi character separator)</param>
    public FlatFileLine(CsvReader reader, Func<string, string> restoreRawRecord = null)
    {
        LineNumber = checked((int)reader.LineNumber);

        var current = reader.Current;
        Cells = current.ToArray();
        RawRecord = Restore(current.ToString(), restoreRawRecord);
    }

    /// <summary>
    /// Records a line that CsvHelper reported as malformed (e.g. a quote in the middle of a cell)
    /// </summary>
    /// <param name="bad"></param>
    /// <param name="restoreRawRecord">See <see cref="FlatFileLine(CsvReader,Func{string,string})"/></param>
    public FlatFileLine(CsvMalformedRecord bad, Func<string, string> restoreRawRecord = null)
    {
        LineNumber = checked((int)bad.LineNumber);
        Cells = Array.Empty<string>();
        RawRecord = Restore(bad.RawRecord, restoreRawRecord);
    }

    private static string Restore(string raw, Func<string, string> restoreRawRecord) =>
        raw == null || restoreRawRecord == null ? raw : restoreRawRecord(raw);

    public string this[int i] => Cells[i];

    public string GetLineDescription() => $"line {LineNumber}";
}
