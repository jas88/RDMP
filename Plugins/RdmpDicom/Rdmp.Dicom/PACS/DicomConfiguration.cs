// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using FellowOakDicom.Network;

namespace Rdmp.Dicom.PACS;

/// <summary>
/// Configuration for DICOM network operations including remote/local AE titles, ports, and timeout/cooldown settings for PACS communication
/// </summary>
public class DicomConfiguration
{
    public string RemoteAetHost { get; set; }
    public ushort RemoteAetPort { get; set; }
    public string RemoteAetTitle { get; set; }
    public string LocalAetHost { get; set; }
    public ushort LocalAetPort { get; set; }
    public string LocalAetTitle { get; set; }
    public int RequestCooldownInMilliseconds { get; set; }
    public int TransferCooldownInMilliseconds { get; set; }
    public int TransferPollingInMilliseconds { get; set; }
    public int TransferTimeOutInMilliseconds { get; set; }
    public DicomPriority Priority { get; set; }

}