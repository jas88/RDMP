// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace Rdmp.Core.Tests.DataLoad.Engine.Unit;

/// <summary>
/// Writes small .xlsx workbooks directly as raw XML parts in a zip (no third party library) so that tests can control exactly
/// which cells physically exist, their number formats and cached formula results.
/// </summary>
public sealed class MinimalXlsxBuilder
{
    /// <summary>Named number formats for <see cref="Number"/>: either a built-in format id or a custom format code</summary>
    private static readonly Dictionary<string, (int BuiltinId, string Custom)> NamedStyles = new()
    {
        { "General", (0, null) },
        { "BuiltinDate", (14, null) },
        { "Percent", (10, null) },
        { "DateTime", (-1, "yyyy-mm-dd hh:mm") },
        { "Time", (-1, "hh:mm:ss") },
        { "TwoDp", (-1, "0.00") },
        { "MinSec", (-1, "mm:ss") },
        { "DayMonth", (-1, "d-mmm") }
    };

    /// <summary>numFmtId of each cellXfs entry, index 0 is always General</summary>
    private readonly List<int> _cellXfs = new() { 0 };

    /// <summary>Custom number format codes and their numFmtId</summary>
    private readonly Dictionary<string, int> _customFormats = new();

    private readonly List<(string Name, List<string> CellXml)> _sheets = new();
    private bool _date1904;
    private readonly List<string> _sharedStrings = new();

    /// <summary>Makes the workbook use the 1904 date system (serial 0 is 1904-01-01)</summary>
    public MinimalXlsxBuilder Date1904()
    {
        _date1904 = true;
        return this;
    }

    /// <summary>Starts a new worksheet; subsequent cells are added to it</summary>
    public MinimalXlsxBuilder Sheet(string name)
    {
        _sheets.Add((name, new List<string>()));
        return this;
    }

    /// <summary>Adds a shared string cell at the 1-based Excel reference e.g. "B2"</summary>
    public MinimalXlsxBuilder Text(string reference, string value)
    {
        _sharedStrings.Add(value);
        return Cell(reference, $"<c r=\"{reference}\" t=\"s\"><v>{_sharedStrings.Count - 1}</v></c>");
    }

    /// <summary>Adds a numeric cell with the given named number format</summary>
    public MinimalXlsxBuilder Number(string reference, double value, string style = "General") =>
        Cell(reference, $"<c r=\"{reference}\" s=\"{StyleIndex(style)}\"><v>{Num(value)}</v></c>");

    /// <summary>Adds a numeric cell using the given built-in number format id (e.g. 14 for a short date)</summary>
    public MinimalXlsxBuilder NumberWithBuiltinFormat(string reference, double value, int numFmtId) =>
        Cell(reference, $"<c r=\"{reference}\" s=\"{XfFor(numFmtId)}\"><v>{Num(value)}</v></c>");

    /// <summary>Adds a numeric cell using a custom number format code e.g. "0.000"</summary>
    public MinimalXlsxBuilder NumberWithCustomFormat(string reference, double value, string formatCode) =>
        Cell(reference, $"<c r=\"{reference}\" s=\"{XfFor(CustomFormatId(formatCode))}\"><v>{Num(value)}</v></c>");

    /// <summary>Adds a boolean cell</summary>
    public MinimalXlsxBuilder Bool(string reference, bool value) =>
        Cell(reference, $"<c r=\"{reference}\" t=\"b\"><v>{(value ? 1 : 0)}</v></c>");

    /// <summary>Adds an error cell e.g. #DIV/0!</summary>
    public MinimalXlsxBuilder Error(string reference, string error) =>
        Cell(reference, $"<c r=\"{reference}\" t=\"e\"><v>{Escape(error)}</v></c>");

    /// <summary>Adds a cell that physically exists but has no value (styled blank)</summary>
    public MinimalXlsxBuilder Blank(string reference) =>
        Cell(reference, $"<c r=\"{reference}\" s=\"{StyleIndex("TwoDp")}\"/>");

    /// <summary>Adds a formula cell whose cached result is a number</summary>
    public MinimalXlsxBuilder NumberFormula(string reference, string formula, double cached, string style = "General") =>
        Cell(reference,
            $"<c r=\"{reference}\" s=\"{StyleIndex(style)}\"><f>{Escape(formula)}</f><v>{Num(cached)}</v></c>");

    /// <summary>Adds a formula cell whose cached result is a string</summary>
    public MinimalXlsxBuilder TextFormula(string reference, string formula, string cached) =>
        Cell(reference, $"<c r=\"{reference}\" t=\"str\"><f>{Escape(formula)}</f><v>{Escape(cached)}</v></c>");

    /// <summary>Adds a formula cell whose cached result is an error</summary>
    public MinimalXlsxBuilder ErrorFormula(string reference, string formula, string cached) =>
        Cell(reference, $"<c r=\"{reference}\" t=\"e\"><f>{Escape(formula)}</f><v>{Escape(cached)}</v></c>");

    private MinimalXlsxBuilder Cell(string reference, string xml)
    {
        if (_sheets.Count == 0)
            Sheet("Sheet1");

        _sheets[^1].CellXml.Add(xml);
        return this;
    }

