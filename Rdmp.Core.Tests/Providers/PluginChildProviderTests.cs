// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.Providers;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Tests.Common;

namespace Rdmp.Core.Tests.Providers;

internal class PluginChildProviderTests : UnitTests
{
    private sealed class ChildOfAllANOTablesNode : IChildProvider
    {
        public readonly object Child = new();

        public object[] GetChildren(object model) => model is Rdmp.Core.Providers.Nodes.AllANOTablesNode ? [Child] : null;
    }

    [Test]
    public void GetPluginChildren_OfARootObject_AreAddedBelowIt()
    {
        var plugin = new ChildOfAllANOTablesNode();
        var errors = new ToMemoryCheckNotifier();

        // AllANOTablesNode is at the root of its tree so it has no descendancy of its own
        var provider = new DataExportChildProvider(RepositoryLocator, [plugin], errors, null);

        Assert.Multiple(() =>
        {
            Assert.That(errors.GetWorst(), Is.LessThan(CheckResult.Fail));
            Assert.That(provider.GetChildren(provider.AllANOTablesNode), Does.Contain(plugin.Child));
            Assert.That(provider.GetDescendancyListIfAnyFor(plugin.Child)?.Parents,
                Is.EqualTo(new object[] { provider.AllANOTablesNode }));
        });
    }
}
