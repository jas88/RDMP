// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

public class SciStoreFileRepository : IRepository<SciStoreReport>
{
    private readonly string _rootPath;
    private readonly SciStoreTableRecord _tableInfo;

    // TODO: tableInfo doesn't make too much sense in the context of a File Repository - fix by injecting the Sql maker
    public SciStoreFileRepository(string rootPath, SciStoreTableRecord tableInfo)
    {
        _rootPath = rootPath;
        _tableInfo = tableInfo;
    }

    public IEnumerable<SciStoreReport> ReadSince(DateTime day)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IEnumerable<SciStoreReport>> ChunkedReadFromDateRange(DateTime start, DateTime end,IDataLoadEventListener job)
    {
        throw new NotImplementedException();
    }

    public void Create(IEnumerable<SciStoreReport> reports, IDataLoadEventListener listener)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<SciStoreReport> ReadAll()
    {
        throw new NotImplementedException();
    }
}