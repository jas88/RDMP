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
using Rdmp.Core.Curation.Data;
using Rdmp.UI.ExtractionUIs;

namespace Rdmp.UI.Tests.ExtractionUIs;

/// <summary>
/// Tests that the Core, Supplemental, Special Approval and Internal buttons of <see cref="ViewExtractionSqlUI"/>, which
/// share a click handler, work as radio buttons: the one clicked becomes the only one checked, and the columns listed
/// are those of its category and the categories it includes
/// </summary>
internal class ViewExtractionSqlUITests : UITests
{
    private static readonly string[] CategoryButtons = { "Core", "Supplemental", "Special Approval", "Internal" };

    [Test]
    [UITimeout(50000)]
    public void Test_ViewExtractionSqlUI_CategoryButtonsWorkAsRadioButtons()
    {
        var core = WhenIHaveA<ExtractionInformation>();
        var catalogue = core.CatalogueItem.Catalogue;

        var supplemental = CreateColumn(core, "Supp", ExtractionCategory.Supplemental);
        var specialApproval = CreateColumn(core, "Special", ExtractionCategory.SpecialApprovalRequired);
        var internalColumn = CreateColumn(core, "Secret", ExtractionCategory.Internal);
        catalogue.ClearAllInjections();

        var ui = AndLaunch<ViewExtractionSqlUI>(catalogue);
        AssertShowing(ui, "Supplemental", core, supplemental);

        ClickCategory(ui, "Core");
        AssertShowing(ui, "Core", core);

        ClickCategory(ui, "Special Approval");
        AssertShowing(ui, "Special Approval", core, supplemental, specialApproval);

        ClickCategory(ui, "Internal");
        AssertShowing(ui, "Internal", internalColumn);

        ClickCategory(ui, "Supplemental");
        AssertShowing(ui, "Supplemental", core, supplemental);
    }

    /// <summary>
    /// Creates a column of the given <paramref name="category"/> in the same Catalogue and table as
    /// <paramref name="sibling"/>
    /// </summary>
    private ExtractionInformation CreateColumn(ExtractionInformation sibling, string name,
        ExtractionCategory category)
    {
        var catalogueItem = new CatalogueItem(Repository, sibling.CatalogueItem.Catalogue, name);
        var columnInfo = new ColumnInfo(Repository, name, "varchar(10)", sibling.ColumnInfo.TableInfo);

        var extractionInformation = new ExtractionInformation(Repository, catalogueItem, columnInfo, name)
        {
            ExtractionCategory = category
        };
        extractionInformation.SaveToDatabase();

        return extractionInformation;
    }

    private static ToolStripButton[] GetCategoryButtons(ViewExtractionSqlUI ui) =>
        ui.CommonFunctionality.ToolStrip.Items.OfType<ToolStripButton>()
            .Where(static b => CategoryButtons.Contains(b.Text)).ToArray();

    /// <summary>
    /// Clicks the category button labelled <paramref name="category"/>.  These are tool strip buttons, which unlike
    /// <see cref="Button"/> can be clicked without being shown.
    /// </summary>
    private static void ClickCategory(ViewExtractionSqlUI ui, string category)
    {
        var button = GetCategoryButtons(ui).Single(b => b.Text == category);
        Assert.That(button.Enabled, $"The {category} button is disabled so the user could not click it");

        button.PerformClick();
    }

    private void AssertShowing(ViewExtractionSqlUI ui, string checkedButton,
        params ExtractionInformation[] expectedColumns)
    {
        // a list that has been cleared has no objects collection at all
        var columns = GetControl<ObjectListView>("olvExtractionInformations").Objects ?? Array.Empty<object>();

        Assert.Multiple(() =>
        {
            Assert.That(GetCategoryButtons(ui).Select(static b => b.Text), Is.EquivalentTo(CategoryButtons),
                "The category buttons on the tool strip");
            Assert.That(GetCategoryButtons(ui).Where(static b => b.Checked).Select(static b => b.Text),
                Is.EqualTo(new[] { checkedButton }), "The checked category buttons");
            Assert.That(columns, Is.EquivalentTo(expectedColumns), $"Columns listed for {checkedButton}");
        });
    }
}
