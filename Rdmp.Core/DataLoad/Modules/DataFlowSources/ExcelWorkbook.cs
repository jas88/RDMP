// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Text;
using ExcelDataReader;

namespace Rdmp.Core.DataLoad.Modules.DataFlowSources;

/// <summary>
/// An Excel workbook (.xls, .xlsx, .xlsb) opened for reading with ExcelDataReader, together with the workbook level
/// settings needed to interpret its cells.
/// </summary>
public sealed class ExcelWorkbook : IDisposable
{
    static ExcelWorkbook()
    {
        // Legacy .xls files store text in Windows code pages which .NET (Core) does not provide by default
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// The reader, positioned at the first worksheet when opened.  It owns the underlying file handle.
    /// </summary>
    public IExcelDataReader Reader { get; }

    /// <summary>
    /// True if the workbook uses the 1904 date system (serial number 0 is 1904-01-01)
    /// </summary>
    public bool IsDate1904 { get; }

    private ExcelWorkbook(IExcelDataReader reader, bool isDate1904)
    {
        Reader = reader;
        IsDate1904 = isDate1904;
    }

    /// <summary>
    /// Opens the workbook for reading, the format (xls/xlsx/xlsb) is detected from the file contents.
    /// </summary>
    public static ExcelWorkbook Open(FileInfo file)
    {
        var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        try
        {
            var isDate1904 = ExcelDateSystem.IsDate1904(stream);
            return new ExcelWorkbook(ExcelReaderFactory.CreateReader(stream), isDate1904);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Returns the C# value that best represents cell <paramref name="column"/> of the current row (see
    /// <see cref="ExcelCellReader.GetCellValue"/>).
    /// </summary>
    public object GetCellValue(int column) => ExcelCellReader.GetCellValue(Reader, column, IsDate1904);

    public void Dispose() => Reader.Dispose();
}
