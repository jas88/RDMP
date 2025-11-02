// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Dicom.PipelineComponents;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.ItemActivation;

namespace Rdmp.Dicom.UI.CommandExecution.AtomicCommands;

class ExecuteCommandReviewIsolations: BasicUICommandExecution
{
    private readonly IsolationReview _reviewer;

    public ExecuteCommandReviewIsolations(IActivateItems activator, ProcessTask processTask) : base(activator)
    {
        _reviewer = new(processTask);

        if (_reviewer.Error != null)
            SetImpossible(_reviewer.Error);

    }

    public override void Execute()
    {
        base.Execute();

        var ui = new IsolationTableUI(_reviewer);
        ui.Show();
    }
}