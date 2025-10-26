// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using LoadModules.Extensions.AutomationPlugins.Data;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;

namespace LoadModules.Extensions.AutomationPlugins;

public class ExecuteCommandCreateNewAutomateExtractionSchedule : BasicAutomationCommandExecution
{
    public IProject Project { get; }

    public ExecuteCommandCreateNewAutomateExtractionSchedule(IBasicActivateItems activator,IProject project) :base(activator)
    {
        // if base class already errored out (e.g. no automation setup)
        if(IsImpossible)
        {
            return;
        }

        var existing = AutomationRepository.GetAllObjects<AutomateExtractionSchedule>();

        if(existing.Any(s=>s.Project_ID == project.ID))
        {
            SetImpossible($"Project already has an {nameof(AutomateExtractionSchedule)}");
            return;
        }

        Project = project;
    }

    public override Image<Rgba32> GetImage(IIconProvider iconProvider)
    {
        return iconProvider.GetImage(typeof(AutomateExtractionSchedule), OverlayKind.Add);
    }
    public override void Execute()
    {
        base.Execute();

        var schedule = new AutomateExtractionSchedule(AutomationRepository, Project);
        Publish(Project);
        Emphasise(schedule);
    }

}