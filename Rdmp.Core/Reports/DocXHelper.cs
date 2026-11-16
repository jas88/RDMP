// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Rdmp.Core.ReusableLibraryCode;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Rdmp.Core.Reports;

/// <summary>
/// Base class for all reports which generate Microsoft DocX files.  The DocumentFormat.OpenXml library is used to create the .docx file so it doesn't actually
/// require Microsoft Office to be installed on the machine using the class but in order to open the resulting files the user will need something compatible with .docx.
/// 
/// <para>Also contains all the helper methods for simplifying the OpenXml API for adding paragraphs/pictures/tables.</para>
/// </summary>
public class DocXHelper
{
    private const int H1Size = 16;
    private const int H2Size = 13;
    private const int H3Size = 12;
    private const int H4Size = 11;

    private const int DefaultFontSize = 10;

    /// <summary>
    /// English Metric Units per pixel (at 96 DPI) used when sizing embedded pictures
    /// </summary>
    private const long EmuPerPixel = 9525;

    /// <summary>
    /// Total width (in twentieths of a point) shared out between the columns of tables created by <see cref="InsertTable"/>
    /// </summary>
    private const int TableWidth = 10000;

    protected static void InsertParagraph(DocXDocument document, string ptext, int textFontSize = -1)
    {
        if (string.IsNullOrWhiteSpace(ptext)) return;

        foreach (var para in ptext.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            document.Append(new Paragraph(CreateTextRun(para, textFontSize != -1 ? textFontSize : DefaultFontSize)));
    }

    protected static void InsertHeader(DocXDocument document, string htext, int headSize = 1)
    {
        document.Append(new Paragraph(CreateTextRun(htext ?? "", GetSize(headSize))));
    }

    private static int GetSize(int headSize) =>
        headSize switch
        {
            1 => H1Size,
            2 => H2Size,
            3 => H3Size,
            4 => H4Size,
            _ => throw new ArgumentOutOfRangeException(nameof(headSize))
        };

    /// <summary>
    /// Creates a run containing <paramref name="text"/> (whitespace preserved) in the given point size (-1 for unspecified).
    /// As when these documents were written with NPOI, every line feed in <paramref name="text"/> becomes a line break
    /// (so blank lines and leading or trailing breaks are kept) and a carriage return on its own is white space.
    /// Characters which cannot be stored in XML are dealt with by <see cref="RemoveInvalidXmlCharacters"/>.
    /// </summary>
    private static Run CreateTextRun(string text, int fontSize)
    {
        var run = new Run();

        if (fontSize != -1)
            run.AppendChild(new RunProperties(new FontSize { Val = (fontSize * 2).ToString() }));

        var lines = RemoveInvalidXmlCharacters(text ?? "").Replace("\r\n", "\n").Replace('\r', ' ').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                run.AppendChild(new Break());

            if (lines[i].Length > 0)
                run.AppendChild(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
        }

        return run;
    }

    /// <summary>
    /// Returns <paramref name="text"/> without the characters that XML cannot hold (a document containing any of them
    /// cannot be saved).  Whitespace control characters, such as the vertical tab Microsoft Word uses for a manual
    /// line break, become a space so that the words either side stay apart.  Other control characters and unpaired
    /// surrogates are dropped.
    /// </summary>
    protected static string RemoveInvalidXmlCharacters(string text)
    {
        StringBuilder sb = null;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (XmlConvert.IsXmlChar(c))
            {
                sb?.Append(c);
                continue;
            }

            if (i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], c))
            {
                sb?.Append(c).Append(text[i + 1]);
                i++;
                continue;
            }

            // first character to be changed: start from everything accepted so far
            sb ??= new StringBuilder(text, 0, i, text.Length);

