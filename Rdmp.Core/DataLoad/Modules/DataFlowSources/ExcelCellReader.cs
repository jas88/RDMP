// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using ExcelNumberFormat;

namespace Rdmp.Core.DataLoad.Modules.DataFlowSources;

/// <summary>
/// Translates ExcelDataReader cell values into the representation RDMP loads: strings for text, raw numbers for 'General'
/// (and fraction) formatted numeric cells, unambiguous date/time strings for date/time formats and the Excel rendering for
/// any other number format.  Formulas are never evaluated, the cached result stored in the file is used.
/// </summary>
public static partial class ExcelCellReader
{
    private const long MillisecondsPerDay = 86_400_000;

    private static readonly DateTime Epoch1900 = new(1900, 1, 1);
    private static readonly DateTime Epoch1904 = new(1904, 1, 1);

    /// <summary>
    /// The first day which Excel (1900 date system) and OLE Automation dates agree on.  Before this Excel believes that
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

    private static readonly ConcurrentDictionary<string, FormatKind> FormatKinds = new();

    private static readonly char[] DayMonthSecondTokens = { 'd', 'm', 's' };

    private enum FormatKind
    {
        /// <summary>Not a date/time format (e.g. "0.00", "0%")</summary>
        Number,

        /// <summary>A fraction e.g. "# ?/?", the value is loaded as the raw number</summary>
        Fraction,
        DateOnly,
        DateAndTime,
        TimeOnly,

        /// <summary>A time format which counts hours beyond 24 e.g. "[h]:mm:ss"</summary>
        ElapsedHours,

        /// <summary>A date/time format with neither a year nor an hour component e.g. "d-mmm", "mm:ss" or "[mm]:ss"</summary>
        OtherDateTime
    }

    /// <summary>
    /// Returns true if the cell has any content (i.e. is not empty, an error, or whitespace only text).
    /// </summary>
    public static bool HasContent(object rawValue) =>
        rawValue != null && !string.IsNullOrWhiteSpace(Convert.ToString(rawValue, CultureInfo.InvariantCulture));

    /// <summary>
    /// Returns the text of a header cell: strings as is, numbers (including date formatted numbers) as the raw number.
    /// </summary>
    public static string GetHeaderText(object rawValue, bool isDate1904) => rawValue switch
    {
        null => null,
        string s => s,
        DateTime dt => ToExcelSerial(dt, isDate1904).ToString(CultureInfo.CurrentCulture),
        bool b => b.ToString(),
        _ => Convert.ToDouble(rawValue, CultureInfo.InvariantCulture).ToString(CultureInfo.CurrentCulture)
    };

