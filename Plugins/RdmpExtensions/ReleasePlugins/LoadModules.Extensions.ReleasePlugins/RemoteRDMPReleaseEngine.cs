// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using LoadModules.Extensions.ReleasePlugins.Data;
using Newtonsoft.Json;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataRelease;
using Rdmp.Core.DataExport.DataRelease.Pipeline;
using Rdmp.Core.DataExport.DataRelease.Potential;
using Rdmp.Core.Ticketing;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Ionic.Zip;

namespace LoadModules.Extensions.ReleasePlugins;

/// <summary>
/// Extends the standard ReleaseEngine to zip release folders and upload them to a remote RDMP instance via HTTP, integrating with ticketing systems to determine the target safe haven folder.
/// </summary>
public class RemoteRDMPReleaseEngine : ReleaseEngine
{
    public RemoteRDMPReleaseEngineSettings RemoteRDMPSettings { get; }

    public RemoteRDMPReleaseEngine(Project project, RemoteRDMPReleaseEngineSettings releaseSettings, IDataLoadEventListener listener, DirectoryInfo releaseFolder) : base(project, new ReleaseEngineSettings(), listener, new ReleaseAudit { ReleaseFolder = releaseFolder })
    {
        RemoteRDMPSettings = releaseSettings;
        RemoteRDMPSettings ??= new RemoteRDMPReleaseEngineSettings();
    }

    public override void DoRelease(Dictionary<IExtractionConfiguration, List<ReleasePotential>> toRelease, Dictionary<IExtractionConfiguration, ReleaseEnvironmentPotential> environments, bool isPatch)
    {
        base.DoRelease(toRelease, environments, isPatch);

        if (!ReleaseSuccessful)
            throw new Exception("Something horrible happened during Release... cannot progress!");

        ReleaseSuccessful = false;

        var releaseFileName = $"{GetArchiveNameForProject()}.zip";
        var projectSafeHavenFolder = GetSafeHavenFolder(Project.MasterTicket);
        if (projectSafeHavenFolder.Equals(string.Empty))
            throw new Exception("No Safe Haven folder specified in the Project Master Ticket");

        var zipOutput = Path.Combine(ReleaseAudit.ReleaseFolder.FullName, releaseFileName);
        ZipReleaseFolder(ReleaseAudit.ReleaseFolder, RemoteRDMPSettings.ZipPassword.GetDecryptedValue(), zipOutput);

        UploadToRemote(zipOutput, releaseFileName, projectSafeHavenFolder);

        ReleaseSuccessful = true;
    }

    private void UploadToRemote(string zipOutput, string releaseFileName, string projectSafeHavenFolder)
    {
        using var handler = new HttpClientHandler();
        using var client = new HttpClient(handler);
        using var content = new MultipartFormDataContent();
        handler.Credentials = new NetworkCredential
        {
            UserName = RemoteRDMPSettings.RemoteRDMP.Username,
            Password = RemoteRDMPSettings.RemoteRDMP.GetDecryptedPassword()
        };

        content.Add(new StreamContent(File.OpenRead(zipOutput)), "file", Path.GetFileName(releaseFileName));
        var settings = new
        {
            ProjectFolder = projectSafeHavenFolder,
            ZipPassword = RemoteRDMPSettings.ZipPassword.GetDecryptedValue()
        };
        content.Add(new StringContent(JsonConvert.SerializeObject(settings)), "settings");

        try
        {
            var result = client.PostAsync(RemoteRDMPSettings.RemoteRDMP.GetUrlForRelease(), content).Result;
            string resultStream;
            List<NotifyEventArgsProxy> messages;
            if (!result.IsSuccessStatusCode)
            {
                resultStream = result.Content.ReadAsStringAsync().Result;
                messages = JsonConvert.DeserializeObject<List<NotifyEventArgsProxy>>(resultStream);
                foreach (var eventArg in messages)
                {
                    _listener.OnNotify(this, eventArg);
                }
                throw new Exception("Upload failed");
            }
            else
            {
                resultStream = result.Content.ReadAsStringAsync().Result;
                messages = JsonConvert.DeserializeObject<List<NotifyEventArgsProxy>>(resultStream);
                foreach (var eventArg in messages)
                {
                    _listener.OnNotify(this, eventArg);
                }
                _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Upload succeeded"));
            }
        }
        catch (Exception ex)
        {
            _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Failed to upload data", ex));
            throw;
        }
    }

    private void ZipReleaseFolder(DirectoryInfo customExtractionDirectory, string zipPassword, string zipOutput)
    {
        var zip = new ZipFile() { UseZip64WhenSaving = Zip64Option.AsNecessary };
        if (!string.IsNullOrWhiteSpace(zipPassword))
            zip.Password = zipPassword;

        zip.AddDirectory(customExtractionDirectory.FullName);
        zip.Save(zipOutput);
    }

    private string GetArchiveNameForProject()
    {
        return $"{DateTime.UtcNow:yyyy-MM-dd_}Release-Proj-{Project.ProjectNumber}";
    }

    private string GetSafeHavenFolder(string masterTicket)
    {
        if (string.IsNullOrWhiteSpace(masterTicket))
            return $"Proj-{Project.ProjectNumber}";

        var catalogueRepository = Project.DataExportRepository.CatalogueRepository;
        var factory = new TicketingSystemFactory(catalogueRepository);
        var system = factory.CreateIfExists(catalogueRepository.GetTicketingSystem());

        return system == null ? string.Empty : system.GetProjectFolderName(masterTicket).Replace("/", "");
    }
}