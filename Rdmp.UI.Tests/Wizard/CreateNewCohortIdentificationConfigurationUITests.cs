// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Windows.Forms;
using NUnit.Framework;
using Rdmp.UI.Wizard;

namespace Rdmp.UI.Tests.Wizard;

/// <summary>
/// Tests the tick boxes of the cohort wizard (<see cref="CreateNewCohortIdentificationConfigurationUI"/>) that add a
/// second inclusion dataset and the two exclusion datasets.  They share a checked changed handler.
/// </summary>
/// <remarks>No test here creates a Catalogue: the wizard must start with no dataset chosen</remarks>
internal class CreateNewCohortIdentificationConfigurationUITests : UITests
{
    [Test]
    [UITimeout(50000)]
    public void Test_CohortWizard_FirstExclusionTickBoxEnablesOnlyItsOwnDataset()
    {
        using var ui = LaunchWithWizardTicked();

        var firstExclusion = GetControl<CheckBox>("cbExclusion1");
        AssertEnabled(false, false, false, "before any box is ticked");

        Click(firstExclusion);
        Assert.That(firstExclusion.Checked, "The first exclusion box should stay ticked");
        AssertEnabled(true, false, false, "after ticking the first exclusion box");

        Click(firstExclusion);
        Assert.That(firstExclusion.Checked, Is.False, "The first exclusion box should untick");
        AssertEnabled(false, false, false, "after unticking the first exclusion box");
    }

    [Test]
    [UITimeout(50000)]
    public void Test_CohortWizard_SecondDatasetCannotBeTickedBeforeTheFirstIsChosen()
    {
        using var ui = LaunchWithWizardTicked();

        foreach (var tickBox in new[] { "cbExclusion2", "cbInclusion2" })
        {
            var second = GetControl<CheckBox>(tickBox);
            Click(second);

            Assert.That(second.Checked, Is.False,
                $"{tickBox} should untick itself until a first dataset is chosen");
            AssertEnabled(false, false, false, $"after trying to tick {tickBox}");
        }
    }

    /// <summary>
    /// Launches the wizard and ticks 'use wizard', which is what enables the tick boxes the tests click
    /// </summary>
    private CreateNewCohortIdentificationConfigurationUI LaunchWithWizardTicked()
    {
        var ui = new CreateNewCohortIdentificationConfigurationUI(ItemActivator);

        //pretend like we launched it
        LastUserInterfaceLaunched = ui;

        Click(GetControl<CheckBox>("cbUseWizard"));

        return ui;
    }

    private void AssertEnabled(bool firstExclusion, bool secondExclusion, bool secondInclusion, string when)
    {
        var exclusion1 = GetControl<SimpleCohortSetUI>("exclusionCriteria1");
        var exclusion2 = GetControl<SimpleCohortSetUI>("exclusionCriteria2");
        var inclusion2 = GetControl<SimpleCohortSetUI>("inclusionCriteria2");

        Assert.Multiple(() =>
        {
            Assert.That(exclusion1.Enabled, Is.EqualTo(firstExclusion), $"First exclusion dataset {when}");
            Assert.That(exclusion2.Enabled, Is.EqualTo(secondExclusion), $"Second exclusion dataset {when}");
            Assert.That(inclusion2.Enabled, Is.EqualTo(secondInclusion), $"Second inclusion dataset {when}");
        });
    }
}
