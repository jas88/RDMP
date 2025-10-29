// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using System.Reflection;
using System.Runtime.CompilerServices;

#if WINDOWS
[assembly: System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
#endif

[assembly: AssemblyCompany("Health Informatics Centre, University of Dundee")]
[assembly: AssemblyProduct("RDMP Dicom Plugin")]
[assembly: AssemblyCopyright("Copyright (c) 2018 - 2024")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// These should be replaced with correct values by the release process
[assembly: AssemblyVersion("7.1.6")]
[assembly: AssemblyFileVersion("7.1.6")]
[assembly: AssemblyInformationalVersion("7.1.6")]
[assembly: InternalsVisibleTo("Rdmp.Dicom.Tests")]