    /// <summary>Writes the workbook to <paramref name="path"/> and returns it</summary>
    public FileInfo Save(string path)
    {
        if (_sheets.Count == 0)
            Sheet("Sheet1");

        File.Delete(path);
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            Write(zip, "[Content_Types].xml", ContentTypes());
            Write(zip, "_rels/.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");
            Write(zip, "xl/workbook.xml", Workbook());
            Write(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
            Write(zip, "xl/styles.xml", StylesXml());
            Write(zip, "xl/sharedStrings.xml", SharedStrings());

            for (var i = 0; i < _sheets.Count; i++)
                Write(zip, $"xl/worksheets/sheet{i + 1}.xml", SheetXml(_sheets[i].CellXml));
        }

        return new FileInfo(path);
    }

    private string ContentTypes()
    {
        var sb = new StringBuilder(
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>");
        for (var i = 1; i <= _sheets.Count; i++)
            sb.Append(
                $"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        return sb.Append("</Types>").ToString();
    }

    private string Workbook()
    {
        var sb = new StringBuilder(
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        if (_date1904)
            sb.Append("<workbookPr date1904=\"1\"/>");
        sb.Append("<sheets>");
        for (var i = 0; i < _sheets.Count; i++)
            sb.Append($"<sheet name=\"{Escape(_sheets[i].Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
        return sb.Append("</sheets></workbook>").ToString();
    }

    private string WorkbookRels()
    {
        var sb = new StringBuilder(
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (var i = 0; i < _sheets.Count; i++)
            sb.Append(
                $"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
        var n = _sheets.Count;
        sb.Append(
            $"<Relationship Id=\"rId{n + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        sb.Append(
            $"<Relationship Id=\"rId{n + 2}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>");
        return sb.Append("</Relationships>").ToString();
    }

    private string StylesXml()
    {
        var custom = _customFormats.ToArray();
        var sb = new StringBuilder(
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        if (custom.Length > 0)
        {
            sb.Append($"<numFmts count=\"{custom.Length}\">");
            foreach (var (code, id) in custom)
                sb.Append($"<numFmt numFmtId=\"{id}\" formatCode=\"{Escape(code)}\"/>");
            sb.Append("</numFmts>");
        }
        sb.Append(
            "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
            "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
            "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");
        sb.Append($"<cellXfs count=\"{_cellXfs.Count}\">");
        foreach (var id in _cellXfs)
            sb.Append(
                $"<xf numFmtId=\"{id}\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"{(id == 0 ? "" : " applyNumberFormat=\"1\"")}/>");
        sb.Append("</cellXfs>");
        return sb.Append("</styleSheet>").ToString();
    }

    private string SharedStrings()
    {
        var sb = new StringBuilder(
            $"<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"{_sharedStrings.Count}\" uniqueCount=\"{_sharedStrings.Count}\">");
        foreach (var s in _sharedStrings)
            sb.Append($"<si><t xml:space=\"preserve\">{Escape(s)}</t></si>");
        return sb.Append("</sst>").ToString();
    }

    private static string SheetXml(IEnumerable<string> cells)
    {
        var rows = cells
            .Select(xml => (xml, Ref: ParseReference(xml)))
            .GroupBy(c => c.Ref.Row)
            .OrderBy(g => g.Key);

        var sb = new StringBuilder(
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        foreach (var row in rows)
        {
            sb.Append($"<row r=\"{row.Key}\">");
            foreach (var cell in row.OrderBy(c => c.Ref.Column))
                sb.Append(cell.xml);
            sb.Append("</row>");
        }

        return sb.Append("</sheetData></worksheet>").ToString();
    }

    private static (int Row, int Column) ParseReference(string cellXml)
    {
        var start = cellXml.IndexOf("r=\"", StringComparison.Ordinal) + 3;
        var reference = cellXml[start..cellXml.IndexOf('"', start)];
        var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
        var column = letters.Aggregate(0, (acc, ch) => acc * 26 + (char.ToUpperInvariant(ch) - 'A' + 1));
        return (int.Parse(reference[letters.Length..], CultureInfo.InvariantCulture), column);
    }

    private int StyleIndex(string style)
    {
        if (!NamedStyles.TryGetValue(style, out var s))
            throw new ArgumentException($"Unknown style '{style}'", nameof(style));
        return XfFor(s.Custom == null ? s.BuiltinId : CustomFormatId(s.Custom));
    }

    private int CustomFormatId(string formatCode)
    {
        if (!_customFormats.TryGetValue(formatCode, out var id))
        {
            id = 164 + _customFormats.Count;
            _customFormats.Add(formatCode, id);
        }

        return id;
    }

    private int XfFor(int numFmtId)
    {
        var index = _cellXfs.IndexOf(numFmtId);
        if (index >= 0)
            return index;

        _cellXfs.Add(numFmtId);
        return _cellXfs.Count - 1;
    }

    private static string Num(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Escape(string s) => SecurityElement.Escape(s);

    private static void Write(ZipArchive zip, string name, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        writer.Write(content);
    }
}
