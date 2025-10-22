using Rdmp.Core.Curation.Data;
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