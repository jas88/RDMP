// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Extensions;

namespace Rdmp.Core.Tests.ReusableCodeTests;

[Category("Unit")]
internal class DataTableExtensionsTests
{
    [Test]
    public void TestEscaping_CommaInCell()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "out.csv");

        using var dt = new DataTable();
        dt.Columns.Add("Phrase");
        dt.Columns.Add("Car");

        dt.Rows.Add("omg,why me!", "Ferrari");

        dt.SaveAsCsv(path);

        var answer = File.ReadAllText(path).Replace("\r\n", "\n");

        var expected = """
                       Phrase,Car
                       "omg,why me!",Ferrari

                       """.Replace("\r\n", "\n");

        Assert.That(answer, Is.EqualTo(expected));
    }

    [Test]
    public void TestEscaping_CommaAndQuotesInCell()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "out.csv");

        using var dt = new DataTable();
        dt.Columns.Add("Phrase");
        dt.Columns.Add("Car");

        dt.Rows.Add("omg,\"why\" me!", "Ferrari");

        dt.SaveAsCsv(path);

        var answer = File.ReadAllText(path).Replace("\r\n", "\n");

        var expected = """
                    Phrase,Car
                    "omg,""why"" me!",Ferrari

                    """.Replace("\r\n", "\n");

        Assert.That(answer, Is.EqualTo(expected));
    }


    [Test]
    public void TestEscaping_CommaAndQuotesInCell2()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "out.csv");

        using var dt = new DataTable();
        dt.Columns.Add("Phrase");
        dt.Columns.Add("Car");

        dt.Rows.Add("\"omg,why me!\"", "Ferrari");

        dt.SaveAsCsv(path);

        var answer = File.ReadAllText(path).Replace("\r\n", "\n");

        var expected = """"
                    Phrase,Car
                    """omg,why me!""",Ferrari

                    """".Replace("\r\n", "\n");

        Assert.That(answer, Is.EqualTo(expected));
    }

    /// <summary>
    /// The file must be the same whatever the regional settings of the computer: German uses ';' to separate lists,
    /// ',' as the decimal separator and dd.MM.yyyy dates
    /// </summary>
    [Test]
    public void SaveAsCsv_GermanCulture_WritesInvariantValues()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "outInvariant.csv");

        using var dt = new DataTable();
        dt.Columns.Add("When", typeof(DateTime));
        dt.Columns.Add("Amount", typeof(decimal));

        dt.Rows.Add(new DateTime(2020, 1, 2, 3, 4, 5), 1.5m);

        var cultureBefore = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            dt.SaveAsCsv(path);
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureBefore;
        }

        var answer = File.ReadAllText(path).Replace("\r\n", "\n");

        var expected = """
                       When,Amount
                       2020-01-02T03:04:05.0000000,1.5

                       """.Replace("\r\n", "\n");

        Assert.That(answer, Is.EqualTo(expected));
    }
}