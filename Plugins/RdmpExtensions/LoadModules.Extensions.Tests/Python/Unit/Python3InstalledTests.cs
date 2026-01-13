// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.IO;
using System.Linq;
using LoadModules.Extensions.Python.DataProvider;
using LoadModules.Extensions.Tests;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Tests.Python.Unit;

public class Python3InstalledTests
{
    [SetUp]
    public void IsPython3Installed()
    {
        var p = new PythonDataProvider
        {
            Version = PythonVersion.Version3
        };
        try
        {
            var version = p.GetPythonVersion();

            Console.WriteLine($"Found python version:{version}");
        }
        catch (Exception e)
        {
            Console.WriteLine("Tests are inconclusive because python version 3 is not installed in the expected location");

            Console.WriteLine(e.ToString());
            Assert.Inconclusive();
        }
    }

    [Test]
    public void PythonScript_Version3_DodgySyntax()
    {
        var MyPythonScript = @"print 'Hello World'";

        File.Delete("Myscript.py");
        File.WriteAllText("Myscript.py", MyPythonScript);

        var provider = new PythonDataProvider
        {
            Version = PythonVersion.Version3,
            FullPathToPythonScriptToRun = "Myscript.py",
            MaximumNumberOfSecondsToLetScriptRunFor = 0
        };

        //call with accept all
        provider.Check(new AcceptAllCheckNotifier());

        var toMemory = new ToMemoryDataLoadJob(false);
        provider.Fetch(toMemory, new GracefulCancellationToken());

        Assert.That(toMemory.EventsReceivedBySender[provider].Count(m => m.Message.Contains("SyntaxError: Missing parentheses in call to 'print'")), Is.EqualTo(1));
    }

    [Test]
    public void PythonScript_ValidScript()
    {
        var MyPythonScript = @"print (""Hello World"")";

        File.Delete("Myscript.py");
        File.WriteAllText("Myscript.py", MyPythonScript);

        var provider = new PythonDataProvider
        {
            Version = PythonVersion.Version3,
            FullPathToPythonScriptToRun = "Myscript.py",
            MaximumNumberOfSecondsToLetScriptRunFor = 0
        };

        //call with accept all
        provider.Check(new AcceptAllCheckNotifier());

        //new MockRepository().DynamicMock<IDataLoadJob>()
        provider.Fetch(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken());
    }

}