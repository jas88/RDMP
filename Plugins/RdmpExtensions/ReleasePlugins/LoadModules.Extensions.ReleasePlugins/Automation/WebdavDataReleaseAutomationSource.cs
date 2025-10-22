using System;
using System.IO;
using System.Linq;
using System.Net;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using CatalogueLibrary.Repositories;
using LoadModules.Extensions.ReleasePlugins.Data;
using MapsDirectlyToDatabaseTable;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.Progress;
using WebDAVClient;
using WebDAVClient.Model;

namespace LoadModules.Extensions.ReleasePlugins.Automation
{
    public class WebdavDataReleaseAutomationSource : IPluginAutomationSource, IPipelineRequirement<IRDMPPlatformRepositoryServiceLocator>, ICheckable
    {
        private AutomationServiceSlot _serviceSlot;
        private IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        private const string PREFIX = "WEBDAV: ";

        [DemandsNestedInitialization()]
        public WebdavAutomationSettings ReleaseSettings { get; set; }

        public OnGoingAutomationTask GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            // we don't have our slot yet
            if(_serviceSlot == null)
                return null;

            var allJobs = _serviceSlot.AutomationJobs;

            // we want to run one at a time
            if (allJobs.Any(aj => (aj.LastKnownStatus == AutomationJobStatus.NotYetStarted || aj.LastKnownStatus == AutomationJobStatus.Running) 
                                  && aj.Description.StartsWith(PREFIX)))
            {
                return null;
            }

            // throttle failures (do not start if 5 or more crashes in the last 24 hours)
            if (allJobs.Where(aj => aj.Lifeline.HasValue && aj.Lifeline > DateTime.UtcNow.AddDays(-1))
                       .Count(aj => (aj.LastKnownStatus == AutomationJobStatus.Crashed)) >= 5)
            {
                return null;
            }

            WebDavDataRepository tableRepo = GetAuditRepo();
            if (tableRepo == null)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Unable to access the Audit Repository"));
                return null;
            }

            var file = GetFirstUnprocessed(tableRepo);
            if (file == null)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "No new files to process..."));
                return null;
            }

            var audit = new WebdavAutomationAudit(tableRepo, file.Href, FileResult.Unknown, "started...");

            var job = _serviceSlot.AddNewJob(AutomationJobType.UserCustomPipeline, PREFIX + file.DisplayName);

            var automaton = new WebdavAutoDownloader(ReleaseSettings, file, audit);

            return new OnGoingAutomationTask(job, automaton);
        }

        private Item GetFirstUnprocessed(WebDavDataRepository tableRepo)
        {
            var client = new Client(new NetworkCredential { UserName = ReleaseSettings.Username, Password = ReleaseSettings.Password.GetDecryptedValue() });
            client.Server = ReleaseSettings.Endpoint;
            client.BasePath = ReleaseSettings.BasePath;

            var remoteFolder = client.GetFolder(ReleaseSettings.RemoteFolder).Result;

            if (remoteFolder == null)
                return null;

            var files = client.List(remoteFolder.Href).Result;
            var enumerable = files as Item[] ?? files.ToArray();
            
            var allFilesDone = tableRepo.GetAllObjects<WebdavAutomationAudit>()
                                    .Where(f => f.FileResult == FileResult.Done);

            var latest = enumerable.Where(f => f.DisplayName.Contains("Release") &&
                                               allFilesDone.All(done => done.FileHref != f.Href))
                                   .OrderBy(f => f.LastModified).FirstOrDefault();

            return latest;
        }

        private WebDavDataRepository GetAuditRepo()
        {
            WebDavDataRepository tableRepo;
            var repoServer = _repositoryLocator.CatalogueRepository.GetAllObjects<ExternalDatabaseServer>()
                    .SingleOrDefault(s => s.CreatedByAssembly == typeof(Database.Class1).Assembly.GetName().Name);

            if (repoServer == null)
                return null;

            var discoveredServer = DataAccessPortal.GetInstance().ExpectServer(repoServer, DataAccessContext.DataExport);

            tableRepo = new WebDavDataRepository(discoveredServer.Builder);
            return tableRepo;
        }

        #region IPluginAutomationSource implementation useless methods
        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
        }

        public void Abort(IDataLoadEventListener listener)
        {
        }

        public OnGoingAutomationTask TryGetPreview()
        {
            return null;
        }

        public void PreInitialize(AutomationServiceSlot value, IDataLoadEventListener listener)
        {
            _serviceSlot = value;
        }

        public void PreInitialize(IRDMPPlatformRepositoryServiceLocator value, IDataLoadEventListener listener)
        {
            _repositoryLocator = value;
        }

        public void Check(ICheckNotifier notifier)
        {
            ((ICheckable)ReleaseSettings).Check(notifier);
        }
        #endregion

    }
}