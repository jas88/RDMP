// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.IO;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Python.Tests.Unit;

public class TestsThatWorkRegardless
{

    [Test]
    public void PythonVersionNotSetYet()
    {
        var provider = new PythonDataProvider();
        var ex = Assert.Throws<Exception>(()=>provider.Check(ThrowImmediatelyCheckNotifier.Quiet));
        Assert.That(ex?.Message, Is.EqualTo("Version of Python required for script has not been selected"));

    }


    [Test]
    public void PythonScript_OverrideExecutablePath_FileDoesntExist()
    {
        var MyPythonScript = @"s = raw_input ('==>')";

        var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");

        File.Delete(py);
        File.WriteAllText(py, MyPythonScript);

        var provider = new PythonDataProvider
        {
            Version = PythonVersion.Version2,
            FullPathToPythonScriptToRun = py,
            MaximumNumberOfSecondsToLetScriptRunFor = 5,
            OverridePythonExecutablePath = new FileInfo(@"C:\fishmongers\python")
        };
        //call with accept all
        var ex = Assert.Throws<Exception>(()=>provider.Check(new AcceptAllCheckNotifier()));

        Assert.That(ex?.Message, Does.Contain(@"The specified OverridePythonExecutablePath:C:\fishmongers\python does not exist"));

    }

}