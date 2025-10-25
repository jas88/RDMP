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

public abstract class TestsRequiringAnAutomationPluginRepository:DatabaseTests
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

    [TearDown]
    public void DisposeAutomationRepository()
    {
        Repo?.Dispose();
    }
}