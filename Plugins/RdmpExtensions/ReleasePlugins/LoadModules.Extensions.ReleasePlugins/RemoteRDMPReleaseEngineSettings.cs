using System;
using System.Net;
using System.Net.Http;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Remoting;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.ReleasePlugins;

public class RemoteRDMPReleaseEngineSettings : ICheckable
{
    [DemandsInitialization("Password for ZIP package")]
    public EncryptedString ZipPassword { get; set; }

    [DemandsInitialization("Delete the released files from the origin location if release is successful", DefaultValue = true)]
    public bool DeleteFilesOnSuccess { get; set; }

    [DemandsInitialization("Remote RDMP instance")]
    public RemoteRDMP RemoteRDMP { get; set; }

    public RemoteRDMPReleaseEngineSettings()
    {
        DeleteFilesOnSuccess = true;
    }

    public void Check(ICheckNotifier notifier)
    {
        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential
            {
                UserName = RemoteRDMP.Username,
                Password = RemoteRDMP.GetDecryptedPassword()
            }
        };
        var client = new HttpClient(handler);
        try
        {
            var baseUri = new UriBuilder(new Uri(RemoteRDMP.URL));
            baseUri.Path += "/api/plugin/";
            var message = new HttpRequestMessage(HttpMethod.Head, baseUri.ToString());
            var check = client.SendAsync(message).Result;
            check.EnsureSuccessStatusCode();
            notifier.OnCheckPerformed(new CheckEventArgs($"Checks passed {check.Content.ReadAsStringAsync().Result}", CheckResult.Success));
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("Checks failed", CheckResult.Fail, e));
        }
    }
}