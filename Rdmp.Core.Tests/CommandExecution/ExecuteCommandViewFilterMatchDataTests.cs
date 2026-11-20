// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.DataViewing;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Tests.Common;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests which column <see cref="ExecuteCommandViewFilterMatchData"/> shows data for, without a database.  The user is
/// asked to pick one of the Catalogue's columns only when the filter is not tied to a column.
/// </summary>
internal class ExecuteCommandViewFilterMatchDataTests : UnitTests
{
    [Test]
    public void Execute_FilterNotTiedToAColumn_ShowsTheColumnTheUserPicks()
    {
        var filter = WhenIHaveAnAggregateFilter(out var dateCol, out var otherCol);
        var user = new FakeUser(otherCol);

        Execute(new ExecuteCommandViewFilterMatchData(user.Activator, filter, ViewType.Aggregate));

        Assert.That(user.Prompts, Has.Count.EqualTo(1), "The user should be asked for a column once");
        var (args, offered) = user.Prompts[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(offered, Is.EquivalentTo(new[] { dateCol, otherCol }));
            Assert.That(args.InitialSearchText, Is.Null.Or.Empty);
        }

        AssertShowsExtractOf(user.Shown, otherCol, ViewType.Aggregate, filter);
    }

    [Test]
    public void Execute_UserCancelsTheColumnPrompt_ShowsNothing()
    {
        var filter = WhenIHaveAnAggregateFilter(out _, out _);
        var user = new FakeUser(null);

        Execute(new ExecuteCommandViewFilterMatchData(user.Activator, filter));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Prompts, Has.Count.EqualTo(1), "The user should be asked for a column once");
            Assert.That(user.Shown, Is.Empty);
        }
    }

    [Test]
    public void Execute_FilterTiedToAColumn_ShowsThatColumnWithoutAsking()
    {
        var filter = WhenIHaveAnAggregateFilter(out var dateCol, out _);
        filter.AssociatedColumnInfo_ID = dateCol.ID;
        filter.SaveToDatabase();

        // Cancels if asked, so asking would also show nothing
        var user = new FakeUser(null);

        Execute(new ExecuteCommandViewFilterMatchData(user.Activator, filter));

        Assert.That(user.Prompts, Is.Empty, "The user should not be asked for a column");
        AssertShowsExtractOf(user.Shown, dateCol, ViewType.TOP_100, filter);
    }

    /// <summary>
    /// Creates an <see cref="AggregateFilter"/> on a graph of a Catalogue with two columns.  The filter is not tied to
    /// either column.
    /// </summary>
    private AggregateFilter WhenIHaveAnAggregateFilter(out ColumnInfo dateCol, out ColumnInfo otherCol)
    {
        var graph = WhenIHaveA(Repository, out var dateEi, out var otherEi);
        var container = new AggregateFilterContainer(Repository, FilterContainerOperation.AND);
        graph.RootFilterContainer_ID = container.ID;
        graph.SaveToDatabase();

        dateCol = dateEi.ColumnInfo;
        otherCol = otherEi.ColumnInfo;
        return new AggregateFilter(Repository, "My Filter", container);
    }

    private static void Execute(ExecuteCommandViewFilterMatchData cmd)
    {
        Assert.That(cmd.IsImpossible, Is.False, cmd.ReasonCommandImpossible);
        cmd.Execute();
    }

    /// <summary>
    /// Asserts that the command showed one extract of <paramref name="column"/>, filtered by <paramref name="filter"/>.
    /// </summary>
    private static void AssertShowsExtractOf(List<IViewSQLAndResultsCollection> shown, ColumnInfo column,
        ViewType viewType, IFilter filter)
    {
        Assert.That(shown, Has.Count.EqualTo(1));
        Assert.That(shown[0], Is.TypeOf<ViewColumnExtractCollection>());

        var extract = (ViewColumnExtractCollection)shown[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(extract.ColumnInfo, Is.EqualTo(column));
            Assert.That(extract.ViewType, Is.EqualTo(viewType));
            Assert.That(extract.DatabaseObjects, Does.Contain(filter));
        }
    }

    /// <summary>
    /// Stands in for the user.  Answers every column prompt with the column it was given, or cancels the prompt when
    /// that is null, and records the prompts and the data it is shown.
    /// </summary>
    private sealed class FakeUser
    {
        public IBasicActivateItems Activator { get; } = Substitute.For<IBasicActivateItems>();

        public List<(DialogArgs Args, IMapsDirectlyToDatabaseTable[] Offered)> Prompts { get; } = new();

        public List<IViewSQLAndResultsCollection> Shown { get; } = new();

        public FakeUser(ColumnInfo answer)
        {
            Activator.SelectOne(Arg.Any<DialogArgs>(), Arg.Any<IMapsDirectlyToDatabaseTable[]>()).Returns(call =>
            {
                Prompts.Add((call.ArgAt<DialogArgs>(0), call.ArgAt<IMapsDirectlyToDatabaseTable[]>(1)));
                return answer;
            });
            Activator.When(static a => a.ShowData(Arg.Any<IViewSQLAndResultsCollection>()))
                .Do(call => Shown.Add(call.Arg<IViewSQLAndResultsCollection>()));
        }
    }
}
