// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using System;
using System.Linq;

namespace LoadModules.Extensions.AutomationPlugins.Data.Repository;

/// <summary>
/// Locates and instantiates the AutomateExtractionRepository by finding the configured external database server created by the AutomateExtractionPluginPatcher.
/// </summary>
public class AutomateExtractionRepositoryFinder : PluginRepositoryFinder
{
    public static int Timeout = 5;

    public AutomateExtractionRepositoryFinder(IRDMPPlatformRepositoryServiceLocator repositoryLocator) : base(repositoryLocator)
    {

    }

    public override PluginRepository GetRepositoryIfAny()
    {
        if (RepositoryLocator.CatalogueRepository == null || RepositoryLocator.DataExportRepository == null)
            return null;

        var patcher = new AutomateExtractionPluginPatcher();

        var compatibleServers = RepositoryLocator.CatalogueRepository.GetAllObjects<ExternalDatabaseServer>()
            .Where(e => e.WasCreatedBy(patcher)).ToArray();

        return compatibleServers.Length switch
        {
            > 1 => throw new Exception(
                $"There are 2+ ExternalDatabaseServers of type '{patcher.Name}'.  This is not allowed, you must delete one.  The servers were called:{string.Join(",", compatibleServers.Select(s => s.ToString()))}"),
            0 => null,
            _ => new AutomateExtractionRepository(RepositoryLocator, compatibleServers[0])
        };
    }

    public override Type GetRepositoryType()
    {
        return typeof (AutomateExtractionRepository);
    }
}