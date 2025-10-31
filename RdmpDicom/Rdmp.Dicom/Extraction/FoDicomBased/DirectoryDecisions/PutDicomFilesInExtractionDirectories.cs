// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.IO;
using FellowOakDicom;

namespace Rdmp.Dicom.Extraction.FoDicomBased.DirectoryDecisions;

public abstract class PutDicomFilesInExtractionDirectories : IPutDicomFilesInExtractionDirectories
{


    public PutDicomFilesInExtractionDirectories() : base() { }

    public string WriteOutDataset(DirectoryInfo outputDirectory, string releaseIdentifier, DicomDataset dicomDataset)
    {
        if(dicomDataset == null)
            throw new ArgumentNullException(nameof(dicomDataset));

        if(!outputDirectory.Exists)
            outputDirectory.Create();

        return WriteOutDatasetImpl(outputDirectory, releaseIdentifier, dicomDataset);
    }

    protected abstract string WriteOutDatasetImpl(DirectoryInfo outputDirectory, string releaseIdentifier,DicomDataset dicomDataset);

    protected DirectoryInfo SubDirectoryCreate(DirectoryInfo parent, string child)
    {
        var childDir = new DirectoryInfo(Path.Combine(parent.FullName, child));
        //If the directory already exists, this method does nothing.
        childDir.Create();
        return childDir;
    }

    protected string SaveDicomData(DirectoryInfo outputDirectory,DicomDataset dicomDataset)
    {
        var path = Path.Combine(outputDirectory.FullName, dicomDataset.GetValue<string>(DicomTag.SOPInstanceUID, 0));

        if(!path.EndsWith(".dcm"))
        {
            path += ".dcm";
        }

        var outPath = new FileInfo(path);
        new DicomFile(dicomDataset).Save(outPath.FullName);
        return outPath.FullName;
    }

    public virtual string PredictOutputPath(DirectoryInfo outputDirectory, string releaseIdentifier, string studyUid, string seriesUid, string sopUid)
    {
        if (string.IsNullOrWhiteSpace(sopUid))
            return null;

        var path = Path.Combine(outputDirectory.FullName, sopUid);

        if (!path.EndsWith(".dcm"))
        {
            path += ".dcm";
        }

        return new FileInfo(path).FullName;
    }
}