// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Tests.DesignPatternTests;
using Rdmp.Core.Tests.DesignPatternTests.ClassFileEvaluation;
using Tests.Common;

namespace Rdmp.UI.Tests.DesignPatternTests;

/// <summary>
/// UI-specific code quality tests that validate UI code standards and form initialization.
/// Separated from EvaluateNamespacesAndSolutionFoldersTests which now runs in Core.Tests.
/// </summary>
public class UICodeQualityTests : DatabaseTests
{
    [Test]
    public void EvaluateUICodeQuality()
    {
        var solutionDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (solutionDir?.GetFiles("*.sln").Any() != true) solutionDir = solutionDir?.Parent;
        Assert.That(solutionDir, Is.Not.Null, "Failed to find solution in any parent directories");

        var csFilesFound = new List<string>();

        // Collect all .cs files from the solution directory
        CollectCsFiles(solutionDir, csFilesFound);

        // Run UI-specific validations
        var uiStandardisationTest = new UserInterfaceStandardisationChecker();
        uiStandardisationTest.FindProblems(csFilesFound);

        var otherTestRunner = new RDMPFormInitializationTests();
        otherTestRunner.FindUninitializedForms(csFilesFound);
    }

    private void CollectCsFiles(DirectoryInfo directory, List<string> csFilesFound)
    {
        foreach (var file in directory.EnumerateFiles("*.cs"))
        {
            if (!file.Name.EndsWith(".Designer.cs") &&
                !file.Name.Equals("AssemblyInfo.cs"))
            {
                csFilesFound.Add(file.FullName);
            }
        }

        foreach (var subdir in directory.EnumerateDirectories())
        {
            if (subdir.Name.Equals("bin") || subdir.Name.Equals("obj") ||
                subdir.Name.Equals("packages") || subdir.Name.Equals(".git"))
                continue;

            CollectCsFiles(subdir, csFilesFound);
        }
    }
}
