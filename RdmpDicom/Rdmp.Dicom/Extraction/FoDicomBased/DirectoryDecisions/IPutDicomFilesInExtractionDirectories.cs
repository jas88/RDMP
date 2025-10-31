// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.IO;
using FellowOakDicom;

namespace Rdmp.Dicom.Extraction.FoDicomBased.DirectoryDecisions;

/// <summary>
/// Interface for strategies that determine the directory structure for extracted DICOM files (e.g., flat, by patient, by study/series)
/// </summary>
public interface IPutDicomFilesInExtractionDirectories
{
    /// <summary>
    /// If running in <see cref="FoDicomAnonymiser.MetadataOnly"/> mode (no access to underlying files) then
    /// return what path you WOULD use for outputting the image.  Note that depending on the tags in the data
    /// table being extracted some of these may be null (e.g. if SOPInstanceUID is not part of the extracted metadata).
    /// If this is required to calculate output path then return null;
    /// </summary>
    /// <param name="outputDirectory"></param>
    /// <param name="releaseIdentifier"></param>
    /// <param name="studyUid"></param>
    /// <param name="seriesUid"></param>
    /// <param name="sopUid"></param>
    /// <returns></returns>
    string PredictOutputPath(DirectoryInfo outputDirectory, string releaseIdentifier, string studyUid, string seriesUid, string sopUid);
    string WriteOutDataset(DirectoryInfo outputDirectory, string releaseIdentifier, DicomDataset dicomDataset);
}