// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using System;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation.Integration;

public class ExtractionFilterTests : UnitTests
{
    [Test]
    public void TestExtractionFilterDeleting_WhenItHas_ExtractionFilterParameterSet_DirectlyFails()
    {
        var filter = GetFilterWithParameterSet();
        var ex = Assert.Throws<Exception>(() => filter.DeleteInDatabase());
        Assert.That(ex.Message, Is.EqualTo("Cannot delete 'Age' because there are one or more ExtractionFilterParameterSet declared on it"));
    }

    private ExtractionFilter GetFilterWithParameterSet()
    {
        var cata = new Catalogue(Repository, "myCata");
        var cataItem = new CatalogueItem(Repository, cata, "MyCol");

        var table = new TableInfo(Repository, "myTbl");
        var col = new ColumnInfo(Repository, "myCol", "varchar(10)", table);

        var ei = new ExtractionInformation(Repository, cataItem, col, "[myTbl].[mycol]");
        var filter = new ExtractionFilter(Repository, "Age", ei)
        {
            WhereSQL = "Age >= @age"
        };
        new ExtractionFilterParameter(Repository, "DECLARE @age int", filter);

        var paramSet = new ExtractionFilterParameterSet(Repository, filter, "Old");
        paramSet.CreateNewValueEntries();

        return filter;
    }
}