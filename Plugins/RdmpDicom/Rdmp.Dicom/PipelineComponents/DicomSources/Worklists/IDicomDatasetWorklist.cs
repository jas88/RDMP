// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using FellowOakDicom;

namespace Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;

public interface IDicomDatasetWorklist : IDicomWorklist
{
    /// <summary>
    /// Returns the next DicomDataset that should be processed.  Returns null if there are no more datasets to process.
    /// </summary>
    /// <param name="filename">The absolute or relative path to the file that is represented by the DicomDataset</param>
    /// <param name="otherValuesToStoreInRow">Key value collection of any other columns that should be populated with values
    /// (there should not include the names of any dicom tags in the key collection).  E.g. 'MessageGuid' would be acceptable but 'StudyDate' would not</param>
    /// <returns></returns>
    DicomDataset GetNextDatasetToProcess(out string filename, out Dictionary<string, string> otherValuesToStoreInRow);

    /// <summary>
    /// Marks the given dataset as corrupt / unloadable
    /// </summary>
    /// <param name="ds"></param>
    void MarkCorrupt(DicomDataset ds);
}