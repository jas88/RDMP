// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Repositories;
using HIC.Logging;
using HIC.Logging.Listeners;
using Ionic.Zip;
using LoadModules.Extensions.ReleasePlugins.Data;
using MapsDirectlyToDatabaseTable;
using RDMPAutomationService;
using RDMPAutomationService.EventHandlers;
using RDMPAutomationService.Interfaces;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.Progress;
using WebDAVClient;
using WebDAVClient.Model;

namespace LoadModules.Extensions.ReleasePlugins.Automation
{
    public class WebdavAutoDownloader : IAutomateable
    {
        private readonly WebdavAutomationSettings options;
        private readonly Item file;
        private readonly WebdavAutomationAudit audit;

        private IDataLoadEventListener listener;
        private const string TASK_NAME = "Webdav Auto Release";

        public WebdavAutoDownloader(WebdavAutomationSettings options, Item file, WebdavAutomationAudit audit)
        {
            this.options = options;
            this.file = file;
            this.audit = audit;
        }

        public OnGoingAutomationTask GetTask()
        {
            throw new NotImplementedException("Cannot do this...");
        }

        public void RunTask(OnGoingAutomationTask task)
        {
            task.Job.SetLastKnownStatus(AutomationJobStatus.Running);
            task.Job.TickLifeline();
            
            var sd = new ServerDefaults((CatalogueRepository) task.Repository);
            var loggingServer = sd.GetDefaultFor(ServerDefaults.PermissableDefaults.LiveLoggingServer_ID);
            if (loggingServer != null)
            {
                var lm = new LogManager(loggingServer);
                lm.CreateNewLoggingTaskIfNotExists(TASK_NAME);
                var dli = lm.CreateDataLoadInfo(TASK_NAME, GetType().Name, task.Job.Description, String.Empty, false);

                listener = new ToLoggingDatabaseDataLoadEventListener(lm, dli);

                task.Job.SetLoggingInfo(loggingServer, dli.ID);
            }
            else
            {
                // TODO: See if we can log anyway somewhere... or bomb out?
                listener = new FromCheckNotifierToDataLoadEventListener(new IgnoreAllErrorsCheckNotifier());
            }

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Ready to download and unzip: " + file.DisplayName));

            WebDavDataRepository tableRepo = GetAuditRepo(task);
            if (tableRepo == null)
            {
                task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Unable to access the Audit Repository"));
                FinalizeLogs();
                return;
            }

            try
            {
                var zipFilePath = DownloadToDestination(file);

                if (String.IsNullOrWhiteSpace(zipFilePath))
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Download failed, will retry later (max 5 times in a 24hr period)"));
                    task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
                    FinalizeLogs();
                    return;
                }

                if (!UnzipToReleaseFolder(zipFilePath))
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Unzipping failed, will retry later (max 5 times in a 24hr period)"));
                    task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
                    FinalizeLogs();
                    return;
                }

                task.Job.TickLifeline();

                var archivedOk = ArchiveFile(file, "Done");
                
                audit.FileResult = FileResult.Done;
                audit.Updated = DateTime.UtcNow;
                audit.Message = "RELEASED!";
                audit.SaveToDatabase();

                task.Job.TickLifeline();

                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Job done: " + file.DisplayName + " RELEASED!"));