            if (char.IsWhiteSpace(c))
                sb.Append(' ');
        }

        return sb?.ToString() ?? text;
    }

    /// <summary>
    /// Returns the cell at the given zero based <paramref name="row"/> and <paramref name="col"/> of <paramref name="table"/>
    /// </summary>
    protected static TableCell GetTableCell(Table table, int row, int col) =>
        table.Elements<TableRow>().ElementAt(row).Elements<TableCell>().ElementAt(col);

    protected static void SetTableCell(Table table, int row, int col, string value, int fontSize = -1)
    {
        if (string.IsNullOrEmpty(value)) return;

        // each line goes in the cell's first paragraph, followed by a line break
        var para = GetTableCell(table, row, col).Elements<Paragraph>().First();

        foreach (var run in value.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                     .Select(bit => CreateTextRun(bit, fontSize)))
        {
            run.AppendChild(new Break());
            para.AppendChild(run);
        }
    }

    /// <summary>
    /// Adds a new paragraph to the end of <paramref name="document"/> containing <paramref name="bmp"/>
    /// </summary>
    protected static Drawing GetPicture(DocXDocument document, Image bmp)
    {
        var run = document.Append(new Paragraph()).AppendChild(new Run());
        return GetPicture(document, run, bmp);
    }

    /// <summary>
    /// Embeds <paramref name="bmp"/> (as PNG) in <paramref name="document"/> and adds it inline to <paramref name="run"/>
    /// </summary>
    protected static Drawing GetPicture(DocXDocument document, Run run, Image bmp)
    {
        var imagePart = document.MainDocumentPart.AddImagePart(ImagePartType.Png);

        using (var ms = new MemoryStream())
        {
            bmp.SaveAsPng(ms);
            ms.Seek(0, SeekOrigin.Begin);
            imagePart.FeedData(ms);
        }

        var relationshipId = document.MainDocumentPart.GetIdOfPart(imagePart);
        var pictureId = (uint)document.MainDocumentPart.ImageParts.Count();
        var name = $"Picture {pictureId}";

        var drawing = CreateInlineDrawing(relationshipId, pictureId, name, bmp.Width * EmuPerPixel,
            bmp.Height * EmuPerPixel);
        run.AppendChild(drawing);
        return drawing;
    }

    private static Drawing CreateInlineDrawing(string relationshipId, uint pictureId, string name, long cx, long cy) =>
        new(
            new DW.Inline(
                new DW.Extent { Cx = cx, Cy = cy },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = pictureId, Name = name },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = $"{name}.png" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = cx, Cy = cy }),
                                new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle }))
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
            )
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            });

    /// <summary>
    /// Adds a table with single line borders, fixed layout and equal width columns to the end of <paramref name="document"/>.
    /// Every cell starts with one empty paragraph.
    /// </summary>
    protected static Table InsertTable(DocXDocument document, int rowCount, int colCount)
    {
        var colWidth = (TableWidth / colCount).ToString();

        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single },
                    new LeftBorder { Val = BorderValues.Single },
                    new BottomBorder { Val = BorderValues.Single },
                    new RightBorder { Val = BorderValues.Single },
                    new InsideHorizontalBorder { Val = BorderValues.Single },
                    new InsideVerticalBorder { Val = BorderValues.Single }),
                new TableLayout { Type = TableLayoutValues.Fixed }));

        var grid = new TableGrid();
        for (var i = 0; i < colCount; i++)
            grid.AppendChild(new GridColumn { Width = colWidth });
        table.AppendChild(grid);

        for (var r = 0; r < rowCount; r++)
        {
            var row = new TableRow();
            for (var c = 0; c < colCount; c++)
                row.AppendChild(new TableCell(
                    new TableCellProperties(new TableCellWidth { Width = colWidth, Type = TableWidthUnitValues.Dxa }),
                    new Paragraph()));
            table.AppendChild(row);
        }

        return document.Append(table);
    }

    protected static FileInfo GetUniqueFilenameInWorkArea(string desiredName, string extension = ".docx")
    {
        var root = GetTempPath();


        var f = new FileInfo(Path.Combine(root.FullName,
            UsefulStuff.RemoveIllegalFilenameCharacters(desiredName) + extension));
        var i = 1;

        //file name is taken
        while (f.Exists)
        {
            f = new FileInfo(Path.Combine(root.FullName, $"{desiredName}_{i}{extension}"));
            i++;

            //give up it has clearly gone horribly wrong
            if (i > 100)
                return new FileInfo(Path.Combine(root.FullName, $"F{Guid.NewGuid()}{extension}"));
        }

        return f;
    }

    protected static DirectoryInfo GetTempPath() => new(Path.GetTempPath());

    /// <summary>
    /// Creates a new document in Work Area (temp) - see <see cref="GetUniqueFilenameInWorkArea(string, string)"/>
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    protected static DocXDocument GetNewDocFile(string filename) => new(GetUniqueFilenameInWorkArea(filename));

    /// <summary>
    /// Opens windows explorer to show the file
    /// </summary>
    /// <param name="fileInfo"></param>
    protected static void ShowFile(FileInfo fileInfo)
    {
        UsefulStuff.ShowPathInWindowsExplorer(fileInfo);
    }

    /// <summary>
    /// Opens windows explorer to show the document
    /// </summary>
    /// <param name="document"></param>
    protected static void ShowFile(DocXDocument document)
    {
        ShowFile(document.FileInfo);
    }

    /// <summary>
    /// Adds a default footer to the document containing <paramref name="text"/>
    /// </summary>
    protected static void AddFooter(DocXDocument document, string text, int textFontSize)
    {
        var footerPart = document.MainDocumentPart.AddNewPart<FooterPart>();
        footerPart.Footer =
            new Footer(new Paragraph(CreateTextRun(text, textFontSize != -1 ? textFontSize : DefaultFontSize)));
        footerPart.Footer.Save();

        // header/footer references must come first in sectPr
        document.SectionProperties.PrependChild(new FooterReference
        {
            Type = HeaderFooterValues.Default,
            Id = document.MainDocumentPart.GetIdOfPart(footerPart)
        });
    }

    /// <summary>
    /// Creates a new document in the location of <paramref name="fileInfo"/>
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    protected static DocXDocument GetNewDocFile(FileInfo fileInfo) => new(fileInfo);

    protected static void InsertSectionPageBreak(DocXDocument document)
    {
        document.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
    }

    protected static void SetLandscape(DocXDocument document)
    {
        var pageSize = document.SectionProperties.GetFirstChild<PageSize>();
        if (pageSize == null)
        {
            pageSize = new PageSize();
            document.SectionProperties.AppendChild(pageSize);
        }

        pageSize.Orient = PageOrientationValues.Landscape;
        pageSize.Width = 842 * 20;
        pageSize.Height = 595 * 20;
    }

    protected static void InsertTableOfContents(DocXDocument document)
    {
        //todo
        //document.InsertTableOfContents("Contents", new TableOfContentsSwitches());
    }

    protected static void AutoFit(Table table)
    {
        //tables use a fixed layout with equal column widths (see InsertTable)
        //table.AutoFit = AutoFit.Contents;
    }

    /// <summary>
    /// Sets the left and right page margins to <paramref name="marginSize"/> in hundredths of an inch e.g. 20 = 0.20"
    /// </summary>
    /// <param name="document"></param>
    /// <param name="marginSize"></param>
    protected static void SetMargins(DocXDocument document, int marginSize)
    {
        var margin = document.SectionProperties.GetFirstChild<PageMargin>();
        if (margin == null)
        {
            margin = new PageMargin();
            document.SectionProperties.AppendChild(margin);
        }

        margin.Right = (uint)(marginSize * 14.60);
        margin.Left = (uint)(marginSize * 14.60);
    }

    protected static float GetPageWidth() => 500;
}
