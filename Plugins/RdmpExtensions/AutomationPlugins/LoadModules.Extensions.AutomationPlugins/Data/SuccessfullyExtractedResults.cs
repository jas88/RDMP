// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Startup;

namespace LoadModules.Extensions.AutomationPlugins.Data;

public class SuccessfullyExtractedResults : DatabaseEntity
{
    #region Database Properties

    private string _sQL;
    private int _extractableDataSet_ID;
    private int _automateExtraction_ID;

    public string SQL
    {
        get => _sQL;
        set => SetField(ref _sQL, value);
    }
    public int ExtractableDataSet_ID
    {
        get => _extractableDataSet_ID;
        set => SetField(ref _extractableDataSet_ID, value);
    }
    public int AutomateExtraction_ID
    {
        get => _automateExtraction_ID;
        set => SetField(ref _automateExtraction_ID, value);
    }
    #endregion

    public SuccessfullyExtractedResults(PluginRepository repository, string sql, AutomateExtraction parent, IExtractableDataSet dataset)
    {
        repository.InsertAndHydrate(this, new Dictionary<string, object>
        {
            {"SQL",sql},
            {"ExtractableDataSet_ID",dataset.ID},
            {"AutomateExtraction_ID",parent.ID}
        });

        if (ID == 0 || Repository != repository)
            throw new ArgumentException("Repository failed to properly hydrate this class");
    }
    public SuccessfullyExtractedResults(PluginRepository repository, DbDataReader r)
        : base(repository, r)
    {
        SQL = r["SQL"].ToString();
        ExtractableDataSet_ID = Convert.ToInt32(r["ExtractableDataSet_ID"]);
        AutomateExtraction_ID = Convert.ToInt32(r["AutomateExtraction_ID"]);
    }
}