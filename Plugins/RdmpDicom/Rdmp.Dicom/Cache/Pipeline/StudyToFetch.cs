// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

namespace Rdmp.Dicom.Cache.Pipeline;

/// <summary>
/// Represents a DICOM study to be fetched from PACS, tracking the study UID and retry count for failed fetch attempts
/// </summary>
public class StudyToFetch
{
    /// <summary>
    /// The unique UID of the study that is to be fetched
    /// </summary>
    public string StudyUid {get; }

    /// <summary>
    /// The number of times this study has been reported as unavailable or errors have manifested downloading it during
    /// a single fetching session
    /// </summary>
    public int RetryCount {get;set;}

    public StudyToFetch(string studyUid)
    {
        StudyUid = studyUid;
    }

    public override bool Equals(object obj) => obj?.GetType()==typeof(StudyToFetch) && ((StudyToFetch)obj).StudyUid==StudyUid;

    public override int GetHashCode() => System.HashCode.Combine(StudyUid);
}