                if (archivedOk)
                {
                    task.Job.SetLastKnownStatus(AutomationJobStatus.Finished);
                    task.Job.DeleteInDatabase();
                }
                else
                {
                    listener.OnNotify(this,
                        new NotifyEventArgs(ProgressEventType.Warning,
                            "Archiving failed: file has been released but could not be archived on the webdav server. " +
                            "It will not be picked up again but you may want to move the file manually using a webdav client."));
                    task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
                }
            }
            catch (Exception ex)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Unexpected error! " +
                                                                                     "Despair not: the file will be picked up again at the next iteration " +
                                                                                     "(max 5 times in a 24hr period).", ex));

                audit.FileResult = FileResult.Errored;
                audit.Message = ExceptionHelper.ExceptionToListOfInnerMessages(ex);
                audit.Updated = DateTime.UtcNow;
                audit.SaveToDatabase();

                task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
            }
            finally
            {
                FinalizeLogs();
            }
        }

        private WebDavDataRepository GetAuditRepo(OnGoingAutomationTask task)
        {
            WebDavDataRepository tableRepo;
            var repoServer = task.Repository.GetAllObjects<ExternalDatabaseServer>()
                    .SingleOrDefault(s => s.CreatedByAssembly == typeof (Database.Class1).Assembly.GetName().Name);

            if (repoServer == null)
                return null;

            var discoveredServer = DataAccessPortal.GetInstance().ExpectServer(repoServer, DataAccessContext.DataExport);

            tableRepo = new WebDavDataRepository(discoveredServer.Builder);
            return tableRepo;
        }

        private string DownloadToDestination(Item file)
        {
            var client = new Client(new NetworkCredential { UserName = options.Username, Password = options.Password.GetDecryptedValue() });
            client.Server = options.Endpoint;
            client.BasePath = options.BasePath;
            try
            {
                using (var fileStream = File.Create(Path.Combine(options.LocalDestination, file.DisplayName)))
                {
                    var content = client.Download(file.Href).Result;
                    content.CopyTo(fileStream);
                }
            }
            catch(Exception ex)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Error downloading file!", ex));
                return null;
            }

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, String.Format("Downloaded to {0}", Path.Combine(options.LocalDestination, file.DisplayName))));

            return Path.Combine(options.LocalDestination, file.DisplayName);
        }

        private bool UnzipToReleaseFolder(string zipFilePath)
        {
            var filename = Path.GetFileNameWithoutExtension(zipFilePath);
            Debug.Assert(filename != null, "filename != null");
            var projFolder = Regex.Match(filename, @"\((.*)\)").Groups[1].Value;

            var outputFolder = projFolder;
            if (String.IsNullOrWhiteSpace(projFolder))
            {
                var linkProj = Regex.Match(filename, "Proj-(\\d+)").Groups[1].Value;
                if (String.IsNullOrWhiteSpace(linkProj))
                    outputFolder = "Project " + Guid.NewGuid().ToString("N");
                else
                    outputFolder = "Project " + linkProj;
            }

            var destination = new DirectoryInfo(Path.Combine(options.LocalDestination, outputFolder, filename));
            if (destination.Exists && destination.EnumerateFileSystemInfos().Any())
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Destination folder is not empty!"));
                return false;
            }

            try
            {
                using (var zip = ZipFile.Read(zipFilePath))
                {
                    zip.Password = options.ZipPassword.GetDecryptedValue();
                    zip.ExtractAll(destination.FullName);
                }
            }
            catch (Exception ex)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Error unzipping file!", ex));
                return false;
            }

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, String.Format("Unzipped all to {0}", destination)));
            return true;
        }

        private bool ArchiveFile(Item file, string archiveLocation)
        {
            var client = new Client(new NetworkCredential { UserName = options.Username, Password = options.Password.GetDecryptedValue() });
            client.Server = options.Endpoint;
            client.BasePath = options.BasePath;

            var archived = client.MoveFile(file.Href, Path.Combine(options.RemoteFolder, archiveLocation, file.DisplayName).Replace("\\","/")).Result;

            if(archived)
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Archived: " + file.DisplayName + " to " + Path.Combine(options.RemoteFolder, archiveLocation, file.DisplayName).Replace("\\", "/")));
            else
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Error archiving file!"));

            return archived;
        }

        private void FinalizeLogs()
        {
            var dbLog = (listener as ToLoggingDatabaseDataLoadEventListener);
            if (dbLog != null)
                dbLog.FinalizeTableLoadInfos();
        }
    }
}