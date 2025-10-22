using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Icons.IconOverlays;
using Rdmp.Core.Icons.IconProvision;
using Rdmp.Core.Providers.Nodes;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoadModules.Extensions.AutomationPlugins;

public class AutomationUserInterface : PluginUserInterface
{
    public AutomateExtractionRepository AutomationRepository { get; private set; }
    public AutomateExtraction[] AllAutomateExtractions { get; private set; }
    public AutomateExtractionSchedule[] AllSchedules { get; private set; }

    public AutomationUserInterface(IBasicActivateItems itemActivator) : base(itemActivator)
    {
        try
        {
            _scheduleIcon = Image.Load<Rgba32>(AutomationImages.AutomateExtractionSchedule);
            _automateExtractionIcon = Image.Load<Rgba32>(AutomationImages.AutomateExtraction);
        }
        catch (Exception)
        {
            _scheduleIcon = Image.Load<Rgba32>(CatalogueIcons.NoIconAvailable);
            _automateExtractionIcon = Image.Load<Rgba32>(CatalogueIcons.NoIconAvailable);
        }

    }

    public override Image<Rgba32> GetImage(object concept, OverlayKind kind = OverlayKind.None)
    {
        if (concept is AutomateExtractionSchedule || concept as Type == typeof(AutomateExtractionSchedule))
        {
            return IconOverlayProvider.GetOverlay(_scheduleIcon,kind);
        }

        if (concept is AutomateExtraction || concept as Type == typeof(AutomateExtraction))
        {
            return IconOverlayProvider.GetOverlay(_automateExtractionIcon, kind);
        }

        return base.GetImage(concept, kind);
    }

    public override object[] GetChildren(object model)
    {
        switch (model)
        {
            case IProject p:
            {
                var schedule = GetScheduleIfAny(p);

                if(schedule != null)
                    return new[] { schedule };
                break;
            }
            case IExtractionConfiguration ec:
            {
                var automate = GetAutomateExtractionIfAny(ec);
                if (automate != null)
                    return new[] { automate };
                break;
            }
        }

        return base.GetChildren(model);
    }

    private AutomateExtractionSchedule GetScheduleIfAny(IProject p)
    {
        TryGettingAutomationRepository();

        return AutomationRepository == null ? null : AllSchedules.FirstOrDefault(aes => aes.Project_ID == p.ID);
    }

    private AutomateExtraction GetAutomateExtractionIfAny(IExtractionConfiguration ec)
    {
        TryGettingAutomationRepository();

        return AutomationRepository == null ? null : AllAutomateExtractions.FirstOrDefault(ae => ae.ExtractionConfigurationId == ec.ID);
    }

    DateTime _lastLook = DateTime.MinValue;
    private readonly Image<Rgba32> _scheduleIcon;
    private readonly Image<Rgba32> _automateExtractionIcon;

    private void TryGettingAutomationRepository()
    {
        // we looked recently already don't spam that thing
        if (DateTime.Now - _lastLook < TimeSpan.FromSeconds(5))
            return;

        if (AutomationRepository != null)
            return;

        try
        {
            var repo = new AutomateExtractionRepositoryFinder(BasicActivator.RepositoryLocator);
            AutomationRepository = (AutomateExtractionRepository)repo.GetRepositoryIfAny();

            AllAutomateExtractions = AutomationRepository.GetAllObjects<AutomateExtraction>();
            AllSchedules = AutomationRepository.GetAllObjects<AutomateExtractionSchedule>();

            _lastLook = DateTime.Now;
        }
        catch (Exception)
        {
            AutomationRepository = null;
            _lastLook = DateTime.Now;
        }
    }

    public override IEnumerable<IAtomicCommand> GetAdditionalRightClickMenuItems(object o)
    {
        switch (o)
        {
            case AllExternalServersNode:
                yield return new ExecuteCommandCreateNewExternalDatabaseServer(BasicActivator, new AutomateExtractionPluginPatcher(), PermissableDefaults.None);
                break;
            case IProject p:
                yield return new ExecuteCommandCreateNewAutomateExtractionSchedule(BasicActivator, p);
                break;
            case IExtractionConfiguration ec:
                yield return new ExecuteCommandCreateNewAutomateExtraction(BasicActivator, ec);
                break;
            case AutomateExtraction ae:
                yield return new ExecuteCommandSet(BasicActivator, ae, typeof(AutomateExtraction).GetProperty(nameof(AutomateExtraction.BaselineDate)))
                {
                    OverrideCommandName = "Set Baseline Date"
                };
                break;
        }
    }
}