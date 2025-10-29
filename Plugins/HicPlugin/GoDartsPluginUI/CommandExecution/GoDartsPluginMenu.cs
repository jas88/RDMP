// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using GoDartsPluginUI.CommandExecution.AtomicCommands;
using Rdmp.Core;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Providers.Nodes;
using Rdmp.UI.ItemActivation;
using System.Collections.Generic;

namespace GoDartsPluginUI.CommandExecution;

public class GoDartsPluginMenu : PluginUserInterface
{
    readonly IActivateItems activator;

    public GoDartsPluginMenu(IBasicActivateItems itemActivator) : base(itemActivator)
    {
        activator = itemActivator as IActivateItems;
    }

    public override IEnumerable<IAtomicCommand> GetAdditionalRightClickMenuItems(object o)
    {
        if(activator != null && o is AllServersNode)
        {
            return new[] { new ExecuteCommandSetupGoFusionFromDatabase(activator) };
        }

        return base.GetAdditionalRightClickMenuItems(o);
    }
}