// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using System;
using System.Linq;

namespace Rdmp.Core.Tests.CommandExecution;

internal class ExecuteCommandDeleteDatasetTest: CommandCliTests
{

    [Test]
    public void TestDeleteExistingDataset()
    {
        var cmd = new ExecuteCommandCreateDataset(GetMockActivator(), "dataset");
        Assert.DoesNotThrow(() => cmd.Execute());
        Assert.That(GetMockActivator().RepositoryLocator.CatalogueRepository.GetAllObjects<Rdmp.Core.Curation.Data.Dataset>(), Has.Length.EqualTo(1));
        var founddataset = GetMockActivator().RepositoryLocator.CatalogueRepository.GetAllObjects<Core.Curation.Data.Dataset>().First(static ds => ds.Name == "dataset");
        var delCmd = new ExecuteCommandDeleteDataset(GetMockActivator(), founddataset);
        Assert.DoesNotThrow(() => delCmd.Execute());
        Assert.That(GetMockActivator().RepositoryLocator.CatalogueRepository.GetAllObjects<Rdmp.Core.Curation.Data.Dataset>(), Is.Empty);
    }

    [Test]
    public void TestDeleteNonExistantDataset()
    {
        Assert.That(GetMockActivator().RepositoryLocator.CatalogueRepository.GetAllObjects<Rdmp.Core.Curation.Data.Dataset>(), Is.Empty);
        var founddataset = new Core.Curation.Data.Dataset();
        var delCmd = new ExecuteCommandDeleteDataset(GetMockActivator(), founddataset);
        Assert.Throws<NullReferenceException>(() => delCmd.Execute());
        Assert.That(GetMockActivator().RepositoryLocator.CatalogueRepository.GetAllObjects<Rdmp.Core.Curation.Data.Dataset>(), Is.Empty);
    }
}   