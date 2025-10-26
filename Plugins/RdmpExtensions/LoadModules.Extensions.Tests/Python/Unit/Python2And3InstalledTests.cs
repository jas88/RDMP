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

public class Python2And3InstalledTests
{
    [SetUp]
    public void IsPython2ANDPython3Installed()
    {
        new Python2InstalledTests().IsPython2Installed();
        new Python3InstalledTests().IsPython3Installed();
    }
    [Test]
    public void PythonScript_OverrideExecutablePath_VersionMismatch()
    {
        var MyPythonScript = @"s = print('==>')";
        File.Delete("Myscript.py");
        File.WriteAllText("Myscript.py", MyPythonScript);

        var provider = new PythonDataProvider
        {
            MaximumNumberOfSecondsToLetScriptRunFor = 500,
            Version = PythonVersion.Version3,
            FullPathToPythonScriptToRun = "Myscript.py"
        };
        //call with accept all
        provider.Check(new AcceptAllCheckNotifier());// version 3 should now be installed

        //version 3 executable path is explicit override for executing commands
        provider.OverridePythonExecutablePath = new FileInfo(provider.GetFullPythonPath());
        provider.Version = PythonVersion.Version2;

        //so we now know that version 3 is installed, and we have overriden the python path to the .exe explicitly and we are trying to launch with Version2 enum now
        var ex = Assert.Throws<Exception>(()=>
        {
            provider.Check(ThrowImmediatelyCheckNotifier.Quiet);
        });
        Assert.That(ex?.Message, Does.Contain(@"which is incompatible with the desired version 2.7"));
    }
}