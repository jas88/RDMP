using System;
using System.Net;
using CatalogueLibrary.Data;
using ReusableLibraryCode.Checks;
using WebDAVClient;

namespace LoadModules.Extensions.ReleasePlugins.Automation
{
    public class WebdavAutomationSettings : ICheckable
    {
        [DemandsInitialization("Webdav remote folder")]
        public string RemoteFolder { get; set; }

        [DemandsInitialization("Password for ZIP package")]
        public EncryptedString ZipPassword { get; set; }

        [DemandsInitialization("Local folder to decompress files in")]
        public string LocalDestination { get; set; }

        [DemandsInitialization("Webdav endpoint")]
        public string Endpoint { get; set; }

        [DemandsInitialization("Webdav Base Path")]
        public string BasePath { get; set; }

        [DemandsInitialization("Webdav username")]
        public string Username { get; set; }

        [DemandsInitialization("Webdav password")]
        public EncryptedString Password { get; set; }

        public void Check(ICheckNotifier notifier)
        {
            var client =
                new Client(new NetworkCredential
                {
                    UserName = this.Username,
                    Password = this.Password.GetDecryptedValue()
                });
            client.Server = this.Endpoint;
            client.BasePath = this.BasePath;

            try
            {
                var remoteFolder = client.GetFolder(this.RemoteFolder).Result;
                if (remoteFolder == null)
                    notifier.OnCheckPerformed(new CheckEventArgs("Checks failed", CheckResult.Fail));
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Checks failed", CheckResult.Fail, e));
            }

            notifier.OnCheckPerformed(new CheckEventArgs("Checks passed", CheckResult.Success));
            // test if release is a valid folder;
            //                                  ^- IMPORTANT semicolon or test will fail!  
        }
    }
}