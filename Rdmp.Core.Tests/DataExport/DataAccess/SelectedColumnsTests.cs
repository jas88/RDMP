// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Tests.Common;

namespace Rdmp.Core.Tests.DataExport.DataAccess;

public class SelectedColumnsTests : UnitTests
{
    //Simple test SelectedColumns in which an extraction configuration is built for a test dataset with a single column configured for extraction
    [Test]
    public void CreateAndAssociateColumns()
    {
        var cata = new Catalogue(Repository, "MyCat");
        var cataItem = new CatalogueItem(Repository, cata, "MyCataItem");
        var TableInfo = new TableInfo(Repository, "Cata");
        var ColumnInfo = new ColumnInfo(Repository, "Col", "varchar(10)", TableInfo);
        var ExtractionInfo = new ExtractionInformation(Repository, cataItem, ColumnInfo, "fish");

        var ds = new ExtractableDataSet(Repository, cata);

        var proj = new Project(Repository, "MyProj");
        var config = new ExtractionConfiguration(Repository, proj);

        var extractableColumn = new ExtractableColumn(Repository, ds, config, ExtractionInfo, 1, "fish");

        try
        {
            _ = new SelectedDataSets(Repository, config, ds, null);

            var cols = config.GetAllExtractableColumnsFor(ds);

            Assert.That(cols, Has.Length.EqualTo(1));
            Assert.That(cols.Single(), Is.EqualTo(extractableColumn));

            cols = config.GetAllExtractableColumnsFor(ds);

            Assert.That(cols, Has.Length.EqualTo(1));
            Assert.That(cols.Single(), Is.EqualTo(extractableColumn));
        }
        finally
        {
            extractableColumn.DeleteInDatabase();
            config.DeleteInDatabase();
            proj.DeleteInDatabase();

            ds.DeleteInDatabase();

            TableInfo.DeleteInDatabase();
            cata.DeleteInDatabase();
        }
    }
}