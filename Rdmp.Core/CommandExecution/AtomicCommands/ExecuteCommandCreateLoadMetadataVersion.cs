// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data;
using System;

namespace Rdmp.Core.CommandExecution.AtomicCommands;

public class ExecuteCommandCreateLoadMetadataVersion: BasicCommandExecution
{
    private readonly LoadMetadata _loadMetadata;
    private readonly IBasicActivateItems _activator;
    public ExecuteCommandCreateLoadMetadataVersion(IBasicActivateItems activator,[DemandsInitialization("The LoadMetadata to version")] LoadMetadata loadMetadata)
    {

        _loadMetadata = loadMetadata;
        _activator = activator;
    }

    public override void Execute()
    {
        base.Execute();
        if(_loadMetadata.RootLoadMetadata_ID != null)
        {
            throw new Exception("Must Use Root LoadMetadata to create Version");
        }
        var lmd = _loadMetadata.SaveNewVersion();
        lmd.SaveToDatabase();
        _activator.Publish(lmd);
    }
}
