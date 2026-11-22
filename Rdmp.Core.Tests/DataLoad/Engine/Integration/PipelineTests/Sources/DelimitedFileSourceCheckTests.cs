// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace Rdmp.Core.Tests.DataLoad.Engine.Integration.PipelineTests.Sources;

/// <summary>
/// Tests for <see cref="DelimitedFlatFileDataFlowSource.Check"/>
/// </summary>
[Category("Unit")]
public class DelimitedFileSourceCheckTests
{
    [Test]
    public void Check_WithoutAFileToLoad_FailsTheCheck()
    {
        // PreInitialize, which gives the source its FlatFileToLoad, has not been called
        var source = new DelimitedFlatFileDataFlowSource { Separator = "," };
        var notifier = new ToMemoryCheckNotifier();

        Assert.DoesNotThrow(() => source.Check(notifier));
        Assert.That(notifier.Messages, Has.Some.Matches<CheckEventArgs>(static m =>
            m.Result == CheckResult.Fail && m.Message.Contains("_fileToLoad was not set")));
    }
}
