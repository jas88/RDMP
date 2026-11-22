// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests for when <see cref="ExecuteCommandChangeExtractionCategory"/> asks the user for the category
/// </summary>
internal class ExecuteCommandChangeExtractionCategoryCliTests : CommandCliTests
{
    [Test]
    public void Execute_WithACategory_SetsItWithoutAsking()
    {
        var ei = WhenIHaveA<ExtractionInformation>();
        Assert.That(ei.ExtractionCategory, Is.Not.EqualTo(ExtractionCategory.Supplemental));
        var activator = GetActivatorChoosing(ExtractionCategory.Internal);

        new ExecuteCommandChangeExtractionCategory(activator, [ei], ExtractionCategory.Supplemental).Execute();

        Assert.That(ei.ExtractionCategory, Is.EqualTo(ExtractionCategory.Supplemental));
        activator.DidNotReceive().SelectValueType(Arg.Any<string>(), Arg.Any<Type>(), Arg.Any<object>(),
            out Arg.Any<object>());
    }

    [Test]
    public void Execute_WithoutACategory_SetsTheCategoryChosen()
    {
        var ei = WhenIHaveA<ExtractionInformation>();
        Assert.That(ei.ExtractionCategory, Is.Not.EqualTo(ExtractionCategory.Supplemental));

        new ExecuteCommandChangeExtractionCategory(GetActivatorChoosing(ExtractionCategory.Supplemental), [ei])
            .Execute();

        Assert.That(ei.ExtractionCategory, Is.EqualTo(ExtractionCategory.Supplemental));
    }

    [Test]
    public void Execute_WhenChoosingACategoryIsCancelled_LeavesTheCategoryUnchanged()
    {
        var ei = WhenIHaveA<ExtractionInformation>();
        var before = ei.ExtractionCategory;

        new ExecuteCommandChangeExtractionCategory(GetActivatorChoosing(null), [ei]).Execute();

        Assert.That(ei.ExtractionCategory, Is.EqualTo(before));
    }

    /// <summary>
    /// Returns an activator whose user chooses <paramref name="category"/> when asked for one, or cancels if it is null
    /// </summary>
    private IBasicActivateItems GetActivatorChoosing(ExtractionCategory? category)
    {
        var activator = Substitute.For<IBasicActivateItems>();
        activator.RepositoryLocator.Returns(RepositoryLocator);
        activator.SelectValueType(Arg.Any<string>(), typeof(ExtractionCategory), Arg.Any<object>(),
                out Arg.Any<object>())
            .Returns(call =>
            {
                call[3] = category;
                return category != null;
            });

        return activator;
    }
}
