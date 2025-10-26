// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using HICPlugin.DataFlowComponents;
using HICPluginInteractive.DataFlowComponents;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Data;
using System.IO;
using Tests.Common.Scenarios;

namespace HICPluginTests.Integration;
class CHIColumnFinderTests : TestsRequiringAnExtractionConfiguration
{
    private readonly CHIColumnFinder _chiFinder = new();
    private readonly ThrowImmediatelyDataLoadEventListener _listener = ThrowImmediatelyDataLoadEventListener.QuietPicky;
    [Test]
    public void IgnoreColumnsAvoidsCHIChecking()
    {
        using var toProcess = new DataTable();
        toProcess.Columns.Add("Height");
        toProcess.Rows.Add(new object[] { 195 });

        Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));

        toProcess.Columns.Add("NothingToSeeHere");
        toProcess.Rows.Add(new object[] { 145, "1111111111" });

        Assert.Throws<Exception>(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));
        var fileName = Path.GetTempFileName();
        var fileInfo = new FileInfo(fileName);
        fileInfo.Attributes = FileAttributes.Temporary;
        StreamWriter streamWriter = File.AppendText(fileName);
        streamWriter.WriteLine("RDMP_ALL:");
        streamWriter.WriteLine("    - NothingToSeeHere");
        streamWriter.Flush();
        streamWriter.Close();
        _chiFinder.AllowListFile = fileInfo.FullName;

        _chiFinder.PreInitialize(_request, ThrowImmediatelyDataLoadEventListener.Quiet);
        Assert.DoesNotThrow(() => _chiFinder.ProcessPipelineData(toProcess, _listener, null));
    }
}
