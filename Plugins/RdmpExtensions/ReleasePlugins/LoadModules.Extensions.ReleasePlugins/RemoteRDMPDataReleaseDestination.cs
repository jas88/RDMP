// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction;
using Rdmp.Core.DataExport.DataRelease.Audit;
using Rdmp.Core.DataExport.DataRelease.Pipeline;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.Ticketing;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.ReleasePlugins;

/// <summary>
/// Pipeline destination component that releases extracted data to a remote RDMP instance by zipping the release folder and uploading it via HTTP, with support for patching existing releases and cleanup of cumulative extraction results.
/// </summary>
public class RemoteRDMPDataReleaseDestination : IPluginDataFlowComponent<ReleaseAudit>, IDataFlowDestination<ReleaseAudit>, IPipelineRequirement<Project>, IPipelineRequirement<ReleaseData>
{
    [DemandsNestedInitialization]
    public RemoteRDMPReleaseEngineSettings RDMPReleaseSettings { get; set; }

    private RemoteRDMPReleaseEngine _remoteRDMPReleaseEngineengine;
    private Project _project;
    private ReleaseData _releaseData;
    private List<IExtractionConfiguration> _configurationReleased;

    public ReleaseAudit ProcessPipelineData(ReleaseAudit releaseAudit, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        if (releaseAudit == null)
            return null;

        if (releaseAudit.ReleaseFolder == null)
        {
            releaseAudit.ReleaseFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            if (!releaseAudit.ReleaseFolder.Exists)
                releaseAudit.ReleaseFolder.Create();
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning,
                $"No destination folder specified! Did you forget to introduce and initialize the ReleaseFolderProvider in the pipeline? The release output will be located in {releaseAudit.ReleaseFolder.FullName}"));
        }

        if (_releaseData.ReleaseState == ReleaseState.DoingPatch)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "CumulativeExtractionResults for datasets not included in the Patch will now be erased."));

            var recordsDeleted = 0;

            foreach (var redundantResult in _releaseData.ConfigurationsForRelease.Keys
                         .Select(configuration => new { configuration, current = configuration })
                         .Select(t => new { t, currentResults = t.configuration.CumulativeExtractionResults })
                         .SelectMany(t => t.currentResults.Where(r =>
                             _releaseData.ConfigurationsForRelease[t.t.current]
                                 .All(rp => rp.DataSet.ID != r.ExtractableDataSet_ID))))
            {
                redundantResult.DeleteInDatabase();
                recordsDeleted++;
            }

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Deleted {recordsDeleted} old CumulativeExtractionResults (That were not included in the final Patch you are preparing)"));
        }

        _remoteRDMPReleaseEngineengine = new RemoteRDMPReleaseEngine(_project, RDMPReleaseSettings, listener, releaseAudit.ReleaseFolder);

        _remoteRDMPReleaseEngineengine.DoRelease(_releaseData.ConfigurationsForRelease, _releaseData.EnvironmentPotentials, isPatch: _releaseData.ReleaseState == ReleaseState.DoingPatch);

        _configurationReleased = _remoteRDMPReleaseEngineengine.ConfigurationsReleased;
        return null;
    }

    public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {
        if (pipelineFailureExceptionIfAny != null && _releaseData != null)
        {
            try
            {
                var remnantsDeleted = 0;

                foreach (var remnant in _releaseData.ConfigurationsForRelease.Keys.Cast<ExtractionConfiguration>()
                             .SelectMany(configuration => configuration.ReleaseLog))
                {
                    remnant.DeleteInDatabase();
                    remnantsDeleted++;
                }

                if (remnantsDeleted > 0)
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                        $"Because release failed we are deleting ReleaseLogEntries, this resulted in {remnantsDeleted} deleted records, you will likely need to re-extract these datasets"));
            }
            catch (Exception e1)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Error occurred when trying to clean up remnant ReleaseLogEntries", e1));
            }
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Pipeline completed..."));
        }

        if (pipelineFailureExceptionIfAny == null)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Data release succeeded into: {RDMPReleaseSettings.RemoteRDMP.Name}"));

            // we can freeze the configuration now:
            foreach (var config in _configurationReleased)
            {
                config.IsReleased = true;
                config.SaveToDatabase();
            }
            if (RDMPReleaseSettings.DeleteFilesOnSuccess)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Cleaning up..."));
                ExtractionDirectory.CleanupExtractionDirectory(this, _project.ExtractionDirectory, _configurationReleased, listener);
            }

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "All done!"));
        }
    }

    public void Abort(IDataLoadEventListener listener)
    {
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "This component cannot Abort!"));
    }

    public void Check(ICheckNotifier notifier)
    {
        var projectSafeHavenFolder = GetSafeHavenFolder(_project.MasterTicket);
        notifier.OnCheckPerformed(string.IsNullOrWhiteSpace(projectSafeHavenFolder)
            ? new CheckEventArgs("No Safe Haven folder specified in the Project Master Ticket", CheckResult.Fail)
            : new CheckEventArgs("Project Master Ticket contains Safe Haven folder", CheckResult.Success));

        ((ICheckable)RDMPReleaseSettings).Check(notifier);
    }

    private string GetSafeHavenFolder(string masterTicket)
    {
        if (string.IsNullOrWhiteSpace(masterTicket))
            return $"Proj-{_project.ProjectNumber}";

        var catalogueRepository = _project.DataExportRepository.CatalogueRepository;
        var factory = new TicketingSystemFactory(catalogueRepository);
        var system = factory.CreateIfExists(catalogueRepository.GetTicketingSystem());

        return system?.GetProjectFolderName(masterTicket).Replace("/", "") ?? string.Empty;
    }

    public void PreInitialize(Project value, IDataLoadEventListener listener)
    {
        _project = value;
    }

    public void PreInitialize(ReleaseData value, IDataLoadEventListener listener)
    {
        _releaseData = value;
    }
}