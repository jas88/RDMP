// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Startup;

namespace LoadModules.Extensions.AutomationPlugins.Data;

/// <summary>
/// Represents an extraction request that has been queued for future execution, tracking the requester, due date, and associated extraction configuration and pipeline.
/// </summary>
public class QueuedExtraction : DatabaseEntity
{
    #region Database Properties

    private int _extractionConfiguration_ID;
    private int _pipeline_ID;
    private DateTime _dueDate;
    private string _requester;
    private DateTime _requestDate;


    public int ExtractionConfiguration_ID
    {
        get => _extractionConfiguration_ID;
        set => SetField(ref _extractionConfiguration_ID, value);
    }
    public int Pipeline_ID
    {
        get => _pipeline_ID;
        set => SetField(ref _pipeline_ID, value);
    }
    public DateTime DueDate
    {
        get => _dueDate;
        set => SetField(ref _dueDate, value);
    }
    public string Requester
    {
        get => _requester;
        set => SetField(ref _requester, value);
    }
    public DateTime RequestDate
    {
        get => _requestDate;
        set => SetField(ref _requestDate, value);
    }
    #endregion

    #region Relationships
    [NoMappingToDatabase]
    public IExtractionConfiguration ExtractionConfiguration => ((AutomateExtractionRepository)Repository).DataExportRepository.GetObjectByID<ExtractionConfiguration>(ExtractionConfiguration_ID);

    [NoMappingToDatabase]
    public Pipeline Pipeline => ((AutomateExtractionRepository)Repository).CatalogueRepository.GetObjectByID<Pipeline>(Pipeline_ID);

    #endregion

    public QueuedExtraction(PluginRepository repository, ExtractionConfiguration configuration, IPipeline extractionPipeline, DateTime dueDate)
    {
        repository.InsertAndHydrate(this, new Dictionary<string, object>
        {
            {"ExtractionConfiguration_ID",configuration.ID},
            {"Pipeline_ID",extractionPipeline.ID},
            {"DueDate",dueDate},
            {"Requester",Environment.UserName}
        });

        if (ID == 0 || Repository != repository)
            throw new ArgumentException("Repository failed to properly hydrate this class");
    }
    public QueuedExtraction(PluginRepository repository, DbDataReader r)
        : base(repository, r)
    {
        ExtractionConfiguration_ID = Convert.ToInt32(r["ExtractionConfiguration_ID"]);
        Pipeline_ID = Convert.ToInt32(r["Pipeline_ID"]);
        DueDate = Convert.ToDateTime(r["DueDate"]);
        Requester = r["Requester"].ToString();
        RequestDate = Convert.ToDateTime(r["RequestDate"]);
    }

    public bool IsDue()
    {
        return DateTime.Now > DueDate;
    }
}