// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using NUnit.Framework;

namespace Rdmp.Dicom.Tests.Unit;

internal class PublicPacsTest
{
    private const string LocalAetTitle = "STORESCP";
    public const string RemoteAetTitle = "ORTHANC";


    [TestCase("www.dicomserver.co.uk", 104)]
    public void EchoTest(string host, int port)
    {
        var success = false;
        var client = DicomClientFactory.Create(host, port, false, LocalAetTitle, RemoteAetTitle);
        client.AddRequestAsync(new DicomCEchoRequest
        {
            OnResponseReceived = (req, res) =>
            {
                success = true;
            }
        }
        ).Wait();
        client.SendAsync().Wait();
        Assert.That(success, $"No echo response from PACS on {host}:{port}");
    }


}