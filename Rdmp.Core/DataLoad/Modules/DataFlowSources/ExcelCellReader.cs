// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ExcelDataReader;
using ExcelNumberFormat;

namespace Rdmp.Core.DataLoad.Modules.DataFlowSources;

/// <summary>
/// Opens Excel workbooks (.xls, .xlsx, .xlsb) with ExcelDataReader and translates cell values into the representation RDMP
/// loads: strings for text, raw numbers for 'General' numeric cells and unambiguous date/time strings (or the Excel rendering)
/// for cells with a number format.  Formulas are never evaluated, the cached result stored in the file is used.
/// </summary>
public static class ExcelCellReader
{
    /// <summary>
    /// The first day which Excel (1900 date system) and DateTime.FromOADate agree on.  Before this Excel believes that
    /// 1900-02-29 exists so serial numbers are offset by one day.
    /// </summary>
    private static readonly DateTime FirstUnambiguousExcelDate = new(1900, 3, 1);

    /// <summary>
    /// Excel's built-in number formats (these are not stored in the workbook).  Using a fixed table means the formatting
    /// rules applied do not depend on the reading library's own interpretation of the built-in formats.
    /// </summary>
    private static readonly Dictionary<int, string> BuiltinFormats = new()
    {
        { 1, "0" }, { 2, "0.00" }, { 3, "#,##0" }, { 4, "#,##0.00" },
        { 5, "\"$\"#,##0_);(\"$\"#,##0)" }, { 6, "\"$\"#,##0_);[Red](\"$\"#,##0)" },
        { 7, "\"$\"#,##0.00_);(\"$\"#,##0.00)" }, { 8, "\"$\"#,##0.00_);[Red](\"$\"#,##0.00)" },
        { 9, "0%" }, { 10, "0.00%" }, { 11, "0.00E+00" }, { 12, "# ?/?" }, { 13, "# ??/??" },
        { 14, "m/d/yy" }, { 15, "d-mmm-yy" }, { 16, "d-mmm" }, { 17, "mmm-yy" },
        { 18, "h:mm AM/PM" }, { 19, "h:mm:ss AM/PM" }, { 20, "h:mm" }, { 21, "h:mm:ss" }, { 22, "m/d/yy h:mm" },
        { 37, "#,##0_);(#,##0)" }, { 38, "#,##0_);[Red](#,##0)" },
        { 39, "#,##0.00_);(#,##0.00)" }, { 40, "#,##0.00_);[Red](#,##0.00)" },
        { 41, "_(* #,##0_);_(* (#,##0);_(* \"-\"_);_(@_)" },
        { 42, "_(\"$\"* #,##0_);_(\"$\"* (#,##0);_(\"$\"* \"-\"_);_(@_)" },
        { 43, "_(* #,##0.00_);_(* (#,##0.00);_(* \"-\"??_);_(@_)" },
        { 44, "_(\"$\"* #,##0.00_);_(\"$\"* (#,##0.00);_(\"$\"* \"-\"??_);_(@_)" },
        { 45, "mm:ss" }, { 46, "[h]:mm:ss" }, { 47, "mm:ss.0" }, { 48, "##0.0E+0" }, { 49, "@" }
    };

    private static readonly ConcurrentDictionary<string, NumberFormat> NumberFormats = new();

