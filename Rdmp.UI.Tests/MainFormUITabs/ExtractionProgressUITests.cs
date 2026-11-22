// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Windows.Forms;
using NUnit.Framework;
using Rdmp.Core.DataExport.Data;
using Rdmp.UI.MainFormUITabs;

namespace Rdmp.UI.Tests.MainFormUITabs;

/// <summary>
/// Tests that the start, end and progress date boxes of <see cref="ExtractionProgressUI"/>, which share a text changed
/// handler, each change their own date and leave the other two alone
/// </summary>
internal class ExtractionProgressUITests : UITests
{
    private static readonly DateTime Start = new(2001, 2, 3);
    private static readonly DateTime End = new(2005, 6, 7);
    private static readonly DateTime Progress = new(2003, 4, 5);

    [Test]
    [UITimeout(50000)]
    public void Test_ExtractionProgressUI_EachDateBoxSetsOnlyItsOwnDate()
    {
        var progress = WhenIHaveA<ExtractionProgress>();
        progress.ProgressDate = Progress;
        progress.SaveToDatabase();

        AndLaunch<ExtractionProgressUI>(progress);
        AssertDatesAre(progress, null, null, Progress, "when launched");

        GetControl<TextBox>("tbStartDate").Text = "2001-02-03";
        AssertDatesAre(progress, Start, null, Progress, "after typing a start date");

        GetControl<TextBox>("tbEndDate").Text = "2005-06-07";
        AssertDatesAre(progress, Start, End, Progress, "after typing an end date");

        // the progress date box is read only, the user can only clear it
        Click(GetControl<Button>("btnResetProgress"));
        AssertDatesAre(progress, Start, End, null, "after resetting the progress");
    }

    private static void AssertDatesAre(ExtractionProgress progress, DateTime? start, DateTime? end,
        DateTime? progressDate, string when)
    {
        Assert.Multiple(() =>
        {
            Assert.That(progress.StartDate, Is.EqualTo(start), $"StartDate {when}");
            Assert.That(progress.EndDate, Is.EqualTo(end), $"EndDate {when}");
            Assert.That(progress.ProgressDate, Is.EqualTo(progressDate), $"ProgressDate {when}");
        });
    }
}
