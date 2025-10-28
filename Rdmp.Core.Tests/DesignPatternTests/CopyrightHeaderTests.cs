// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Tests.Common;

namespace Rdmp.Core.Tests.DesignPatternTests;

/// <summary>
/// Tests that verify all C# source files have proper copyright headers.
/// </summary>
[TestFixture]
[Category("Unit")]
public class CopyrightHeaderTests
{
    private static readonly HashSet<string> IgnoreList = new()
    {
        "Program.cs",
        "Settings.Designer.cs",
        "Class1.cs",
        "Images.Designer.cs",
        "ToolTips.Designer.cs",
        "Resources.Designer.cs",
        "ProjectInstaller.cs",
        "ProjectInstaller.Designer.cs",
        "TableView.cs",
        "TreeView.cs"
    };

    [Test]
    public void AllSourceFiles_HaveCopyrightHeaders()
    {
        var solutionDir = FindSolutionDirectory();
        var csFiles = FindAllCSharpFiles(solutionDir);

        var suggestedNewFileContents = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        var copyrightIssues = new Dictionary<string, (string actual, string expected)>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var file in csFiles)
        {
            if (file.Contains(".Designer.cs") || IgnoreList.Contains(Path.GetFileName(file)))
                continue;

            var changes = false;
            var sbSuggestedText = new StringBuilder();

            var firstLine = File.ReadLines(file).FirstOrDefault() ?? string.Empty;

            if (!firstLine.StartsWith("// Copyright (c) The University of Dundee 2018-20")
                && firstLine != @"// This code is adapted from https://www.codeproject.com/Articles/1182358/Using-Autocomplete-in-Windows-Console-Applications")
            {
                changes = true;
                var expectedCopyright = $"// Copyright (c) The University of Dundee 2018-{DateTime.Now.Year}";
                copyrightIssues.Add(file, (firstLine, expectedCopyright));

                sbSuggestedText.AppendLine(expectedCopyright);
                sbSuggestedText.AppendLine(@"// This file is part of the Research Data Management Platform (RDMP).");
                sbSuggestedText.AppendLine(
                    @"// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.");
                sbSuggestedText.AppendLine(
                    @"// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.");
                sbSuggestedText.AppendLine(
                    @"// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.");
                sbSuggestedText.AppendLine();
                sbSuggestedText.AppendLine(firstLine);
                sbSuggestedText.Append(string.Join(Environment.NewLine, File.ReadLines(file).Skip(1)));
            }

            if (changes)
                suggestedNewFileContents.Add(file, sbSuggestedText.ToString());
        }

        // Report issues
        if (copyrightIssues.Any())
        {
            Console.WriteLine($"Found {copyrightIssues.Count} file(s) with missing or incorrect copyright headers:");
            foreach (var kvp in copyrightIssues)
            {
                Console.WriteLine($"  {kvp.Key}");
                Console.WriteLine($"    Actual:   {kvp.Value.actual}");
                Console.WriteLine($"    Expected: {kvp.Value.expected}");
            }
        }

        // Assert
        foreach (var kvp in suggestedNewFileContents)
        {
            var actualContents = File.ReadAllText(kvp.Key);
            var suggestedContents = kvp.Value;
            Assert.That(actualContents, Is.EqualTo(suggestedContents),
                $"Copyright header missing or incorrect in {kvp.Key}");
        }
    }

    private static DirectoryInfo FindSolutionDirectory()
    {
        var solutionDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (solutionDir?.GetFiles("*.sln").Any() != true)
            solutionDir = solutionDir?.Parent;

        Assert.That(solutionDir, Is.Not.Null, "Failed to find solution directory");
        return solutionDir;
    }

    private static List<string> FindAllCSharpFiles(DirectoryInfo solutionDir)
    {
        var csFiles = new List<string>();

        foreach (var file in solutionDir.EnumerateFiles("*.cs", SearchOption.AllDirectories))
        {
            // Skip obj/bin directories
            if (file.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            csFiles.Add(file.FullName);
        }

        return csFiles;
    }
}
