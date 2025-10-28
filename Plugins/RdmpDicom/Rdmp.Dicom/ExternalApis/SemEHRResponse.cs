// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.Collections.Generic;

namespace Rdmp.Dicom.ExternalApis;

class SemEHRResponse
{
    public bool success { get; set; }
    public int num_results { get; set; }
    public string message { get; set; } = "Message not set.";

    //public IList<SemneHDRReponseResult> results { get; set; }

    public IList<string> results { get; set; }

    /*public List<string> GetResultSopUids()
    {
        return (results.Select(t => t.sop_uid).ToList());
    }

    public List<string> GetResultStudyUids()
    {
        return (results.Select(t => t.study_uid).ToList());
    }

    public List<string> GetResultseriesUids()
    {
        return (results.Select(t => t.series_uid).ToList());
    }*/
}

/*class SemneHDRReponseResult
{
    public string sop_uid { get; set; }
    public string study_uid { get; set; }
    public string series_uid { get; set; }
}*/