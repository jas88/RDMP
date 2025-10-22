using LoadModules.Extensions.AutomationPlugins.Data;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;

namespace LoadModules.Extensions.AutomationPlugins;

internal class ExecuteCommandCreateNewAutomateExtraction : BasicAutomationCommandExecution
{
    public IExtractionConfiguration ExtractionConfiguration { get; }
    public AutomateExtractionSchedule Schedule { get; }

    public ExecuteCommandCreateNewAutomateExtraction(IBasicActivateItems activator,IExtractionConfiguration extractionConfiguration):base(activator)
    {
        // if base class already errored out (e.g. no automation setup)
        if (IsImpossible)
        {
            return;
        }

        var existingSchedules = AutomationRepository.GetAllObjects<AutomateExtractionSchedule>();

        // the project doesnt have a schedule
        Schedule = existingSchedules.FirstOrDefault(s => s.Project_ID == extractionConfiguration.Project_ID);

        if (Schedule == null)
        {
            SetImpossible($"Project {extractionConfiguration.Project} does not have an {nameof(AutomateExtractionSchedule)}");
            return;
        }


        ExtractionConfiguration = extractionConfiguration;
        var existingAutomateExtractions = AutomationRepository.GetAllObjects<AutomateExtraction>();

        if (existingAutomateExtractions.Any(s => s.ExtractionConfigurationId == extractionConfiguration.ID))
        {
            SetImpossible($"Configuration already has a {nameof(AutomateExtraction)}");
        }
    }

    public override Image<Rgba32> GetImage(IIconProvider iconProvider)
    {
        return iconProvider.GetImage(typeof(AutomateExtraction), OverlayKind.Add);
    }

    public override void Execute()
    {
        base.Execute();

        var newObj = new AutomateExtraction(AutomationRepository, Schedule, ExtractionConfiguration);
        Publish(ExtractionConfiguration);
        Emphasise(newObj);
    }


}