// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Dicom.ExternalApis;
using Terminal.Gui;

namespace Rdmp.Dicom;

public class RdmpDicomConsoleUserInterface : PluginUserInterface
{
    private readonly IBasicActivateItems _activator;

    public RdmpDicomConsoleUserInterface(IBasicActivateItems itemActivator) : base(itemActivator)
    {
        _activator = itemActivator;
    }

    public override bool CustomActivate(IMapsDirectlyToDatabaseTable o)
    {
        // if it's not a terminal gui don't run a terminal gui UI!
        if(_activator?.GetType().Name.Equals("ConsoleGuiActivator")!=true)
        {
            return false;
        }

        if (o is not AggregateConfiguration ac) return base.CustomActivate(o);
        var api = new SemEHRApiCaller();

        if (!api.ShouldRun(ac)) return base.CustomActivate(o);
        var ui = new SemEHRConsoleUI(_activator, api, ac);
        Application.Run(ui);
        return true;

    }
}