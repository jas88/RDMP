// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using Rdmp.Core.Caching.Requests;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;

namespace SCIStorePlugin.Cache.Pipeline;

public class SCIStoreCacheChunk : ICacheChunk
{
    public ICacheFetchRequest Request { get; private set; }

    public CombinedReportData[] CombinedReports { get; private set; } // for the time being as there is a repository for writing these out as XML
    public DateTime FetchDate { get; private set; }
    public HealthBoard HealthBoard { get; set; }
    public Discipline Discipline { get; set; }
    public DownloadRequestFailedException DownloadRequestFailedException { get; set; }

    public SCIStoreCacheChunk(CombinedReportData[] combinedReports, DateTime fetchDate, ICacheFetchRequest request)
    {
        CombinedReports = combinedReports;
        FetchDate = fetchDate;
        Request = request;
    }


}