// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using FAnsi.Discovery;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Versioning;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.DataExtraction.Pipeline.Destinations;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Tests.Common;

namespace LoadModules.Extensions.AutomationPlugins.Tests;

public class TestsRequiringAnAutomationPluginRepository:DatabaseTests
{
    public AutomateExtractionRepository Repo;

    [SetUp]
    public void CreateAutomationDatabase()
    {

        Repo = CreateAutomationDatabaseStatic(DiscoveredServerICanCreateRandomDatabasesAndTablesOn,RepositoryLocator);
    }

    public static AutomateExtractionRepository CreateAutomationDatabaseStatic(DiscoveredServer discoveredServerICanCreateRandomDatabasesAndTablesOn, IRDMPPlatformRepositoryServiceLocator repositoryLocator)
    {
        var db = discoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase("TEST_AutomationPluginsTests");
        if (db.Exists())
            db.Drop();

        var patcher = new AutomateExtractionPluginPatcher();

        var executor = new MasterDatabaseScriptExecutor(db);
        executor.CreateAndPatchDatabase(patcher, new AcceptAllCheckNotifier());

        var server = new ExternalDatabaseServer(repositoryLocator.CatalogueRepository, "Automation Server", patcher)
        {
            Server = db.Server.Name,
            Database = db.GetRuntimeName()
        };
        server.SaveToDatabase();

        return new AutomateExtractionRepository(repositoryLocator, server);
    }

    public Pipeline GetValidExtractionPipeline()
    {
        return GetValidExtractionPipelineStatic(CatalogueRepository);
    }

    public static Pipeline GetValidExtractionPipelineStatic(ICatalogueRepository catalogueRepository)
    {
        var validPipeline = new Pipeline(catalogueRepository);

        var source = new PipelineComponent(catalogueRepository, validPipeline, typeof(BaselineHackerExecuteDatasetExtractionSource), 0);
        source.CreateArgumentsForClassIfNotExists<BaselineHackerExecuteDatasetExtractionSource>();

        _=new PipelineComponent(catalogueRepository, validPipeline, typeof(SuccessfullyExtractedResultsDocumenter), 1);

        var destination = new PipelineComponent(catalogueRepository, validPipeline, typeof(ExecuteDatasetExtractionFlatFileDestination), 2);
        destination.CreateArgumentsForClassIfNotExists<ExecuteDatasetExtractionFlatFileDestination>();

        validPipeline.SourcePipelineComponent_ID = source.ID;
        validPipeline.DestinationPipelineComponent_ID = destination.ID;
        validPipeline.SaveToDatabase();

        return validPipeline;
    }
}