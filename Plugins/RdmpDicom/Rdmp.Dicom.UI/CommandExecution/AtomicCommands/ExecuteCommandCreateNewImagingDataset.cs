// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core.Icons.IconProvision;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.ItemActivation;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Rdmp.Dicom.UI.CommandExecution.AtomicCommands;

public class ExecuteCommandCreateNewImagingDataset : BasicUICommandExecution
{
    public ExecuteCommandCreateNewImagingDataset(IActivateItems activator) : base(activator)
    {

    }

    public override string GetCommandName()
    {
        return "Create New Imaging Table";
    }

    public override Image<Rgba32> GetImage(IIconProvider iconProvider)
    {
        return iconProvider.GetImage(RDMPConcept.TableInfo,OverlayKind.Add);
    }

    public override void Execute()
    {
        new CreateNewImagingDatasetUI(Activator).ShowDialog();
    }
}