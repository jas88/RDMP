// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Icons.IconProvision;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using Rdmp.UI.ItemActivation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Rdmp.UI.CommandExecution.AtomicCommands;

public sealed class ExecuteCommandDeleteDatasetUI : BasicUICommandExecution
{
    private readonly IActivateItems _activateItems;
    private readonly Dataset _dataset;

    public ExecuteCommandDeleteDatasetUI(IActivateItems activator, Dataset dataset) : base(activator)
    {
        _dataset = dataset;
        _activateItems = activator;
    }

    public override string GetCommandHelp() =>
       "Delete this dataset and remove all links to it within RDMP";

    public override void Execute()
    {
        base.Execute();
        var confirmDelete = YesNo( $"Are you sure you want to delete the dataset \"{_dataset.Name}\"?", $"Delete Dataset: {_dataset.Name}");
        if (!confirmDelete) return;

        var cmd = new Core.CommandExecution.AtomicCommands.ExecuteCommandDeleteDataset(_activateItems, _dataset);
        cmd.Execute();
        _activateItems.RefreshBus.Publish(this, new Refreshing.RefreshObjectEventArgs(_dataset));
    }


    public override Image<Rgba32> GetImage(IIconProvider iconProvider) =>
        iconProvider.GetImage(RDMPConcept.Dataset, OverlayKind.Delete);
}
