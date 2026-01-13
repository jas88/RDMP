// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using HICPlugin.DataFlowComponents;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace HICPluginTests.Integration;

class ChrisHallSpecialExplicitSourceTests:TestsRequiringAnExtractionConfiguration
{
    [Test]
    public void TestUse()
    {

        //DataExtractionSpecialExplicitSource
        var source = new ChrisHallSpecialExplicitSource
        {
            DatabaseToUse = "master",
            Collation = "Latin1_General_Bin"
        };

        source.PreInitialize(_request,ThrowImmediatelyDataLoadEventListener.Quiet);

        var chunk = source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
        Assert.That(chunk, Is.Not.Null);
    }
}