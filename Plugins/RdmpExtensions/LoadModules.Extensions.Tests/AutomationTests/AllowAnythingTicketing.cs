// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using Rdmp.Core.Curation;
using Rdmp.Core.Ticketing;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Tests.AutomationTests;

public class AllowAnythingTicketing:ITicketingSystem
{
    public void Check(ICheckNotifier notifier)
    {

    }

    public bool IsValidTicketName(string ticketName)
    {
        return true;
    }

    public void NavigateToTicket(string ticketName)
    {

    }

    public TicketingReleaseabilityEvaluation GetDataReleaseabilityOfTicket(string masterTicket, string requestTicket,
        string releaseTicket, List<TicketingSystemReleaseStatus> _, out string reason, out Exception exception)
    {
        reason = null;
        exception = null;
        return TicketingReleaseabilityEvaluation.Releaseable;
    }

    public string GetProjectFolderName(string masterTicket) => $"Project {masterTicket}";
    public List<string> GetAvailableStatuses() => [];
}