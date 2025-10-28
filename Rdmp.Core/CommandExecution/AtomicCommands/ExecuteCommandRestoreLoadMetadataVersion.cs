// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data;
using System;
using System.Linq;

namespace Rdmp.Core.CommandExecution.AtomicCommands;

public class ExecuteCommandRestoreLoadMetadataVersion : BasicCommandExecution
{
    private readonly LoadMetadata _loadMetadata;
    private readonly IBasicActivateItems _activator;
    public ExecuteCommandRestoreLoadMetadataVersion(IBasicActivateItems activator, [DemandsInitialization("The LoadMetadata to version")] LoadMetadata loadMetadata)
    {

        _loadMetadata = loadMetadata;
        _activator = activator;
    }

    public override void Execute()
    {
        if (_activator.IsInteractive && !_activator.YesNo("Replace root Load Metadata with this configuration?", "Restore Load Metadata Version")) return;
        base.Execute();
        if (_loadMetadata.RootLoadMetadata_ID is null)
        {
            throw new Exception("Must Use a versioned LoadMetadata to create Version");
        }
        LoadMetadata lmd = (LoadMetadata)_activator.RepositoryLocator.CatalogueRepository.GetObjectByID(typeof(LoadMetadata), (int)_loadMetadata.RootLoadMetadata_ID) ?? throw new Exception("Could not find root load metadata");
        foreach (ProcessTask task in lmd.ProcessTasks.Cast<ProcessTask>())
        {
            task.DeleteInDatabase();
        }
        foreach (ProcessTask task in _loadMetadata.ProcessTasks.Cast<ProcessTask>())
        {
            task.Clone(lmd);
        }
        lmd.SaveToDatabase();
        _activator.Publish(lmd);
    }
}
