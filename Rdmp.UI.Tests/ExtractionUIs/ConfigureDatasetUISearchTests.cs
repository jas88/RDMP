// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using NUnit.Framework;
using Rdmp.Core.DataExport.Data;
using Rdmp.UI.ProjectUI.Datasets;

namespace Rdmp.UI.Tests.ExtractionUIs;

/// <summary>
/// Tests that the three search boxes of <see cref="ConfigureDatasetUI"/>, which share a text changed handler, each
/// filter their own list and leave the other two alone
/// </summary>
/// <remarks>
/// Not part of <see cref="ConfigureDatasetUITests"/> because the user interface looks its Catalogue up in the
/// activator's child provider, which is built when the first test of a class first uses the activator.  A second
/// test's Catalogue would not be in it, and the user interface reports that in a dialog, which a test would wait
/// on until it timed out.
/// </remarks>
internal class ConfigureDatasetUISearchTests : UITests
{
    [Test]
    [UITimeout(50000)]
    public void Test_ConfigureDatasetUI_EachSearchBoxFiltersOnlyItsOwnList()
    {
        // an extraction of a dataset with the columns MyCataItem and My_Col2, both from My_Table
        var sds = WhenIHaveA<SelectedDataSets>();
        AndLaunch<ConfigureDatasetUI>(sds);

        AssertListed("olvAvailable", "MyCataItem", "My_Col2");
        AssertListed("olvSelected", "MyCataItem", "My_Col2");
        AssertListed("olvJoin", "My_Table");

        GetControl<TextBox>("tbSearchAvailable").Text = "My_Col2";
        AssertListed("olvAvailable", "My_Col2");
        AssertListed("olvSelected", "MyCataItem", "My_Col2");
        AssertListed("olvJoin", "My_Table");

        GetControl<TextBox>("tbSearchSelected").Text = "MyCataItem";
        AssertListed("olvAvailable", "My_Col2");
        AssertListed("olvSelected", "MyCataItem");
        AssertListed("olvJoin", "My_Table");

        GetControl<TextBox>("tbSearchTables").Text = "Not the name of any table";
        AssertListed("olvAvailable", "My_Col2");
        AssertListed("olvSelected", "MyCataItem");
        AssertListed("olvJoin");

        // clearing a search box shows everything in its list again
        GetControl<TextBox>("tbSearchAvailable").Text = "";
        AssertListed("olvAvailable", "MyCataItem", "My_Col2");
        AssertListed("olvSelected", "MyCataItem");
        AssertListed("olvJoin");
    }

    /// <summary>
    /// Asserts that the list called <paramref name="listName"/> shows, after any filtering, the objects called
    /// <paramref name="expected"/> and no others
    /// </summary>
    private void AssertListed(string listName, params string[] expected)
    {
        // an unfiltered list that has been cleared has no objects collection at all
        var shown = GetControl<ObjectListView>(listName).FilteredObjects ?? Array.Empty<object>();

        Assert.That(shown.Cast<object>().Select(static o => o.ToString()), Is.EquivalentTo(expected),
            $"Objects shown in {listName}");
    }
}