    static ExcelCellReader()
    {
        // Legacy .xls files store text in Windows code pages which .NET (Core) does not provide by default
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Opens the workbook for reading, the format (xls/xlsx/xlsb) is detected from the file contents.  The returned reader
    /// owns the underlying file handle and is positioned at the first worksheet.
    /// </summary>
    public static IExcelDataReader Open(FileInfo file)
    {
        var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        try
        {
            return ExcelReaderFactory.CreateReader(stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Returns true if the cell has any content (i.e. is not empty, an error, or whitespace only text).
    /// </summary>
    public static bool HasContent(object rawValue) =>
        rawValue != null && !string.IsNullOrWhiteSpace(Convert.ToString(rawValue, CultureInfo.InvariantCulture));

    /// <summary>
    /// Returns the text of a header cell: strings as is, numbers (including date formatted numbers) as the raw number.
    /// </summary>
    public static string GetHeaderText(object rawValue) => rawValue switch
    {
        null => null,
        string s => s,
        DateTime dt => ToExcelSerial(dt).ToString(CultureInfo.CurrentCulture),
        bool b => b.ToString(),
        _ => Convert.ToDouble(rawValue, CultureInfo.InvariantCulture).ToString(CultureInfo.CurrentCulture)
    };

    /// <summary>
    /// Returns the C# value that best represents the contents of cell <paramref name="column"/> of the current row
    /// of <paramref name="reader"/> or null if it is blank, an error or the text "NULL".
    /// </summary>
    public static object GetCellValue(IExcelDataReader reader, int column)
    {
        var raw = reader.GetValue(column);

        switch (raw)
        {
            case null:
                return null;
            case string s:
                //if it is blank or 'null' then leave it null
                return string.IsNullOrWhiteSpace(s) || s.Trim().Equals("NULL", StringComparison.CurrentCultureIgnoreCase)
                    ? null
                    : s;
            case bool b:
                return b;
            case DateTime dt:
                return FormatNumber(ToExcelSerial(dt), dt, reader.GetNumberFormatIndex(column),
                    reader.GetNumberFormatString(column));
            case TimeSpan ts:
                return FormatNumber(ts.TotalDays, ToDateTime(ts.TotalDays), reader.GetNumberFormatIndex(column),
                    reader.GetNumberFormatString(column));
            default:
                var number = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                return FormatNumber(number, ToDateTime(number), reader.GetNumberFormatIndex(column),
                    reader.GetNumberFormatString(column));
        }
    }

    /// <summary>
    /// Translates a numeric cell.  'General' formatted cells give the raw number, date/time formats give an unambiguous
    /// date and/or time string and any other format gives the value as Excel would display it.
    /// </summary>
    private static object FormatNumber(double number, DateTime? date, int formatIndex, string formatString)
    {
        if (formatIndex == 0)
            return number;

        var format = BuiltinFormats.TryGetValue(formatIndex, out var builtin) ? builtin : formatString ?? "General";

        if (IsDateWithoutTime(format))
            return date?.ToString("yyyy-MM-dd");

        if (IsDateWithTime(format))
            return date?.ToString("yyyy-MM-dd HH:mm:ss");

        if (IsTimeWithoutDate(format))
            return date?.ToString("HH:mm:ss");

        object toFormat = IsDateFormat(format) ? date : number;
        return NumberFormats.GetOrAdd(format, static f => new NumberFormat(f))
            .Format(toFormat, CultureInfo.InvariantCulture);
    }

    private static bool IsDateWithTime(string formatString) => formatString.Contains('h') && formatString.Contains('y');

    private static bool IsDateWithoutTime(string formatString) =>
        formatString.Contains('y') && !formatString.Contains('h');

    private static bool IsTimeWithoutDate(string formatString) =>
        !formatString.Contains('y') && formatString.Contains('h');

    private static bool IsDateFormat(string formatString) => !string.IsNullOrWhiteSpace(formatString) &&
                                                             (formatString.Contains('/') ||
                                                              formatString.Contains('\\') ||
                                                              formatString.Contains(':'));

    /// <summary>
    /// Converts an Excel (1900 date system) serial number to a date, allowing for Excel treating 1900 as a leap year.
    /// Returns null for negative values which Excel cannot display as dates.
    /// </summary>
    private static DateTime? ToDateTime(double serial)
    {
        if (serial < 0 || serial >= 2958466) // 2958466 is 10000-01-01, beyond DateTime.MaxValue
            return null;

        return DateTime.FromOADate(serial < 60 ? serial + 1 : serial);
    }

    /// <summary>
    /// Inverse of the reader's date conversion: returns the Excel (1900 date system) serial number for a date.
    /// </summary>
    private static double ToExcelSerial(DateTime date)
    {
        var oa = date.ToOADate();
        return date < FirstUnambiguousExcelDate ? oa - 1 : oa;
    }
}
