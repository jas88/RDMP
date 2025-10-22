using System;
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