    /// <summary>
    /// Returns the C# value that best represents the contents of cell <paramref name="column"/> of the current row
    /// of <paramref name="reader"/> or null if it is blank, an error or the text "NULL".
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="column"></param>
    /// <param name="isDate1904">True if the workbook uses the 1904 date system</param>
    public static object GetCellValue(IExcelDataReader reader, int column, bool isDate1904)
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
                // ExcelDataReader has already applied the workbook's date system
                return FormatNumber(ToExcelSerial(dt, isDate1904), dt, reader, column, isDate1904);
            case TimeSpan ts:
                return FormatNumber(ts.TotalDays, ToDateTime(ts.TotalDays, isDate1904), reader, column, isDate1904);
            default:
                var number = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                return FormatNumber(number, ToDateTime(number, isDate1904), reader, column, isDate1904);
        }
    }

    /// <summary>
    /// Translates a numeric cell.  'General' and fraction formatted cells give the raw number, date/time formats give an
    /// unambiguous date and/or time string and any other format gives the value as Excel would display it.
    /// </summary>
    /// <param name="number">The cell's value (for dates and times this is the serial number)</param>
    /// <param name="date">The cell's value as a date or null if Excel could not display it as one (e.g. it is negative)</param>
    /// <param name="reader"></param>
    /// <param name="column"></param>
    /// <param name="isDate1904"></param>
    private static object FormatNumber(double number, DateTime? date, IExcelDataReader reader, int column,
        bool isDate1904)
    {
        var formatIndex = reader.GetNumberFormatIndex(column);
        if (formatIndex == 0)
            return number;

        var format = BuiltinFormats.TryGetValue(formatIndex, out var builtin)
            ? builtin
            : reader.GetNumberFormatString(column) ?? "General";

        switch (FormatKinds.GetOrAdd(format, Classify))
        {
            case FormatKind.Fraction:
                return number;
            case FormatKind.DateOnly:
                return date?.ToString("yyyy-MM-dd");
            case FormatKind.DateAndTime:
                return date?.ToString("yyyy-MM-dd HH:mm:ss");
            case FormatKind.TimeOnly:
                return date?.ToString("HH:mm:ss");
            case FormatKind.ElapsedHours:
                return date == null ? null : FormatElapsedHours(number);
            default:
                // Other date/time formats (e.g. "d-mmm", "[mm]:ss") are rendered from the serial number so that
                // ExcelNumberFormat applies the workbook's date system and elapsed time formats show the full duration
                return Render(format, number, isDate1904);
        }
    }

    private static string Render(string format, double value, bool isDate1904) =>
        NumberFormats.GetOrAdd(format, static f => new NumberFormat(f))
            .Format(value, CultureInfo.InvariantCulture, isDate1904);

    /// <summary>
    /// Returns the whole of a duration as hours, minutes and seconds.  The layout is that of a time of day ("HH:mm:ss",
    /// including the current culture's time separator) so a duration of less than 24 hours reads the same as a time of day.
    /// </summary>
    private static string FormatElapsedHours(double serial)
    {
        var (wholeDays, milliseconds) = SplitSerial(serial);
        var seconds = (wholeDays * MillisecondsPerDay + milliseconds) / 1000;
        var separator = CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator;

        return string.Create(CultureInfo.InvariantCulture,
            $"{seconds / 3600:00}{separator}{seconds / 60 % 60:00}{separator}{seconds % 60:00}");
    }

    /// <summary>
    /// Classifies an Excel number format.  Literal text (quoted strings, backslash escapes, padding/fill characters) and
    /// bracketed sections such as colours, conditions and locales are ignored so that only genuine date/time tokens
    /// (d, m, y, h, s and elapsed time e.g. [h]) count, in either case.  A year makes it a date, an hour makes it a time
    /// (or a date and time if there is also a year or a day) and a format containing '/' which has no date/time tokens is
    /// a fraction.
    /// </summary>
    private static FormatKind Classify(string format)
    {
        var tokens = StripLiterals(format, out var hasElapsedHours).ToLowerInvariant();

        var hasYear = tokens.Contains('y');
        var hasHour = tokens.Contains('h');

        if (hasYear)
            return hasHour ? FormatKind.DateAndTime : FormatKind.DateOnly;

        if (hasHour)
        {
            // a day of the month or week makes it a point in time even though no year is shown e.g. "m/d h:mm"
            if (tokens.Contains('d'))
                return FormatKind.DateAndTime;

            return hasElapsedHours ? FormatKind.ElapsedHours : FormatKind.TimeOnly;
        }

        if (tokens.IndexOfAny(DayMonthSecondTokens) >= 0)
            return FormatKind.OtherDateTime;

        return tokens.Contains('/') ? FormatKind.Fraction : FormatKind.Number;
    }

    /// <summary>
    /// Returns <paramref name="format"/> without its literal text and bracketed sections, leaving the format tokens.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="hasElapsedHours">True if the format counts hours beyond 24 i.e. has an [h] section</param>
    private static string StripLiterals(string format, out bool hasElapsedHours)
    {
        hasElapsedHours = false;

        // "General" is a keyword not a date/time pattern
        var withoutKeywords = GeneralKeyword().Replace(format, "");
        var sb = new StringBuilder(withoutKeywords.Length);

        for (var i = 0; i < withoutKeywords.Length; i++)
        {
            var c = withoutKeywords[i];
            switch (c)
            {
                case '"':
                    var close = withoutKeywords.IndexOf('"', i + 1);
                    i = close < 0 ? withoutKeywords.Length : close;
                    break;
                case '\\':
                case '_':
                case '*':
                    i++; // skip the escaped / padding / fill character
                    break;
                case '[':
                    var end = withoutKeywords.IndexOf(']', i + 1);
                    if (end < 0)
                    {
                        i = withoutKeywords.Length;
                        break;
                    }

                    var inner = withoutKeywords.Substring(i + 1, end - i - 1);
                    if (ElapsedTime().IsMatch(inner))
                    {
                        sb.Append(inner); // [h], [mm], [ss] elapsed time
                        hasElapsedHours |= inner[0] is 'h' or 'H';
                    }

                    i = end;
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    [GeneratedRegex("General", RegexOptions.IgnoreCase)]
    private static partial Regex GeneralKeyword();

    [GeneratedRegex("^(h+|m+|s+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ElapsedTime();

    /// <summary>
    /// Converts an Excel serial number to a date in the given date system, allowing for Excel treating 1900 as a leap
    /// year (as NPOI did).  Returns null for values which Excel cannot display as dates.
    /// </summary>
    private static DateTime? ToDateTime(double serial, bool isDate1904)
    {
        if (serial < 0 || serial >= 2_900_000) // comfortably within DateTime range for both systems
            return null;

        var (wholeDays, milliseconds) = SplitSerial(serial);

        if (isDate1904)
            return Epoch1904.AddDays(wholeDays).AddMilliseconds(milliseconds);

        var dayAdjust = wholeDays < 61 ? 0 : -1; // Excel thinks 1900-02-29 exists
        return Epoch1900.AddDays(wholeDays + dayAdjust - 1).AddMilliseconds(milliseconds);
    }

    /// <summary>
    /// Splits a serial number into whole days and the time of day rounded to the nearest millisecond (which can be a
    /// whole day when the time is within half a millisecond of midnight).
    /// </summary>
    private static (int WholeDays, int Milliseconds) SplitSerial(double serial)
    {
        var wholeDays = (int)Math.Floor(serial);
        return (wholeDays, (int)((serial - wholeDays) * MillisecondsPerDay + 0.5));
    }

    /// <summary>
    /// Inverse of the reader's date conversion: returns the Excel serial number for a date in the given date system.
    /// </summary>
    private static double ToExcelSerial(DateTime date, bool isDate1904)
    {
        if (isDate1904)
            return (date - Epoch1904).TotalDays;

        var oa = date.ToOADate();
        return date < FirstUnambiguousExcelDate ? oa - 1 : oa;
    }
}
