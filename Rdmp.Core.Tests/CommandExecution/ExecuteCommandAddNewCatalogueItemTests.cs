// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests for how <see cref="ExecuteCommandAddNewCatalogueItem"/> decides which columns a Catalogue already has
/// </summary>
internal class ExecuteCommandAddNewCatalogueItemTests : CommandCliTests
{
    [Test]
    public void Constructor_ForAColumnThatIsAlreadyInTheCatalogue_IsImpossible()
    {
        var catalogue = WhenIHaveACatalogueWithAColumnAndAnItemWithoutOne(out var existingColumn);

        var cmd = new ExecuteCommandAddNewCatalogueItem(GetActivator(), catalogue, existingColumn);

        Assert.That(cmd.ReasonCommandImpossible, Is.EqualTo("ColumnInfo(s) are already in Catalogue"));
    }

    [Test]
    public void Execute_ForAColumnThatIsNotInTheCatalogue_AddsIt()
    {
        var catalogue = WhenIHaveACatalogueWithAColumnAndAnItemWithoutOne(out var existingColumn);
        var newColumn = new ColumnInfo(Repository, "My_New_Col", "varchar(10)", existingColumn.TableInfo);

        var cmd = new ExecuteCommandAddNewCatalogueItem(GetActivator(), catalogue, existingColumn, newColumn);
        Assert.That(cmd.IsImpossible, Is.False, cmd.ReasonCommandImpossible);

        cmd.Execute();

        // the column that was already in the Catalogue is not added a second time
        Assert.That(Repository.GetAllObjectsWithParent<CatalogueItem>(catalogue).Select(static ci => ci.ColumnInfo_ID),
            Is.EquivalentTo(new int?[] { existingColumn.ID, null, newColumn.ID }));
    }

    /// <summary>
    /// Returns a Catalogue with two items: one for <paramref name="existingColumn"/>, and one that has no column
    /// </summary>
    private Catalogue WhenIHaveACatalogueWithAColumnAndAnItemWithoutOne(out ColumnInfo existingColumn)
    {
        var ei = WhenIHaveA<ExtractionInformation>();
        var catalogue = ei.CatalogueItem.Catalogue;
        existingColumn = ei.ColumnInfo;

        _ = new CatalogueItem(Repository, catalogue, "Item with no column");

        return catalogue;
    }
}
