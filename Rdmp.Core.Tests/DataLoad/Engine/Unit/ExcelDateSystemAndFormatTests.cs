// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Rdmp.Core.Tests.DataLoad.Engine.Unit;

/// <summary>
/// Tests for workbook date system (1900 vs 1904) handling and for distinguishing date formats from other number formats
/// (e.g. fractions) in <see cref="ExcelDataFlowSource"/>.  Plain numbers are formatted with the current culture so the
/// fixture is pinned to one.
/// </summary>
[TestFixture]
[Category("Unit")]
[SetCulture("en-GB")]
public class ExcelDateSystemAndFormatTests
{
    public const string Date1904XlsFile = "Date1904.xls";

    private DirectoryInfo _dir;

    [SetUp]
    public void CreateDirectory()
    {
        // The test ID is unique to each test case and, unlike the test name (which includes the test case arguments
        // e.g. number formats such as "# ?/?"), never contains directory separators or characters that are not allowed
        // in file names
        _dir = new DirectoryInfo(Path.Join(TestContext.CurrentContext.TestDirectory,
            nameof(ExcelDateSystemAndFormatTests), TestContext.CurrentContext.Test.ID));
        if (_dir.Exists)
            _dir.Delete(true);
        _dir.Create();
    }

    [TearDown]
    public void DeleteDirectory() => _dir.Delete(true);

    private FileInfo Save(MinimalXlsxBuilder builder, string name = "Book.xlsx") =>
        builder.Save(Path.Join(_dir.FullName, name));

    private FileInfo Spray(string resourceFile) =>
        UsefulStuff.SprayFile(typeof(ExcelTests).Assembly, $"{typeof(ExcelTests).Namespace}.TestFile.{resourceFile}",
            resourceFile, _dir.FullName);

    private static DataTable Read(FileInfo file)
    {
        var listener = ThrowImmediatelyDataLoadEventListener.Quiet;
        var source = new ExcelDataFlowSource();
        source.PreInitialize(new FlatFileToLoad(file), listener);
        return source.GetChunk(listener, new GracefulCancellationToken());
    }

    private static object[] Row(DataTable dt, int row) =>
        dt.Rows[row].ItemArray.Select(v => v == DBNull.Value ? null : v).ToArray();

    /// <summary>
    /// A built-in date (converted by ExcelDataReader), "General yyyy" (a date format ExcelDataReader does not recognise
    /// so RDMP converts the serial number itself), "d-mmm" (rendered by ExcelNumberFormat) and a date time.
    /// </summary>
    private static MinimalXlsxBuilder DateBook(bool date1904)
    {
        var builder = new MinimalXlsxBuilder();
        if (date1904)
            builder.Date1904();

        return builder
            .Text("A1", "Builtin").Text("B1", "Unusual").Text("C1", "DayMonth").Text("D1", "DateTime")
            .Number("E1", 1000, "BuiltinDate")
            .Number("A2", 1000, "BuiltinDate").NumberWithCustomFormat("B2", 1000.25, "General yyyy")
            .Number("C2", 1000, "DayMonth").Number("D2", 1000.5, "DateTime").Text("E2", "x");
    }

