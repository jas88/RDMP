// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Rdmp.Core.Reports;

/// <summary>
/// A new Microsoft Word .docx file being written to disk via DocumentFormat.OpenXml.  The document is saved and the
/// underlying file closed when this object is disposed.
/// <para>Content added through <see cref="Append"/> is always kept ahead of the body's final
/// <see cref="SectionProperties"/> (page size, margins, footer reference) as the OOXML schema requires.</para>
/// </summary>
public sealed class DocXDocument : IDisposable
{
    // Page geometry in twentieths of a point, matching the defaults previously produced by NPOI (A4 portrait)
    private const int A4WidthTwips = 11906;
    private const int A4HeightTwips = 16838;
    private const int DefaultTopBottomMargin = 1440;
    private const int DefaultLeftRightMargin = 1800;
    private const int DefaultHeaderFooterMargin = 851;
    private const int DefaultFooterMargin = 992;

    private readonly WordprocessingDocument _package;
    private bool _disposed;

    /// <summary>
    /// The file the document is being written to
    /// </summary>
    public FileInfo FileInfo { get; }

    /// <summary>
    /// The main part of the package (used for adding images, footers etc)
    /// </summary>
    public MainDocumentPart MainDocumentPart { get; }

    /// <summary>
    /// The document body.  Prefer <see cref="Append"/> for adding content so it stays ahead of <see cref="SectionProperties"/>
    /// </summary>
    public Body Body { get; }

    /// <summary>
    /// The final (document level) section properties: page size, orientation, margins and header/footer references
    /// </summary>
    public SectionProperties SectionProperties { get; }

    /// <summary>
    /// Creates (or overwrites) a .docx file at <paramref name="fileInfo"/>
    /// </summary>
    /// <param name="fileInfo"></param>
    public DocXDocument(FileInfo fileInfo)
    {
        FileInfo = fileInfo;
        _package = WordprocessingDocument.Create(fileInfo.FullName, WordprocessingDocumentType.Document);
        MainDocumentPart = _package.AddMainDocumentPart();

        SectionProperties = new SectionProperties(
            new PageSize { Width = A4WidthTwips, Height = A4HeightTwips },
            new PageMargin
            {
                Top = DefaultTopBottomMargin,
                Right = DefaultLeftRightMargin,
                Bottom = DefaultTopBottomMargin,
                Left = DefaultLeftRightMargin,
                Header = DefaultHeaderFooterMargin,
                Footer = DefaultFooterMargin,
                Gutter = 0
            },
            new Columns { Space = "425" });

        Body = new Body(SectionProperties);
        MainDocumentPart.Document = new Document(Body);
    }

    /// <summary>
    /// Adds <paramref name="element"/> to the end of the document content (ahead of the final section properties)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="element"></param>
    /// <returns><paramref name="element"/></returns>
    public T Append<T>(T element) where T : OpenXmlElement
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Body.InsertBefore(element, SectionProperties);
        return element;
    }

    /// <summary>
    /// Saves the document and releases the underlying file.  Safe to call more than once, so callers can dispose
    /// explicitly (e.g. before showing the file to the user) while still holding a <c>using</c> declaration.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        MainDocumentPart.Document.Save();
        _package.Dispose();
    }
}
