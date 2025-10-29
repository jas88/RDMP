// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using FellowOakDicom;

namespace Rdmp.Dicom.Extraction;

/// <summary>
/// Maps a private DICOM UID to an anonymized release UID for a specific project and UID type (Study, Series, SOP Instance, etc.)
/// </summary>
public class UIDMapping
{
    public string PrivateUID { get; set; }
    public string ReleaseUID { get; set; }
    public int ProjectNumber { get; set; }
    public UIDType UIDType { get; set; }
    public bool IsExternalReference { get; set; }

    public static Dictionary<DicomTag, UIDType> SupportedTags = new()
    {
        {DicomTag.SOPInstanceUID,UIDType.SOPInstanceUID},
        {DicomTag.SeriesInstanceUID,UIDType.SeriesInstanceUID},
        {DicomTag.StudyInstanceUID,UIDType.StudyInstanceUID},
        {DicomTag.FrameOfReferenceUID,UIDType.FrameOfReferenceUID},
        {DicomTag.MediaStorageSOPInstanceUID,UIDType.MediaStorageSOPInstanceUID}
    };

    public void SetUIDType(DicomTag tag)
    {
        if (SupportedTags.TryGetValue(tag, out var supportedTag))
            UIDType = supportedTag;
        else
            throw new InvalidOperationException(
                $"UIDMapping does not handle this tag type: {tag.DictionaryEntry.Keyword}");
    }
}