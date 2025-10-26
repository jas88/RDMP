// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

public class SciStoreXmlRepository : ISciStoreRepository<SciStoreReport>
{
    private readonly string _rootPath;

    public SciStoreXmlRepository(string rootPath)
    {
        _rootPath = rootPath;
    }

    public IEnumerable<SciStoreReport> ReadAll()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<SciStoreReport> ReadSince(DateTime day)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IEnumerable<SciStoreReport>> ChunkedReadFromDateRange(DateTime start, DateTime end, IDataLoadEventListener job)
    {
        throw new NotImplementedException();
    }

    public void Create(IEnumerable<SciStoreReport> reports, IDataLoadEventListener listener)
    {
        var serialiser = new XmlSerializer(typeof (SciStoreReport));
        foreach (var report in reports)
        {
            if (report == null)
                throw new Exception("Could not cast SciStoreReport object");

            try
            {
                var path = $"{_rootPath}{Path.DirectorySeparatorChar}report-{report.Header.LabNumber}.xml";
                using var stream = new StreamWriter(path, false);
                serialiser.Serialize(stream, report);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not open stream to write CombinedReportData files: {e}");
            }
        }
    }
}