// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
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