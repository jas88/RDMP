// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode;
using SCIStore.SciStoreServices81;

namespace SCIStorePlugin.Data;

/// <summary>
/// Data Transfer Object
/// (Database column names should really be named according to the same C# conventions...)
/// </summary>
public class SciStoreHeader
{
    private string _labNumber;
    private string _testReportId;
    public string CHI { get; set; }

    public string LabNumber
    {
        get => _labNumber;
        set => _labNumber = UsefulStuff.RemoveIllegalFilenameCharacters(value);
    }

    public string TestReportID
    {
        get => _testReportId;
        set => _testReportId = UsefulStuff.RemoveIllegalFilenameCharacters(value);
    }

    public string PatientID { get; set; }
    public string ClinicalDataRequired { get; set; } // ServiceRequest > ClinicalDataRequired

    // ServiceProvider > ProvidingLocation
    public string ProvidingOrganisationID { get; set; }
    public string ProvidingOrganisationName { get; set; }
    public string ProvidingOrganisationType { get; set; }

    // RequestingParty
    public string RequestingPartyID { get; set; }
    public string RequestingPartyPosition { get; set; }
    public string RequestingPartyName { get; set; }
    public string RequestingPartyStatus { get; set; }

    // RequestingParty > EmployingOrganisation
    public string RequestingOrganisationID { get; set; } // Source_code in old schema
    public string RequestingOrganisationName { get; set; }
    public string RequestingOrganisationStatus { get; set; }
    public string RequestingOrganisationType { get; set; }

    public string Discipline { get; set; }
    public string hb_extract { get; set; }
}