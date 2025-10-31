// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using NUnit.Framework;
using Rdmp.Dicom.CommandExecution;

namespace Rdmp.Dicom.Tests.Unit;

internal class ExecuteCommandCreateNewImagingDatasetSuiteUnitTests
{
    [Test]
    public void TestMakeUniqueName()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ExecuteCommandCreateNewImagingDatasetSuite.MakeUniqueName(Array.Empty<string>(), "ff")
        , Is.EqualTo("ff"));

            Assert.That(ExecuteCommandCreateNewImagingDatasetSuite.MakeUniqueName(new[] { "ff" }, "ff")
    , Is.EqualTo("ff2"));
            Assert.That(ExecuteCommandCreateNewImagingDatasetSuite.MakeUniqueName(new[] { "ff", "ff2", "ff3" }, "ff")
    , Is.EqualTo("ff4"));
        });
    }

}