    [TestCase(false, "1902-09-26", "1902-09-26", "26-Sep", "1902-09-26 12:00:00")]
    [TestCase(true, "1906-09-27", "1906-09-27", "27-Sep", "1906-09-27 12:00:00")]
    public void Xlsx_DateSystemIsApplied(bool date1904, string builtin, string unusual, string dayMonth,
        string dateTime)
    {
        var dt = Read(Save(DateBook(date1904)));

        Assert.Multiple(() =>
        {
            Assert.That(dt.Columns[4].ColumnName, Is.EqualTo("1000"), "Date formatted header should be the raw serial");
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { builtin, unusual, dayMonth, dateTime, "x" }));
        });
    }

    /// <summary>
    /// Date1904.xls is a 1904 date system workbook (DATEMODE = 1) produced by converting a <see cref="MinimalXlsxBuilder"/>
    /// workbook with Libre Office.  Its "Custom" column format was then patched in place to "@ yyyy " (a date format which
    /// ExcelDataReader does not recognise, so RDMP converts the serial number itself) and "Era" uses the "e" (year) format.
    /// </summary>
    [Test]
    public void Xls_DateSystemIsApplied()
    {
        var dt = Read(Spray(Date1904XlsFile));

        Assert.Multiple(() =>
        {
            Assert.That(dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName),
                Is.EqualTo(new[] { "Date", "Custom", "DayMonth", "Era", "Fraction", "Num" }));
            Assert.That(Row(dt, 0),
                Is.EqualTo(new object[] { "1906-09-27", "1906-09-27", "27-Sep", "1906", "1.5", "3" }));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void IsDate1904_Xlsx(bool date1904)
    {
        using var stream = File.OpenRead(Save(DateBook(date1904)).FullName);
        Assert.That(ExcelDateSystem.IsDate1904(stream), Is.EqualTo(date1904));
    }

    [TestCase(Date1904XlsFile, true)]
    [TestCase(ExcelTests.OddFormatsFile, false)]
    [TestCase(ExcelTests.TestFile, false)]
    public void IsDate1904_Fixtures(string fixture, bool expected)
    {
        using var stream = File.OpenRead(Spray(fixture).FullName);
        Assert.Multiple(() =>
        {
            Assert.That(ExcelDateSystem.IsDate1904(stream), Is.EqualTo(expected));
            Assert.That(stream.Position, Is.Zero, "Stream should be left at its original position");
        });
    }

    [Test]
    public void IsDate1904_UnrecognisedContentIsFalse()
    {
        using var stream = new MemoryStream("not a workbook at all"u8.ToArray());
        Assert.That(ExcelDateSystem.IsDate1904(stream), Is.False);
    }

    [TestCase("# ?/?", 1.5, "1.5")]
    [TestCase("# ??/??", 0.25, "0.25")]
    [TestCase("?/?", 0.75, "0.75")]
    [TestCase("# ?/8", 2.125, "2.125")]
    [TestCase("0 0/100", 3.07, "3.07")]
    [TestCase("dd/mm/yyyy", 1000, "1902-09-26")]
    [TestCase("mm/dd/yy", 1000, "1902-09-26")]
    [TestCase("m/d/yyyy h:mm", 1000.5, "1902-09-26 12:00:00")]
    [TestCase("d/m", 1000, "26/9")]
    [TestCase("dd\"/\"mm", 1000, "26/09")]
    [TestCase("0.00\"/kg\"", 1000.25, "1000.25/kg")]
    [TestCase("0.0 \"hrs\"", 1000.25, "1000.3 hrs")]
    [TestCase("[Red]0.00", 1000.25, "1000.25")]
    [TestCase("[$-F400]h:mm:ss\\ AM/PM", 0.5, "12:00:00")]
    // times of day wrap at 24 hours
    [TestCase("h:mm", 1000.25, "06:00:00")]
    [TestCase("h:mm:ss", 2.0625, "01:30:00")]
    // elapsed hours do not: 2.0625 days is 49.5 hours
    [TestCase("[h]:mm:ss", 0, "00:00:00")]
    [TestCase("[h]:mm:ss", 0.5, "12:00:00")]
    [TestCase("[h]:mm:ss", 0.99999, "23:59:59")]
    [TestCase("[h]:mm:ss", 1, "24:00:00")]
    [TestCase("[h]:mm:ss", 1.00001, "24:00:00")]
    [TestCase("[h]:mm:ss", 2.0625, "49:30:00")]
    [TestCase("[h]:mm:ss", 1000.25, "24006:00:00")]
    [TestCase("[hh]:mm", 2.0625, "49:30:00")]
    [TestCase("[h]", 2.0625, "49:30:00")]
    [TestCase("[h]:mm:ss.00", 2.0625, "49:30:00")]
    [TestCase("[Red][h]:mm", 2.0625, "49:30:00")]
    [TestCase("[h]:mm \"d\"", 2.0625, "49:30:00")]
    // half a millisecond short of 24 hours rounds up to it (as a time of day this value is 00:00:00)
    [TestCase("[h]:mm:ss", 0.999999999, "24:00:00")]
    [TestCase("h:mm:ss", 0.999999999, "00:00:00")]
    // elapsed minutes and seconds are rendered as Excel displays them
    [TestCase("[mm]:ss", 0.5, "720:00")]
    [TestCase("[mm]:ss", 1000.25, "1440360:00")]
    [TestCase("[m]", 2.5, "3600")]
    [TestCase("[ss]", 0.5, "43200")]
    [TestCase("[s].00", 0.5, "43200.00")]
    // a day and an hour but no year is still a date and time
    [TestCase("m/d h:mm", 1000.5, "1902-09-26 12:00:00")]
    [TestCase("dd-mmm hh:mm", 1000.5, "1902-09-26 12:00:00")]
    [TestCase("ddd h:mm", 1000.5, "1902-09-26 12:00:00")]
    [TestCase("[$-409]d-mmm h:mm AM/PM", 1000.5, "1902-09-26 12:00:00")]
    [TestCase("d [h]:mm", 1000.5, "1902-09-26 12:00:00")]
    // without an hour a year-less date is rendered as Excel displays it, without a day an hour is only a time
    [TestCase("d-mmm", 1000.5, "26-Sep")]
    [TestCase("mmm h:mm", 1000.5, "12:00:00")]
    // quoted and escaped letters are text not date/time tokens
    [TestCase("0 \"d h\"", 1000.25, "1000 d h")]
    [TestCase("0\\d\\h", 1000.25, "1000dh")]
    [TestCase("\"[h]\" 0", 1000.25, "[h] 1000")]
    [TestCase("h:mm \"d\"", 1000.25, "06:00:00")]
    [TestCase("h:mm \\d", 1000.25, "06:00:00")]
    [TestCase("\"h\" d-mmm", 1000.25, "h 26-Sep")]
    public void NumberFormats_RenderAsExpected(string format, double value, string expected)
    {
        var dt = Read(Save(new MinimalXlsxBuilder().Text("A1", "Value").NumberWithCustomFormat("A2", value, format)));

        Assert.That(dt.Rows[0][0], Is.EqualTo(expected));
    }

    /// <summary>
    /// Built-in format 46 is "[h]:mm:ss", 20 and 21 are the times of day "h:mm" and "h:mm:ss" and 45 is "mm:ss"
    /// </summary>
    [TestCase(46, 0.99999, "23:59:59")]
    [TestCase(46, 1.00001, "24:00:00")]
    [TestCase(46, 1.5, "36:00:00")]
    [TestCase(20, 1.5, "12:00:00")]
    [TestCase(21, 1.5, "12:00:00")]
    [TestCase(45, 1.5, "00:00")]
    public void NumberFormats_BuiltinTimesAndElapsedHours(int builtinFormat, double value, string expected)
    {
        var dt = Read(Save(new MinimalXlsxBuilder().Text("A1", "Value")
            .NumberWithBuiltinFormat("A2", value, builtinFormat)));

        Assert.That(dt.Rows[0][0], Is.EqualTo(expected));
    }

    [TestCase("[h]:mm:ss")]
    [TestCase("[hh]:mm")]
    public void NumberFormats_NegativeElapsedHoursAreNull(string format)
    {
        var dt = Read(Save(new MinimalXlsxBuilder().Text("A1", "Elapsed").Text("B1", "Other")
            .NumberWithCustomFormat("A2", -0.5, format).Text("B2", "x")));

        Assert.That(Row(dt, 0), Is.EqualTo(new object[] { null, "x" }));
    }

    /// <summary>
    /// Elapsed hours are laid out like a time of day (which uses the current culture's time separator, '.' in Finnish) so
    /// that durations under 24 hours read exactly as they did when they were loaded as a time of day
    /// </summary>
    [Test]
    [SetCulture("fi-FI")]
    public void NumberFormats_ElapsedHoursAreLaidOutLikeTimesOfDay()
    {
        var dt = Read(Save(new MinimalXlsxBuilder().Text("A1", "Elapsed").Text("B1", "TimeOfDay")
            .NumberWithCustomFormat("A2", 0.75, "[h]:mm:ss").NumberWithCustomFormat("B2", 0.75, "hh:mm:ss")
            .NumberWithCustomFormat("A3", 2.0625, "[h]:mm:ss").NumberWithCustomFormat("B3", 2.0625, "hh:mm:ss")));

        var separator = CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator;
        var sixPm = new DateTime(2000, 1, 1, 18, 0, 0).ToString("HH:mm:ss");

        Assert.Multiple(() =>
        {
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { sixPm, sixPm }));
            Assert.That(Row(dt, 1), Is.EqualTo(new object[] { $"49{separator}30{separator}00", $"01{separator}30{separator}00" }));
        });
    }
}
