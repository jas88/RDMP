using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using System;
using System.Linq;

namespace LoadModules.Extensions.AutomationPlugins.Data.Repository;

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