// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using System;

namespace LoadModules.Extensions.AutomationPlugins.Data.Repository;

public class AutomateExtractionRepository : PluginRepository
{
    public ICatalogueRepository CatalogueRepository { get; private set; }
    public IDataExportRepository DataExportRepository { get; private set; }

    public AutomateExtractionRepository(IRDMPPlatformRepositoryServiceLocator repositoryLocator, ExternalDatabaseServer server):base(server,null)
    {
        CatalogueRepository = repositoryLocator.CatalogueRepository;
        DataExportRepository = repositoryLocator.DataExportRepository;
    }

    protected override bool IsCompatibleType(Type type)
    {
        return typeof(DatabaseEntity).IsAssignableFrom(type);
    }
}