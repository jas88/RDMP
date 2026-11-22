// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Windows.Forms;
using FAnsi.Discovery.QuerySyntax.Aggregation;
using NUnit.Framework;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;
using Rdmp.UI.AggregationUIs;

namespace Rdmp.UI.Tests.AggregationUIs;

/// <summary>
/// Tests the start and end date boxes of <see cref="AggregateContinuousDateAxisUI"/>: a date typed into either is
/// quoted and saved, and a blank box shows an error that only filling in that same box clears
/// </summary>
internal class AggregateContinuousDateAxisUITests : UITests
{
    private const string BlankError = "Field cannot be blank";

    [Test]
    [UITimeout(50000)]
    public void Test_AggregateContinuousDateAxisUI_BlankDateIsAnErrorUntilThatBoxIsFilledIn()
    {
        var axis = WhenIHaveA<AggregateContinuousDateAxis>();
        axis.AxisIncrement = AxisIncrement.Month;
        axis.SaveToDatabase();

        var ui = new AggregateContinuousDateAxisUI();
        AndLaunch(ui);
        ui.Dimension = axis.AggregateDimension;

        var start = GetControl<TextBox>("tbStartDate");
        var end = GetControl<TextBox>("tbEndDate");

        start.Text = "2001-02-03";
        AssertNoErrors(ExpectedErrorType.ErrorProvider);
        Assert.Multiple(() =>
        {
            Assert.That(start.Text, Is.EqualTo("'2001-02-03'"));
            Assert.That(axis.StartDate, Is.EqualTo("'2001-02-03'"));
            Assert.That(axis.HasLocalChanges().Evaluation, Is.EqualTo(ChangeDescription.NoChanges),
                "The start date should have been saved");
        });

        start.Text = "";
        AssertErrorWasShown(ExpectedErrorType.ErrorProvider, BlankError);

        // the error is on the start date, so it stays when the end date is filled in
        end.Text = "2010-11-12";
        AssertErrorWasShown(ExpectedErrorType.ErrorProvider, BlankError);
        Assert.That(axis.EndDate, Is.EqualTo("'2010-11-12'"));

        start.Text = "2004-05-06";
        AssertNoErrors(ExpectedErrorType.ErrorProvider);
        Assert.That(axis.StartDate, Is.EqualTo("'2004-05-06'"));
    }
}
