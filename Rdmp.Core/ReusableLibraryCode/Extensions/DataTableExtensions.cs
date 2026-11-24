// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Data;
using System.IO;
using System.Text;
using CsvHelper;

namespace Rdmp.Core.ReusableLibraryCode.Extensions;

public static class DataTableExtensions
{
    /// <summary>
    /// Formats the data in the <paramref name="dt"/> to CSV format to the given <paramref name="stream"/>.  The output does not depend
    /// on the regional settings of the computer: the separator is a comma, numbers use '.' and dates are ISO 8601.
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="stream"></param>
    public static void SaveAsCsv(this DataTable dt, StreamWriter stream)
    {
        using var csvWriter = CsvWriter.Create(stream);

        // a record must have at least one field
        if (dt.Columns.Count == 0)
            return;

        foreach (DataColumn column in dt.Columns)
            csvWriter.WriteField(column.ColumnName);

        csvWriter.EndRecord();

        foreach (DataRow row in dt.Rows)
        {
            for (var i = 0; i < dt.Columns.Count; i++)
                csvWriter.WriteField(row[i]);

            csvWriter.EndRecord();
        }
    }

    public static void SaveAsCsv(this DataTable dt, string path)
    {
        using var stream = new StreamWriter(path, false, Encoding.UTF8, 1 << 20);
        dt.SaveAsCsv(stream);
    }
}