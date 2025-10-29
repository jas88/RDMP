// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.TestsAndSetup.ServicePropogation;
using System;

namespace Rdmp.UI.SimpleDialogs.Datasets;

public partial class CreateNewDatasetUI : RDMPForm
{
    private readonly IActivateItems _activator;
    public CreateNewDatasetUI(IActivateItems activator, ExecuteCommandCreateNewDatasetUI command) : base(activator)
    {
        _activator = activator;
        InitializeComponent();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void btnCreate_Click(object sender, EventArgs e)
    {
        var cmd = new ExecuteCommandCreateDataset(_activator,tbName.Text,tbDOI.Text,tbSource.Text);
        cmd.Execute();
        Close();
    }


}