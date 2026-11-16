// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
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
/// (e.g. fractions) in <see cref="ExcelDataFlowSource"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ExcelDateSystemAndFormatTests
{
    public const string Date1904XlsFile = "Date1904.xls";

    private DirectoryInfo _dir;

    [SetUp]
    public void CreateDirectory()
    {
        _dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory,
            nameof(ExcelDateSystemAndFormatTests), TestContext.CurrentContext.Test.Name.Replace('"', '_')));
        if (_dir.Exists)
            _dir.Delete(true);
        _dir.Create();
    }

    [TearDown]
    public void DeleteDirectory() => _dir.Delete(true);

    private FileInfo Save(MinimalXlsxBuilder builder, string name = "Book.xlsx") =>
        builder.Save(Path.Combine(_dir.FullName, name));

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
    [TestCase("[h]:mm:ss", 0.5, "12:00:00")]
    public void NumberFormats_FractionsAreNotDates(string format, double value, string expected)
    {
        var dt = Read(Save(new MinimalXlsxBuilder().Text("A1", "Value").NumberWithCustomFormat("A2", value, format)));

        Assert.That(dt.Rows[0][0], Is.EqualTo(expected));
    }
}
