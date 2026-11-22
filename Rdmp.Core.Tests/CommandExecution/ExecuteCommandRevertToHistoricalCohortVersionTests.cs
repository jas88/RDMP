// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests for what <see cref="ExecuteCommandRevertToHistoricalCohortVersion"/> does with what the configuration had before
/// it was reverted
/// </summary>
internal class ExecuteCommandRevertToHistoricalCohortVersionTests : CommandCliTests
{
    [Test]
    public void Execute_ForAVersionSavedBeforeTheConfigurationHadARootContainer_LeavesItWithNone()
    {
        var cic = WhenIHaveA<CohortIdentificationConfiguration>();
        Assert.That(cic.RootCohortAggregateContainer_ID, Is.Null);
        var version = WhenIHaveAVersionOf(cic);

        // opening the configuration in the cohort builder gives it a root container, to which cohort sets are added
        cic.CreateRootContainerIfNotExists();
        var root = cic.RootCohortAggregateContainer;
        var cohortSet = WhenIHaveA<AggregateConfiguration>();
        root.AddChild(cohortSet, 1);

        new ExecuteCommandRevertToHistoricalCohortVersion(GetActivator(), cic, version).Execute();

        Assert.Multiple(() =>
        {
            Assert.That(cic.RootCohortAggregateContainer_ID, Is.Null);
            Assert.That(root.Exists(), Is.False);

            // the cohort set is left as an orphan, like that of any container that is deleted
            Assert.That(cohortSet.Exists());
            Assert.That(cohortSet.GetCohortAggregateContainerIfAny(), Is.Null);
        });
    }

    /// <summary>
    /// Saves the current state of <paramref name="cic"/> as a version of it, and returns the version
    /// </summary>
    private CohortIdentificationConfiguration WhenIHaveAVersionOf(CohortIdentificationConfiguration cic)
    {
        var versionsBefore = cic.GetVersions();

        new ExecuteCommandCreateVersionOfCohortConfiguration(GetActivator(), cic).Execute();

        return cic.GetVersions().Except(versionsBefore).Single();
    }
}
