// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Data;
using System.Diagnostics;
using FellowOakDicom;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;

namespace Rdmp.Dicom.PipelineComponents.DicomSources;

public class DicomDatasetCollectionSource : DicomSource, IPipelineRequirement<IDicomWorklist>
{
    private IDicomDatasetWorklist _datasetListWorklist;

    private const int BatchSize = 50000;

    public void PreInitialize(IDicomWorklist value, IDataLoadEventListener listener)
    {
        _datasetListWorklist = value as IDicomDatasetWorklist;

        if (_datasetListWorklist == null)
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"Expected IDicomWorklist to be of Type IDicomDatasetWorklist (but it was {value.GetType().Name}). Component will be skipped."));
    }

    protected override void MarkCorrupt(DicomDataset ds)
    {
        base.MarkCorrupt(ds);
        _datasetListWorklist.MarkCorrupt(ds);
    }

    public override DataTable GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        if(_datasetListWorklist == null)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Skipping component because _datasetListWorklist is null"));
            return null;
        }

        var sw = Stopwatch.StartNew();
        var currentBatch = BatchSize;
        var dt = GetDataTable();
        dt.BeginLoadData();
        var returned = false;
        try
        {
            while (currentBatch > 0 && _datasetListWorklist.GetNextDatasetToProcess(out var filename,out var otherValuesToStoreInRow) is { } ds)
            {
                ProcessDataset(filename, ds, dt, listener, otherValuesToStoreInRow);
                currentBatch--;
            }

            sw.Stop();
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"GetChunk cumulative total time is {sw.ElapsedMilliseconds}ms"));

            dt.EndLoadData();
            if (dt.Rows.Count > 0)
            {
                returned = true;
                return dt;
            }
        }
        finally
        {
            if (!returned)
                dt.Dispose();
        }
        return null;
    }

    public override DataTable TryGetPreview()
    {
        return GetDataTable();
    }
}