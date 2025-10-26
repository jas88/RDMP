// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.DataExport.DataExtraction.Pipeline.Sources;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;

public class BaselineHackerExecuteDatasetExtractionSource : ExecuteDatasetExtractionSource
{
    public override string HackExtractionSQL(string sql, IDataLoadEventListener listener)
    {
        var finder = new AutomateExtractionRepositoryFinder(new RepositoryProvider(Request.DataExportRepository));
        var repository = (AutomateExtractionRepository)finder.GetRepositoryIfAny() ?? throw new Exception(
            "AutomateExtractionRepositoryFinder returned null, are you missing an AutomationPlugins database");
        var hacker = new DeltaHacker(repository,Request);

        //hacking allowed?
        if (hacker.ExecuteHackIfAllowed(listener, out var hackSql) != BaselineHackEvaluation.Allowed) return sql;
        var newSql = sql + hackSql;
        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,
            $"Full Hacked Query is now:{Environment.NewLine}{newSql}"));

        return newSql;
    }
}