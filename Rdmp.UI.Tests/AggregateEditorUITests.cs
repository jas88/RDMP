// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using System.Windows.Forms;
using FAnsi.Discovery.QuerySyntax.Aggregation;
using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;
using Rdmp.UI.AggregationUIs.Advanced;

namespace Rdmp.UI.Tests;

internal class AggregateEditorUITests : UITests
{
    [Test]
    [UITimeout(50000)]
    public void Test_AggregateEditorUI_NormalState()
    {
        var config = GetAggregateConfigurationWithNoDimensions();
        var ui = AndLaunch<AggregateEditorUI>(config);

        //The selected columns ui
        var colsUi = ui.selectColumnUI1;

        //should show two available columns
        var available = colsUi.AvailableColumns;
        Assert.That(available, Has.Count.EqualTo(2));

        //the count(*) column
        var included = colsUi.IncludedColumns;
        Assert.That(included, Has.Count.EqualTo(1));

        //before we have added any columns it should not be possible to launch the graph
        var cmdExecuteGraph = new ExecuteCommandExecuteAggregateGraph(ItemActivator, config);
        Assert.Multiple(() =>
        {
            Assert.That(cmdExecuteGraph.IsImpossible);
            Assert.That(cmdExecuteGraph.ReasonCommandImpossible, Does.Contain("No tables could be identified for the query.  Try adding a column or a force join"));
        });

        var ei = config.Catalogue.CatalogueItems[0].ExtractionInformation;

        //create a new dimension to the config in the database
        _ = new AggregateDimension(Repository, ei, config);

        //publish a refresh
        Publish(config);

        //should show one available columns
        available = colsUi.AvailableColumns;
        Assert.That(available, Has.Count.EqualTo(1));

        //the count(*) column and the added dimension
        included = colsUi.IncludedColumns;
        Assert.That(included, Has.Count.EqualTo(2));

        //should now be possible to launch the graph
        cmdExecuteGraph = new ExecuteCommandExecuteAggregateGraph(ItemActivator, config);
        Assert.That(cmdExecuteGraph.IsImpossible, Is.False);

        AssertNoErrors(ExpectedErrorType.Any);
    }

    [Test]
    [UITimeout(50000)]
    public void Test_AggregateEditorUI_AxisOnlyShowsDateDimensions()
    {
        var config = GetAggregateConfigurationWithNoDimensions(out var dateEi, out var otherEi);

        var dimDate = new AggregateDimension(Repository, dateEi, config);
        var dimOther = new AggregateDimension(Repository, otherEi, config);
        config.ClearAllInjections();

        var ui = AndLaunch<AggregateEditorUI>(config);

        //only date should be an option for axis dimension
        Assert.That(ui.ddAxisDimension.Items, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(ui.ddAxisDimension.Items[0], Is.EqualTo(dimDate));

            //dates are not valid for pivots
            Assert.That(ui.ddPivotDimension.Items, Has.Count.EqualTo(1));
        });
        Assert.That(ui.ddPivotDimension.Items[0], Is.EqualTo(dimOther));

        //it wants us to pick either a pivot or an axis
        AssertErrorWasShown(ExpectedErrorType.FailedCheck,
            "In order to have 2 columns, one must be selected as a pivot");
        config.PivotOnDimensionID = dimOther.ID;
        config.SaveToDatabase();

        Publish(config);

