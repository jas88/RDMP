// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.IO;
using System.Xml.Serialization;
using HICPluginTests;
using NUnit.Framework;
using Rdmp.Core.Validation;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using Tests.Common;

namespace HICPluginTests.Integration;

[Category("Database")]
public class SCIStoreWebServiceProviderTests : DatabaseTests
{
    [Test]
    public void LabWithDifferentClinicalCodeDescriptionsForSameTestCode()
    {
        Validator.LocatorForXMLDeserialization = RepositoryLocator;

        var serializer = new XmlSerializer(typeof(CombinedReportData));
        var lab = serializer.Deserialize(new StringReader(TestReports.report_with_multiple_descriptions)) as CombinedReportData;

        var readCodeConstraint = new MockReferentialIntegrityConstraint();

        var reportFactory = new SciStoreReportFactory(readCodeConstraint);
        reportFactory.Create(lab, ThrowImmediatelyDataLoadEventListener.Quiet);
    }
}