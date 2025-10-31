// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.Data;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;

namespace Rdmp.Dicom.Attachers.Routing;

/// <summary>
/// Defines the input objects / and explicit destination for the pipeline which the user must create for use with an <see cref="AutoRoutingAttacher"/>.
/// </summary>
public sealed class AutoRoutingAttacherPipelineUseCase:PipelineUseCase
{
    public AutoRoutingAttacherPipelineUseCase(AutoRoutingAttacher attacher, IDicomWorklist worklist)
    {
        ExplicitDestination = attacher;

        AddInitializationObject(worklist);

        GenerateContext();
    }

    protected override IDataFlowPipelineContext GenerateContextImpl()
    {
        var context = new DataFlowPipelineContextFactory<DataTable>().Create(PipelineUsage.FixedDestination);
        context.MustHaveSource = typeof(IDataFlowSource<DataTable>);

        return context;
    }

    private AutoRoutingAttacherPipelineUseCase(AutoRoutingAttacher attacher)
        : base(new[] { typeof(IDicomWorklist), typeof(IDicomDatasetWorklist), typeof(IDicomFileWorklist) })
    {
        ExplicitDestination = attacher;
        GenerateContext();
    }

    public static AutoRoutingAttacherPipelineUseCase GetDesignTimeUseCase(AutoRoutingAttacher attacher)
    {
        return new AutoRoutingAttacherPipelineUseCase(attacher);
    }
}