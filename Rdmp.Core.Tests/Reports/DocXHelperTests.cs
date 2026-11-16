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
        _path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"DocXHelperTests_{Guid.NewGuid():N}.docx");

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
            Assert.That(contents.FontSizes["Main Heading"], Is.EqualTo(16));
            Assert.That(contents.FontSizes["Sub Heading"], Is.EqualTo(13));
            Assert.That(contents.FontSizes["First line"], Is.EqualTo(7));
            Assert.That(contents.FontSizes["Col A"], Is.EqualTo(8));
            Assert.That(contents.FontSizes.ContainsKey("Col B"), Is.False, "No size requested so none should be set");

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
