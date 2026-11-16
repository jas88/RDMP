// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;
using Rdmp.Core.DataLoad.Modules.DataProvider.FlatFileManipulation;
using Rdmp.Core.DataLoad.Modules.Exceptions;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Rdmp.Core.Tests.DataLoad.Engine.Unit;

/// <summary>
/// Pins down how <see cref="ExcelDataFlowSource"/> and <see cref="ExcelToCSVFilesConverter"/> translate worksheet
/// layout and cell contents (number formats, formulas, blanks, offsets etc).  The expected values were captured from the
/// original NPOI based implementation so that changes of reading library do not silently change loaded data.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ExcelSourceCellHandlingTests
{
    private DirectoryInfo _dir;

    [SetUp]
    public void CreateDirectory()
    {
        _dir = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory,
            nameof(ExcelSourceCellHandlingTests), TestContext.CurrentContext.Test.Name.Replace('"', '_')));
        if (_dir.Exists)
            _dir.Delete(true);
        _dir.Create();
    }

    [TearDown]
    public void DeleteDirectory() => _dir.Delete(true);

    private FileInfo Save(MinimalXlsxBuilder builder, string name = "Book.xlsx") =>
        builder.Save(Path.Combine(_dir.FullName, name));

    private static DataTable Read(FileInfo file, IDataLoadEventListener listener = null, string sheet = null,
        int rowOffset = 0, int columnOffset = 0, string[] replacementHeaders = null)
    {
        listener ??= ThrowImmediatelyDataLoadEventListener.Quiet;
        var source = new ExcelDataFlowSource { WorkSheetName = sheet };
        source.PreInitialize(new FlatFileToLoad(file), listener);
        return source.GetChunk(listener, new GracefulCancellationToken(), rowOffset, columnOffset, replacementHeaders);
    }

    private static string[] Headers(DataTable dt) => dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

    private static object[] Row(DataTable dt, int row) =>
        dt.Rows[row].ItemArray.Select(v => v == DBNull.Value ? null : v).ToArray();

    private static string[] Messages(ToMemoryDataLoadEventListener listener, ProgressEventType type) =>
        listener.EventsReceivedBySender.SelectMany(kv => kv.Value)
            .Where(e => e.ProgressEventType == type).Select(e => e.Message).ToArray();

    [Test]
    public void SparseSheet_BlankRowsColumnsNullsAndHangingValues()
    {
        var file = Save(new MinimalXlsxBuilder().Sheet("Data")
            .Text("B3", "Name").Text("C3", "Age").Text("E3", "Notes")
            .Text("B4", "Frank").Number("C4", 30).Text("E4", "x").Text("F4", "hanging")
            .Blank("B5").Blank("C5")
            .Text("B6", "NULL").Text("C6", "  ").Text("E6", " null ")
            .Text("F7", "onlyhanging")
            .Text("B8", "Jo").Number("C8", 1.5));

        var listener = new ToMemoryDataLoadEventListener(false);
        var dt = Read(file, listener);

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "Name", "Age", "Notes" }));
            Assert.That(dt.Rows, Has.Count.EqualTo(2));
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { "Frank", "30", "x" }));
            Assert.That(Row(dt, 1), Is.EqualTo(new object[] { "Jo", "1.5", null }));
            Assert.That(dt.TableName, Is.EqualTo("Book"));
            Assert.That(Messages(listener, ProgressEventType.Warning), Is.EqualTo(new[]
            {
                "Discarded the following data (that was found in unnamed columns):hanging",
                "Discarded the following data (that was found in unnamed columns):onlyhanging"
            }));
            Assert.That(Messages(listener, ProgressEventType.Information),
                Does.Contain("Excel sheet Data contains 3"));
        });
    }

    [Test]
    public void NumberFormats_GeneralDatesTimesAndCustomFormats()
    {
        var file = Save(new MinimalXlsxBuilder()
            .Text("A1", "General").Text("B1", "Date").Text("C1", "DateTime").Text("D1", "Time").Text("E1", "TwoDp")
            .Text("F1", "MinSec").Text("G1", "DayMonth").Text("H1", "Pct").Text("I1", "Bool").Text("J1", "Err")
            .Number("A2", 3).Number("B2", 36892, "BuiltinDate").Number("C2", 36892.4375, "DateTime")
            .Number("D2", 0.4375, "Time").Number("E2", 0.1, "TwoDp").Number("F2", 0.00273148148, "MinSec")
            .Number("G2", 36892, "DayMonth").Number("H2", 0.5, "Percent").Bool("I2", true).Error("J2", "#DIV/0!")
            .Number("A3", 1234567.891).Number("B3", 1, "BuiltinDate").Number("C3", 61.5, "DateTime")
            .Number("D3", 1.25, "Time").Number("E3", 2, "TwoDp").Number("F3", 0.5, "MinSec")
            .Number("G3", 45000, "DayMonth").Number("H3", 1, "Percent").Bool("I3", false).Text("J3", "ok"));

        var dt = Read(file);

        Assert.Multiple(() =>
        {
            Assert.That(Row(dt, 0), Is.EqualTo(new object[]
                { "3", "2001-01-01", "2001-01-01 10:30:00", "10:30:00", "0.10", "03:56", "1-Jan", "50.00%", "True", null }));
            Assert.That(Row(dt, 1), Is.EqualTo(new object[]
            {
                "1234567.891", "1900-01-01", "1900-03-01 12:00:00", "06:00:00", "2.00", "00:00", "15-Mar", "100.00%",
                "False", "ok"
            }));
        });
    }

    [Test]
    public void NumberFormats_BuiltinFormats()
    {
        var ids = new[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 37, 38, 39, 40, 41, 42, 43, 44,
            45, 46, 47, 48, 49
        };
        var builder = new MinimalXlsxBuilder();
        for (var k = 0; k < ids.Length; k++)
        {
            var col = k < 26 ? ((char)('A' + k)).ToString() : $"A{(char)('A' + k - 26)}";
            builder.Text($"{col}1", $"f{ids[k]}")
                .NumberWithBuiltinFormat($"{col}2", 36892.2534, ids[k])
                .NumberWithBuiltinFormat($"{col}3", 1234.5678, ids[k]);
        }

        var dt = Read(Save(builder));

        Assert.Multiple(() =>
        {
            Assert.That(Row(dt, 0), Is.EqualTo(new object[]
            {
                "36892", "36892.25", "36,892", "36,892.25", "$36,892 ", "$36,892 ", "$36,892.25 ", "$36,892.25 ",
                "3689225%", "3689225.34%", "3.69E+04", "01/01/2001 06:04:53", "01/01/2001 06:04:53", "2001-01-01",
                "2001-01-01", "1-Jan", "2001-01-01", "06:04:53", "06:04:53", "06:04:53", "06:04:53",
                "2001-01-01 06:04:53", "36,892 ", "36,892 ", "36,892.25 ", "36,892.25 ", "  36,892 ", " $ 36,892 ",
                "  36,892.25 ", " $ 36,892.25 ", "04:53", "06:04:53", "04:53.8", "36.9E+3", "36892.2534"
            }));
            Assert.That(Row(dt, 1), Is.EqualTo(new object[]
            {
                "1235", "1234.57", "1,235", "1,234.57", "$1,235 ", "$1,235 ", "$1,234.57 ", "$1,234.57 ", "123457%",
                "123456.78%", "1.23E+03", "05/18/1903 13:37:37", "05/18/1903 13:37:37", "1903-05-18", "1903-05-18",
                "18-May", "1903-05-18", "13:37:37", "13:37:37", "13:37:37", "13:37:37", "1903-05-18 13:37:37",
                "1,235 ", "1,235 ", "1,234.57 ", "1,234.57 ", "  1,235 ", " $ 1,235 ", "  1,234.57 ", " $ 1,234.57 ",
                "37:37", "13:37:37", "37:37.9", "1.2E+3", "1234.5678"
            }));
        });
    }

    [Test]
    public void Formulas_UseCachedResults()
    {
        var file = Save(new MinimalXlsxBuilder()
            .Text("A1", "Num").Text("B1", "Str").Text("C1", "Err").Text("D1", "Date")
            .NumberFormula("A2", "1+1", 2).TextFormula("B2", "\"a\"&\"b\"", "ab").ErrorFormula("C2", "1/0", "#DIV/0!")
            .NumberFormula("D2", "DATE(2001,1,1)", 36892, "BuiltinDate")
            .NumberFormula("A3", "0.1*3", 0.30000000000000004).TextFormula("B3", "\"\"", "")
            .ErrorFormula("C3", "NA()", "#N/A").Text("D3", "z"));

        var dt = Read(file);

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "Num", "Str", "Err", "Date" }));
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { "2", "ab", null, "2001-01-01" }));
            Assert.That(Row(dt, 1), Is.EqualTo(new object[] { "0.30000000000000004", null, null, "z" }));
        });
    }

    [Test]
    public void NumericHeaders_UseRawNumber()
    {
        var file = Save(new MinimalXlsxBuilder()
            .Number("A1", 1).Number("B1", 2.5).Number("C1", 36892, "BuiltinDate")
            .Text("A2", "a").Text("B2", "b").Text("C2", "c"));

        Assert.That(Headers(Read(file)), Is.EqualTo(new[] { "1", "2.5", "36892" }));
    }

    [Test]
    public void ErrorOnlyRow_IsTreatedAsBlank()
    {
        var file = Save(new MinimalXlsxBuilder().Error("A1", "#REF!").Text("A2", "H").Text("A3", "v"));

        var dt = Read(file);

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "H" }));
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { "v" }));
        });
    }

    private static MinimalXlsxBuilder JunkAroundData() => new MinimalXlsxBuilder()
        .Text("B1", "junk").Text("C1", "junk").Text("D1", "junk")
        .Text("B2", "junk").Text("C2", "chi").Text("D2", "value")
        .Text("B3", "junk").Text("C3", "111").Text("D3", "v1")
        .Text("B4", "junk").Text("C4", "222").Text("D4", "v2");

    [Test]
    public void RowAndColumnOffsets_SkipJunk()
    {
        var listener = new ToMemoryDataLoadEventListener(false);
        var dt = Read(Save(JunkAroundData()), listener, rowOffset: 2, columnOffset: 2);

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "chi", "value" }));
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { "111", "v1" }));
            Assert.That(Row(dt, 1), Is.EqualTo(new object[] { "222", "v2" }));
            Assert.That(Messages(listener, ProgressEventType.Warning), Is.EqualTo(new[]
            {
                "Discarded the following data (that was found in unnamed columns):junk",
                "Discarded the following data (that was found in unnamed columns):junk"
            }));
        });
    }

    [Test]
    public void RowOffset_IsOneBasedRowOfHeaders()
    {
        var dt = Read(Save(JunkAroundData()), rowOffset: 3);

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "junk", "111", "v1" }));
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { "junk", "222", "v2" }));
        });
    }

    [Test]
    public void ReplacementHeaders_MatchingCount()
    {
        var file = Save(new MinimalXlsxBuilder().Text("A1", "a").Text("B1", "b").Text("A2", "1").Text("B2", "2"));
        var listener = new ToMemoryDataLoadEventListener(false);

        var dt = Read(file, listener, replacementHeaders: new[] { "x", "y" });

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "x", "y" }));
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { "1", "2" }));
            Assert.That(Messages(listener, ProgressEventType.Information).Last(),
                Is.EqualTo(
                    $"Force headers will make the following header changes:{Environment.NewLine}[0]a>>>x{Environment.NewLine}[1]b>>>y"));
        });
    }

    [Test]
    public void ReplacementHeaders_WrongCountIsReportedAndIgnored()
    {
        var file = Save(new MinimalXlsxBuilder().Text("A1", "a").Text("B1", "b").Text("A2", "1").Text("B2", "2"));
        var listener = new ToMemoryDataLoadEventListener(false);

        var dt = Read(file, listener, replacementHeaders: new[] { "x" });

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(Messages(listener, ProgressEventType.Error).Single(),
                Does.StartWith("ForceReplacementHeaders was set but it had 1 column header names while the file had 2"));
        });
    }

    [Test]
    public void ReplacementHeaders_CountIncludesCellsBeforeColumnOffset()
    {
        var file = Save(new MinimalXlsxBuilder()
            .Text("A1", "junk").Text("B1", "a").Text("C1", "b").Text("B2", "1").Text("C2", "2"));

        var dt = Read(file, columnOffset: 1, replacementHeaders: new[] { "x", "y", "z" });

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "y", "z" }));
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { "1", "2" }));
        });
    }

    private static MinimalXlsxBuilder TwoSheets() => new MinimalXlsxBuilder()
        .Sheet("First").Text("A1", "f").Text("A2", "1")
        .Sheet("Second").Text("A1", "s").Text("A2", "2");

    [Test]
    public void Worksheet_DefaultsToFirst()
    {
        Assert.That(Headers(Read(Save(TwoSheets()))), Is.EqualTo(new[] { "f" }));
    }

    [Test]
    public void Worksheet_SelectedByNameIgnoringCase()
    {
        var dt = Read(Save(TwoSheets()), sheet: "second");

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "s" }));
            Assert.That(Row(dt, 0), Is.EqualTo(new object[] { "2" }));
        });
    }

    [Test]
    public void Worksheet_MissingNameThrows()
    {
        var file = Save(TwoSheets());

        var ex = Assert.Throws<FlatFileLoadException>(() => Read(file, sheet: "Nope"));
        Assert.That(ex?.Message, Is.EqualTo("The Excel sheet 'Nope' was not found in workbook 'Book.xlsx'"));
    }

    [Test]
    public void HeaderOnly_GivesNoRows()
    {
        var dt = Read(Save(new MinimalXlsxBuilder().Text("A1", "h")));

        Assert.Multiple(() =>
        {
            Assert.That(Headers(dt), Is.EqualTo(new[] { "h" }));
            Assert.That(dt.Rows, Is.Empty);
        });
    }

    [Test]
    public void DuplicateHeaders_Throw()
    {
        var file = Save(new MinimalXlsxBuilder().Text("A1", "h").Text("B1", "h").Text("A2", "1"));

        Assert.Throws<DuplicateNameException>(() => Read(file));
    }

    [Test]
    public void ToCsvConverter_WritesMatchingWorksheets()
    {
        Save(new MinimalXlsxBuilder()
            .Sheet("Data1").Text("A1", "a").Text("B1", "b").Text("A2", "1").Number("B2", 36892, "BuiltinDate")
            .Sheet("Other").Text("A1", "o").Text("A2", "x")
            .Sheet("Data2").Text("A1", "c").Text("A2", "NULL").Text("A3", "y"), "Multi.xlsx");

        var converter = new ExcelToCSVFilesConverter
        {
            ExcelFilePattern = "Multi.xlsx",
            WorksheetPattern = new Regex(@"\d$"),
            PrefixWithWorkbookName = true
        };

        var loadDirectory = Substitute.For<ILoadDirectory>();
        loadDirectory.ForLoading.Returns(_dir);

        converter.Fetch(new ThrowImmediatelyDataLoadJob { LoadDirectory = loadDirectory },
            new GracefulCancellationToken());

        static string Contents(FileInfo f) =>
            File.ReadAllText(f.FullName).Replace("\r\n", "\n").Trim('\n');

        Assert.Multiple(() =>
        {
            Assert.That(_dir.GetFiles("*.csv").Select(f => f.Name).Order(),
                Is.EqualTo(new[] { "Multi_Data1.csv", "Multi_Data2.csv" }));
            Assert.That(Contents(new FileInfo(Path.Combine(_dir.FullName, "Multi_Data1.csv"))),
                Is.EqualTo("a,b\n1,2001-01-01"));
            Assert.That(Contents(new FileInfo(Path.Combine(_dir.FullName, "Multi_Data2.csv"))),
                Is.EqualTo("c\ny"));
        });
    }

    [Test]
    public void ToCsvConverter_ReadsLegacyXls()
    {
        UsefulStuff.SprayFile(typeof(ExcelTests).Assembly,
            $"{typeof(ExcelTests).Namespace}.TestFile.{ExcelTests.OddFormatsFile}", ExcelTests.OddFormatsFile,
            _dir.FullName);

        var converter = new ExcelToCSVFilesConverter
        {
            ExcelFilePattern = "*.xls",
            WorksheetPattern = new Regex("^MySheet$")
        };

        var loadDirectory = Substitute.For<ILoadDirectory>();
        loadDirectory.ForLoading.Returns(_dir);

        converter.Fetch(new ThrowImmediatelyDataLoadJob { LoadDirectory = loadDirectory },
            new GracefulCancellationToken());

        var lines = File.ReadAllLines(Path.Combine(_dir.FullName, "MySheet.csv"));
        Assert.Multiple(() =>
        {
            Assert.That(lines[0], Is.EqualTo("Name,Category,Age,Wage,Invisibre"));
            Assert.That(lines[1], Does.Match(@"^Frank,""Upper, Left"",30,\W11.00,0.1$"));
            Assert.That(lines[2], Is.EqualTo(@"Castello,""Lower, Back"",31,50.00%,0.2"));
        });
    }
}
