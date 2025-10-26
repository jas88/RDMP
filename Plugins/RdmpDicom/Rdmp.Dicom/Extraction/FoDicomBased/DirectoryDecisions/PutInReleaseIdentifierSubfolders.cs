// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.IO;
using FellowOakDicom;

namespace Rdmp.Dicom.Extraction.FoDicomBased.DirectoryDecisions;

public class PutInReleaseIdentifierSubfolders : PutDicomFilesInExtractionDirectories
{
    public override string PredictOutputPath(DirectoryInfo outputDirectory, string releaseIdentifier, string studyUid, string seriesUid, string sopUid)
    {
        if (string.IsNullOrWhiteSpace(releaseIdentifier))
            return null;

        return base.PredictOutputPath(
            new DirectoryInfo(Path.Combine(outputDirectory.FullName, releaseIdentifier)),
            releaseIdentifier, studyUid, seriesUid, sopUid);
    }

    protected override string WriteOutDatasetImpl(DirectoryInfo outputDirectory, string releaseIdentifier, DicomDataset dicomDataset)
    {
        var patientDir = SubDirectoryCreate(outputDirectory, releaseIdentifier);
        return SaveDicomData(patientDir, dicomDataset);
    }
}