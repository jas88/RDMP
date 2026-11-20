// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.UI.AggregationUIs;
using Rdmp.UI.ItemActivation;

namespace Rdmp.UI.Tests;

internal class AggregateGraphUITests : UITests
{
    private sealed class CollectionGraphUI : AggregateGraphUI
    {
        // like FilterGraphUI, CohortSummaryAggregateGraphUI and ExtractionAggregateGraph, which show a collection not a DatabaseObject
        public void BuildMenuWithoutADatabaseObject(IActivateItems activator)
        {
            SetItemActivator(activator);
            BuildMenu(activator);
        }
    }

    [Test]
    [UITimeout(50000)]
    public void BuildMenu_WithoutADatabaseObject_DoesNotThrow()
    {
        var ui = new CollectionGraphUI();
        AndLaunch(ui);

        Assert.DoesNotThrow(() => ui.BuildMenuWithoutADatabaseObject(ItemActivator));
    }
}
