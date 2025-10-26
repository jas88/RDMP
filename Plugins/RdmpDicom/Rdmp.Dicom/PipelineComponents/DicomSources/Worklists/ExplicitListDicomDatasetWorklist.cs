// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using FellowOakDicom;

namespace Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;

/// <summary>
/// Worklist implementation that provides an explicit in-memory list of DicomDatasets for testing DicomDatasetCollectionSource
/// </summary>
public class ExplicitListDicomDatasetWorklist : IDicomDatasetWorklist
{
    private readonly DicomDataset[] _datasets;
    private readonly string _filenameToPretend;
    private int index = 0;
    private readonly Dictionary<string, string> _otherValuesToStoreInRow;


    public HashSet<DicomDataset> CorruptMessages = new();


    /// <summary>
    /// For testing <see cref="DicomDatasetCollectionSource"/> this will feed the source the datasets you specify and make it look like they came from the given
    /// filename (must not be null).  Optionally you can specify a dictionary of other values to have fed to the source e.g. "MessageGuid=102321380"
    /// </summary>
    /// <param name="datasets"></param>
    /// <param name="filenameToPretend"></param>
    /// <param name="otherValuesToStoreInRow"></param>
    public ExplicitListDicomDatasetWorklist(DicomDataset[] datasets, string filenameToPretend,Dictionary<string, string> otherValuesToStoreInRow = null)
    {
        _datasets = datasets;
        _filenameToPretend = filenameToPretend;
        _otherValuesToStoreInRow = otherValuesToStoreInRow;
    }

    public DicomDataset GetNextDatasetToProcess(out string filename, out Dictionary<string, string> otherValuesToStoreInRow)
    {
        otherValuesToStoreInRow = _otherValuesToStoreInRow;
        filename = _filenameToPretend;

        return index >= _datasets.Length ? null : _datasets[index++];
    }

    public void MarkCorrupt(DicomDataset ds)
    {
        CorruptMessages.Add(ds);
    }
}