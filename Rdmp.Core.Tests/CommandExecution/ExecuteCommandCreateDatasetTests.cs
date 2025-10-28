// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using System.Linq;
using Rdmp.Core.CommandExecution;

namespace Rdmp.Core.Tests.CommandExecution;

public class ExecuteCommandCreateDatasetTests : CommandCliTests
{
    [Test]
    public void TestDatasetCreationOKParameters() {
        var cmd = new ExecuteCommandCreateDataset(GetMockActivator(),"dataset");
        Assert.DoesNotThrow(()=>cmd.Execute());
    }

    [Test]
    public void TestDatasetCreationNoParameters()
    {
        var cmd = new ExecuteCommandCreateDataset(GetMockActivator(), null);
        Assert.Throws<ImpossibleCommandException>(cmd.Execute);
    }

    [Test]
    public void TestDatasetCreationOKExtendedParameters()
    {
        var cmd = new ExecuteCommandCreateDataset(GetMockActivator(), "dataset2","somedoi","some source");
        Assert.DoesNotThrow(cmd.Execute);
        var founddataset = GetMockActivator().RepositoryLocator.CatalogueRepository.GetAllObjects<Core.Curation.Data.Dataset>().First(static ds => ds.Name == "dataset2" && ds.DigitalObjectIdentifier == "somedoi" && ds.Source == "some source");
        Assert.That(founddataset,Is.Not.Null);
    }
}
