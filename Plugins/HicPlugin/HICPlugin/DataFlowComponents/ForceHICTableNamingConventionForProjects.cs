// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents;

[Description("Forces tables being loaded to match the hic regex ")]
public partial class ForceHICTableNamingConventionForProjects : IPluginDataFlowComponent<DataTable>, IPipelineRequirement<TableInfo>
{
    private static readonly Regex NamingConvention = TtPrefix();

    public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener job, GracefulCancellationToken cancellationToken)
    {
        return toProcess;
    }

    public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {

    }

    public void Abort(IDataLoadEventListener listener)
    {
            
    }

    public void PreInitialize(TableInfo target,IDataLoadEventListener listener)
    {
        if (!NamingConvention.IsMatch(target.GetRuntimeName()))
            listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Error,
                $"TableInfo {target} does not match hic regex for naming conventions of project/group data ({NamingConvention})"));
    }


    public void Check(ICheckNotifier notifier)
    {
            
    }

    [GeneratedRegex("tt_\\d*", RegexOptions.Compiled)]
    private static partial Regex TtPrefix();
}