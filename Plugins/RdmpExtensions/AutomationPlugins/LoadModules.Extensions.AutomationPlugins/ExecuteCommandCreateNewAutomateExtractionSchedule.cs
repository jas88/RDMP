using LoadModules.Extensions.AutomationPlugins.Data;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;

namespace LoadModules.Extensions.AutomationPlugins;

public class ExecuteCommandCreateNewAutomateExtractionSchedule : BasicAutomationCommandExecution
{
    public IProject Project { get; }

    public ExecuteCommandCreateNewAutomateExtractionSchedule(IBasicActivateItems activator,IProject project) :base(activator)
    {
        // if base class already errored out (e.g. no automation setup)
        if(IsImpossible)
        {
            return;
        }

        var existing = AutomationRepository.GetAllObjects<AutomateExtractionSchedule>();

        if(existing.Any(s=>s.Project_ID == project.ID))
        {
            SetImpossible($"Project already has an {nameof(AutomateExtractionSchedule)}");
            return;
        }

        Project = project;
    }

    public override Image<Rgba32> GetImage(IIconProvider iconProvider)
    {
        return iconProvider.GetImage(typeof(AutomateExtractionSchedule), OverlayKind.Add);
    }
    public override void Execute()
    {
        base.Execute();

        var schedule = new AutomateExtractionSchedule(AutomationRepository, Project);
        Publish(Project);
        Emphasise(schedule);
    }

}