        AssertNoErrors(ExpectedErrorType.Any);
    }

    [Test]
    [UITimeout(50000)]
    public void Test_AggregateEditorUI_NoExtractableColumns()
    {
        //Create a Catalogue with an AggregateConfiguration that doesn't have any extractable columns yet
        var cata = WhenIHaveA<Catalogue>();
        var config = new AggregateConfiguration(Repository, cata, "My config");

        //these commands should be impossible
        var cmd = new ExecuteCommandAddNewAggregateGraph(ItemActivator, cata);
        Assert.Multiple(() =>
        {
            Assert.That(cmd.IsImpossible);
            Assert.That(cmd.ReasonCommandImpossible, Does.Contain("no extractable columns"));
        });

        //and if the broken config is activated
        var ui = AndLaunch<AggregateEditorUI>(config);

        //it should not launch and instead show the following message
        var killed = ItemActivator.Results.KilledForms.Single();
        Assert.Multiple(() =>
        {
            Assert.That(killed.Key, Is.EqualTo(ui.ParentForm));
            Assert.That(killed.Value.Message, Does.Contain("no extractable columns"));
        });
    }

    /// <summary>
    /// The Clear button of the pivot saves the aggregate without its pivot and publishes the change
    /// </summary>
    [Test]
    [UITimeout(50000)]
    public void Test_AggregateEditorUI_ClearPivot_SavesAndPublishes()
    {
        var config = GetAggregateConfigurationWithAPivot(false, out var pivot);

        var ui = AndLaunch<AggregateEditorUI>(config);
        Assert.That(ui.ddPivotDimension.SelectedItem, Is.EqualTo(pivot));

        var publishes = 0;
        ItemActivator.RefreshBus.BeforePublish += (_, _) => publishes++;

        Click(GetControl<Button>("btnClearPivotDimension"));

        Assert.Multiple(() =>
        {
            Assert.That(ui.ddPivotDimension.SelectedItem, Is.Null);
            Assert.That(config.PivotOnDimensionID, Is.Null);
            Assert.That(config.HasLocalChanges().Evaluation, Is.EqualTo(ChangeDescription.NoChanges),
                "The aggregate should have been saved without its pivot");
            Assert.That(publishes, Is.EqualTo(1), "The change should have been published");
        });
    }

    /// <summary>
    /// The Clear button of the axis deletes the axis and clears the pivot too.  It clears the pivot through the click
    /// handler of the pivot's own Clear button, which then leaves the refreshing to its caller: only this user
    /// interface is refreshed.
    /// </summary>
    [Test]
    [UITimeout(50000)]
    public void Test_AggregateEditorUI_ClearAxis_DeletesTheAxisAndClearsThePivot()
    {
        var config = GetAggregateConfigurationWithAPivot(true, out var pivot);
        var axis = config.GetAxisIfAny();

        var ui = AndLaunch<AggregateEditorUI>(config);
        Assert.Multiple(() =>
        {
            Assert.That(ui.ddPivotDimension.SelectedItem, Is.EqualTo(pivot));
            Assert.That(ui.ddAxisDimension.SelectedItem, Is.EqualTo(axis.AggregateDimension));
        });

        var publishes = 0;
        ItemActivator.RefreshBus.BeforePublish += (_, _) => publishes++;

        Click(GetControl<Button>("btnClearAxis"));

        Assert.Multiple(() =>
        {
            Assert.That(axis.Exists(), Is.False, "The axis should have been deleted");
            Assert.That(ui.ddAxisDimension.SelectedItem, Is.Null);
            Assert.That(ui.ddPivotDimension.SelectedItem, Is.Null);
            Assert.That(config.PivotOnDimensionID, Is.Null);
            Assert.That(publishes, Is.Zero, "Only this user interface should have been refreshed");
        });
    }

    /// <summary>
    /// Returns an aggregate with a date dimension and one other dimension, which is its pivot
    /// </summary>
    /// <param name="withAxis">True to make the date dimension the aggregate's axis</param>
    /// <param name="pivot">The dimension the aggregate pivots on</param>
    private AggregateConfiguration GetAggregateConfigurationWithAPivot(bool withAxis, out AggregateDimension pivot)
    {
        var config = GetAggregateConfigurationWithNoDimensions(out var dateEi, out var otherEi);

        var dimDate = new AggregateDimension(Repository, dateEi, config);
        pivot = new AggregateDimension(Repository, otherEi, config);

        if (withAxis)
        {
            var axis = new AggregateContinuousDateAxis(Repository, dimDate)
            {
                AxisIncrement = AxisIncrement.Month
            };
            axis.SaveToDatabase();
        }

        config.PivotOnDimensionID = pivot.ID;
        config.SaveToDatabase();
        config.ClearAllInjections();

        return config;
    }

    private AggregateConfiguration GetAggregateConfigurationWithNoDimensions() =>
        GetAggregateConfigurationWithNoDimensions(out _, out _);

    private AggregateConfiguration GetAggregateConfigurationWithNoDimensions(out ExtractionInformation dateEi,
        out ExtractionInformation otherEi)
    {
        var config = WhenIHaveA(Repository, out dateEi, out otherEi);

        //remove any existing dimensions
        foreach (var d in config.AggregateDimensions)
            d.DeleteInDatabase();

        config.ClearAllInjections();
        return config;
    }
}