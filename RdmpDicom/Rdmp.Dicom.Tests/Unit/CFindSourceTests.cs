// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using NUnit.Framework;
using Rdmp.Core.CommandLine.Interactive;
using Rdmp.Dicom.CommandExecution;
using Rdmp.Core.ReusableLibraryCode.Checks;
using System.IO;
using NUnit.Framework.Legacy;
using Tests.Common;

namespace Rdmp.Dicom.Tests.Unit;

class CFindSourceTests : UnitTests
{
    [Test]
    public void TestRunFindOn_PublicServer()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);

        var cmd = new ExecuteCommandCFind(
            new ConsoleInputManager(RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet) { DisallowInput = true },
            "2001-01-01",
            "2002-01-01",
            "www.dicomserver.co.uk",
            104,
            "you",
            "me",
            dir.FullName);
        cmd.Execute();

        // file name is miday on 2001 1st January
        var f = Path.Combine(dir.FullName, @"out/Data/Cache/ALL/20010101120000.csv");
        FileAssert.Exists(f);

        var result = File.ReadAllLines(f);

        // should be at least 1 image in the public test server
        Assert.That(result, Is.Not.Empty);
    }
}
