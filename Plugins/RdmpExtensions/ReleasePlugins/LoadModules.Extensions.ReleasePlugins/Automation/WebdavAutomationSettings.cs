// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
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