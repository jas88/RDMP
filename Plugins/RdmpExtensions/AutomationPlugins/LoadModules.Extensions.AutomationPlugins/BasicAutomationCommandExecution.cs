using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.Startup;

namespace LoadModules.Extensions.AutomationPlugins;

public class BasicAutomationCommandExecution : BasicCommandExecution
{
    protected PluginRepository AutomationRepository { get; }

    public BasicAutomationCommandExecution(IBasicActivateItems activator):base(activator)
    {
        var repoFinder = new AutomateExtractionRepositoryFinder(BasicActivator.RepositoryLocator);
        try
        {
            AutomationRepository = repoFinder.GetRepositoryIfAny();
        }
        catch (System.Exception e)
        {
            SetImpossible($"No Automation Repository Found:{e.Message}");
            return;
        }

        if (AutomationRepository == null)
        {
            SetImpossible("There is no Automation Repository configured");
        }

    }

}