// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using SCIStorePlugin;

namespace SCIStorePlugin.Repositories;

public class WebServiceLoginFailureException : Exception
{
    public WebServiceLoginFailureException(string message) : base(message) { }
    public WebServiceLoginFailureException(string message, Exception inner) : base(message, inner) { }
}

public class WebServiceRetrievalFailure : Exception
{
    public WebServiceRetrievalFailure(string message) : base(message) { }
    public WebServiceRetrievalFailure(string message, Exception inner) : base(message, inner) { }
    public WebServiceRetrievalFailure(DateTime day, TimeSpan timeSpan, Exception inner)
        : base($"Failed to retrieve web service data for {day:yyyy-MM-dd} ({timeSpan.Hours}h)", inner) { }
}

public class LabReportRetrievalFailureException : Exception
{
    public LabReportRetrievalFailureException(string message) : base(message) { }
    public LabReportRetrievalFailureException(string message, Exception inner) : base(message, inner) { }
    public LabReportRetrievalFailureException(SciStoreRecord record, Exception inner)
        : base($"Failed to retrieve lab report for record {record.LabNumber}", inner) { }
}