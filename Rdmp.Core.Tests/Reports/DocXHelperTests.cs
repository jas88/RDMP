// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using NUnit.Framework;
using Rdmp.Core.Reports;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;

namespace Rdmp.Core.Tests.Reports;

/// <summary>
/// Exercises every <see cref="DocXHelper"/> building block and reads the resulting .docx back with OpenXml
/// </summary>
internal class DocXHelperTests : DocXHelper
{
    private string _path;

    [SetUp]
    public void SetUp() =>
        _path = Path.Join(TestContext.CurrentContext.WorkDirectory, $"DocXHelperTests_{Guid.NewGuid():N}.docx");

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Test]
    public void AllHelpers_ProduceValidDocumentWithExpectedContent()
    {
        using (var document = GetNewDocFile(new FileInfo(_path)))
        {
            InsertHeader(document, "Main Heading");
            InsertHeader(document, "Sub Heading", 2);
            InsertParagraph(document, $"First line{Environment.NewLine}Second line", 7);
            InsertParagraph(document, "   "); // ignored

            var table = InsertTable(document, 2, 3);
            SetTableCell(table, 0, 0, "Col A", 8);
            SetTableCell(table, 0, 1, "Col B");
            SetTableCell(table, 1, 2, $"multi{Environment.NewLine}line");
            SetTableCell(table, 1, 0, null);

            InsertSectionPageBreak(document);

            using var img = new Image<Rgba32>(40, 20, Color.Red.ToPixel<Rgba32>());
            GetPicture(document, img);

            var cellRun = GetTableCell(table, 1, 1).Elements<Paragraph>().First().AppendChild(new Run());
            GetPicture(document, cellRun, img);

            SetLandscape(document);
            SetMargins(document, 20);
            AddFooter(document, "My Footer", 6);
        }

        var contents = DocXContents.Read(_path);

        Assert.Multiple(() =>
        {
            contents.AssertValid();

            Assert.That(contents.Paragraphs.Take(4),
                Is.EqualTo(new[] { "Main Heading", "Sub Heading", "First line", "Second line" }));
            Assert.That(contents.FontSizeOf("Main Heading"), Is.EqualTo(16));
            Assert.That(contents.FontSizeOf("Sub Heading"), Is.EqualTo(13));
            Assert.That(contents.FontSizeOf("First line"), Is.EqualTo(7));
            Assert.That(contents.FontSizeOf("Col A"), Is.EqualTo(8));
            Assert.That(contents.FontSizeOf("Col B"), Is.Null, "No size requested so none should be set");

            Assert.That(contents.Tables, Has.Count.EqualTo(1));
            Assert.That(contents.Tables[0], Has.Length.EqualTo(2));
            Assert.That(contents.Tables[0][0], Is.EqualTo(new[] { "Col A", "Col B", "" }));
            Assert.That(contents.Tables[0][1], Is.EqualTo(new[] { "", "", "multiline" }));

            Assert.That(contents.PageBreakCount, Is.EqualTo(1));
            Assert.That(contents.ImageCount, Is.EqualTo(2));
            Assert.That(contents.DrawingCount, Is.EqualTo(2));
            Assert.That(contents.FooterTexts, Is.EqualTo(new[] { "My Footer" }));

            Assert.That(contents.PageSize.Orient?.Value, Is.EqualTo(PageOrientationValues.Landscape));
            Assert.That(contents.PageSize.Width?.Value, Is.EqualTo(16840u));
            Assert.That(contents.PageSize.Height?.Value, Is.EqualTo(11900u));
            Assert.That(contents.PageMargin.Left?.Value, Is.EqualTo(292u));
            Assert.That(contents.PageMargin.Right?.Value, Is.EqualTo(292u));
            Assert.That(contents.PageMargin.Top?.Value, Is.EqualTo(1440));
        });
    }

    [Test]
    public void Table_HasFixedLayoutBordersAndEqualColumnWidths()
    {
        using (var document = GetNewDocFile(new FileInfo(_path)))
            InsertTable(document, 1, 4);

        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(_path, false);
        var table = doc.MainDocumentPart!.Document.Body!.Elements<Table>().Single();
        var props = table.GetFirstChild<TableProperties>()!;

        Assert.Multiple(() =>
        {
            Assert.That(props.TableLayout?.Type?.Value, Is.EqualTo(TableLayoutValues.Fixed));
            Assert.That(props.TableBorders?.InsideVerticalBorder?.Val?.Value, Is.EqualTo(BorderValues.Single));
            Assert.That(table.GetFirstChild<TableGrid>()!.Elements<GridColumn>().Select(c => c.Width?.Value),
                Is.EqualTo(Enumerable.Repeat("2500", 4)));
        });
    }

    [Test]
    public void Dispose_SavesCompleteFileAndIsIdempotent()
    {
        using var document = GetNewDocFile(new FileInfo(_path));
        InsertParagraph(document, "content");
        AddFooter(document, "footer", -1);

        // explicit dispose (as done before showing the file to the user) must leave a complete, closed file
        document.Dispose();

        var contents = DocXContents.Read(_path);
        Assert.Multiple(() =>
        {
            contents.AssertValid();
            Assert.That(contents.Paragraphs, Is.EqualTo(new[] { "content" }));
            Assert.That(contents.FooterTexts, Is.EqualTo(new[] { "footer" }));
            Assert.That(() => document.Append(new Paragraph()), Throws.TypeOf<ObjectDisposedException>());
            Assert.That(document.Dispose, Throws.Nothing);
        });
    }

    [Test]
    public void Dispose_WhenSavingFails_StillClosesTheFile()
    {
        var document = GetNewDocFile(new FileInfo(_path));

        // 0x01 cannot be written to XML.  The helpers remove such characters, so add it directly to make saving fail
        document.Append(new Paragraph(new Run(new Text("\u0001"))));

        Assert.That(document.Dispose, Throws.ArgumentException);

        Assert.Multiple(() =>
        {
            // nothing may still have the file open
            Assert.That(() =>
            {
                using var exclusive = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }, Throws.Nothing);

            Assert.That(document.Dispose, Throws.Nothing);
        });
    }

    [Test]
    public void FontSizeOf_FailsWhenRunsWithTheSameTextDisagree()
    {
        using (var document = GetNewDocFile(new FileInfo(_path)))
        {
            InsertHeader(document, "Same");
            InsertParagraph(document, "Same", 7);
            InsertParagraph(document, "Agreed", 9);
            InsertParagraph(document, "Agreed", 9);

            var table = InsertTable(document, 1, 2);
            SetTableCell(table, 0, 0, "Mixed", 8);
            SetTableCell(table, 0, 1, "Mixed");
        }

        var contents = DocXContents.Read(_path);

        Assert.Multiple(() =>
        {
            Assert.That(contents.FontSizeOf("Agreed"), Is.EqualTo(9));
            Assert.That(() => contents.FontSizeOf("Same"),
                Throws.TypeOf<AssertionException>().With.Message.Contains("conflicting font sizes: 7, 16"));
            Assert.That(() => contents.FontSizeOf("Mixed"),
                Throws.TypeOf<AssertionException>().With.Message.Contains("conflicting font sizes: none, 8"));
            Assert.That(() => contents.FontSizeOf("Missing"),
                Throws.TypeOf<AssertionException>().With.Message.Contains("no run with the text"));
        });
    }

    [Test]
    public void Text_CharactersXmlCannotHold_AreRemovedSoTheDocumentSaves()
    {
        using (var document = GetNewDocFile(new FileInfo(_path)))
        {
            InsertHeader(document, "Head\u0001ing");
            InsertParagraph(document, "manual\vbreak, form\ffeed, tab\tkept, lone \ud83d surrogate, pair \ud83d\ude00 kept");

            var table = InsertTable(document, 1, 1);
            SetTableCell(table, 0, 0, "ce\u001bll\uffff");

            AddFooter(document, "foot\u0000er", -1);
        }

        var contents = DocXContents.Read(_path);

        Assert.Multiple(() =>
        {
            contents.AssertValid();
            Assert.That(contents.Paragraphs, Is.EqualTo(new[]
            {
                "Heading",
                "manual break, form feed, tab\tkept, lone  surrogate, pair \ud83d\ude00 kept"
            }));
            Assert.That(contents.Tables.Single()[0], Is.EqualTo(new[] { "cell" }));
            Assert.That(contents.FooterTexts, Is.EqualTo(new[] { "footer" }));
        });
    }

    [Test]
    public void RemoveInvalidXmlCharacters_KeepsEverythingXmlCanHold()
    {
        // not [TestCase]s: attribute arguments cannot hold unpaired surrogates and test names should stay printable
        var cases = new (string Text, string Expected)[]
        {
            ("plain text", "plain text"),
            ("", ""),
            ("\u0000", ""),
            ("a\u000bb\u000cc\u001fd", "a b cd"),
            ("tab\t cr\r lf\n kept", "tab\t cr\r lf\n kept"),
            ("\ud83d\ude00 ok \ud83d\ude00", "\ud83d\ude00 ok \ud83d\ude00"),
            ("\ude00 low first \ud83d", " low first "),
            ("\ud83d\ud83d\ude00", "\ud83d\ude00"),
            ("ends with bad\ufffe", "ends with bad")
        };

        Assert.Multiple(() =>
        {
            foreach (var (text, expected) in cases)
                Assert.That(RemoveInvalidXmlCharacters(text), Is.EqualTo(expected));

            // unchanged text is returned as is rather than copied
            Assert.That(RemoveInvalidXmlCharacters(cases[0].Text), Is.SameAs(cases[0].Text));
        });
    }

    [Test]
    public void Text_LineEndingsInsideARun_BecomeLineBreaks()
    {
        using (var document = GetNewDocFile(new FileInfo(_path)))
        {
            // InsertHeader does not split its text into paragraphs, so every kind of line ending reaches the run
            InsertHeader(document, "one\ntwo\r\nthree\rfour");

            // each line ending is one break: blank lines, leading and trailing breaks are kept
            InsertHeader(document, "\nfirst\n\nsecond\r\n", 2);

            InsertHeader(document, "", 3);
        }

        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(_path, false);
        var runs = doc.MainDocumentPart!.Document.Body!.Descendants<Run>().ToArray();

        // the first child of each run is its properties (the font size)
        string[] ContentOf(Run run) => run.ChildElements.Skip(1)
            .Select(e => e is Text t ? t.Text : $"<{e.LocalName}>").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(runs, Has.Length.EqualTo(3));

            Assert.That(ContentOf(runs[0]),
                Is.EqualTo(new[] { "one", "<br>", "two", "<br>", "three", "<br>", "four" }));
            Assert.That(ContentOf(runs[1]), Is.EqualTo(new[] { "<br>", "first", "<br>", "<br>", "second", "<br>" }));
            Assert.That(ContentOf(runs[2]), Is.Empty);
        });
    }

    [Test]
    public void ParagraphsAndCells_SplitOnEveryKindOfLineEnding()
    {
        // every kind of line ending, with blank lines, whatever Environment.NewLine is on this machine
        const string text = "first\r\nsecond\nthird\rfourth\r\n\r\n\nfifth\n";

        using (var document = GetNewDocFile(new FileInfo(_path)))
        {
            InsertParagraph(document, text);

            var table = InsertTable(document, 1, 1);
            SetTableCell(table, 0, 0, text);
        }

        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(_path, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var expected = new[] { "first", "second", "third", "fourth", "fifth" };

        Assert.Multiple(() =>
        {
            // one paragraph per line with nothing left over from the line endings (no stray carriage returns or spaces)
            Assert.That(body.Elements<Paragraph>().Select(p => p.InnerText), Is.EqualTo(expected));
            Assert.That(body.Elements<Paragraph>().SelectMany(p => p.Descendants<Break>()), Is.Empty);

            // in a cell each line is a run ending in a line break
            var cellRuns = body.Descendants<TableCell>().Single().Descendants<Run>().ToArray();
            Assert.That(cellRuns.Select(r => r.InnerText), Is.EqualTo(expected));
            Assert.That(cellRuns.Select(r => r.Elements<Break>().Count()), Is.All.EqualTo(1));
        });
    }

    [Test]
    public void Document_CanBeWrittenWithNonDocxExtension()
    {
        var docPath = Path.ChangeExtension(_path, ".doc");
        try
        {
            using (var document = GetNewDocFile(new FileInfo(docPath)))
                InsertParagraph(document, "hello");

            Assert.That(DocXContents.Read(docPath).Paragraphs, Is.EqualTo(new[] { "hello" }));
        }
        finally
        {
            File.Delete(docPath);
        }
    }

    [Test]
    public void Picture_IsSizedFromPixels()
    {
        using (var document = GetNewDocFile(new FileInfo(_path)))
        {
            using var img = new Image<Rgba32>(100, 50);
            GetPicture(document, img);
        }

        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(_path, false);
        var extent = doc.MainDocumentPart!.Document.Body!
            .Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>().Single();

        Assert.Multiple(() =>
        {
            // 9525 EMU per pixel at 96 DPI, matching the previous NPOI output (0.75pt per pixel)
            Assert.That(extent.Cx?.Value, Is.EqualTo(100 * 9525L));
            Assert.That(extent.Cy?.Value, Is.EqualTo(50 * 9525L));
        });
    }
}
