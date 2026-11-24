// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Tests.Common;

namespace Rdmp.Core.Tests.DataExport.DataExtraction;

internal class SupplementalExtractionResultsTest : UnitTests
{
    [Test]
    public void TestCreating()
    {
        var p = new Project(Repository, "MyProj");

        var ec = new ExtractionConfiguration(Repository, p);

        var cata = new Catalogue(Repository, "MyCata");
        var tbl = new SupportingSQLTable(Repository, cata, "Some global data");

        var othertbl = new SupportingSQLTable(Repository, cata, "Some global data");

        var result = new SupplementalExtractionResults(Repository, ec, "select * from Globalsglba", tbl);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsReferenceTo(typeof(SupportingSQLTable)));
            Assert.That(result.IsReferenceTo(tbl));
            Assert.That(result.IsReferenceTo(othertbl), Is.False);
        });
    }
}