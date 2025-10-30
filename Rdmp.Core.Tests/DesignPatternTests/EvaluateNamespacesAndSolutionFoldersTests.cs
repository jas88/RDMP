// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.VisualStudioSolutionFileProcessing;
using Rdmp.Core.Tests.DesignPatternTests.ClassFileEvaluation;
using Tests.Common;

namespace Rdmp.Core.Tests.DesignPatternTests;

public class EvaluateNamespacesAndSolutionFoldersTests : DatabaseTests
{
    private const string SolutionName = "HIC.DataManagementPlatform.sln";
    private readonly List<string> _csFilesFound = new();

    public static readonly HashSet<string> IgnoreList = new()
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
    public void EvaluateNamespacesAndSolutionFolders()
    {
        // This test now runs all separated tests to maintain compatibility with existing test runners
        // Each test is run individually so failures can be traced to specific validation areas

        var allTests = new[]
        {
            "EvaluateSolutionStructure",
            "EvaluateProjectFileStructure",
            "EvaluateClassNamingAndNamespaces",
            "EvaluateDuplicateFiles",
            "EvaluateInterfaceDeclarations",
            "EvaluateClassDocumentation",
            "EvaluateDocumentationCrossExamination",
            "EvaluateRelationshipProperties",
            "EvaluateExplicitDatabaseNames",
            "EvaluateAutoComments",
            "EvaluateCopyrightHeaders"
        };

        var testRunner = new EvaluateNamespacesAndSolutionFoldersTestsSeparated();
        var failedTests = new List<string>();

        foreach (var testName in allTests)
        {
            try
            {
                var testMethod = testRunner.GetType().GetMethod(testName);
                Assert.That(testMethod, Is.Not.Null, $"Test method {testName} not found");

                Console.WriteLine($"Running {testName}...");
                testMethod.Invoke(testRunner, null);
                Console.WriteLine($"✓ {testName} passed");
            }
            catch (Exception ex)
            {
                var message = $"✗ {testName} failed: {ex.Message}";
                Console.WriteLine(message);
                failedTests.Add(message);

                // Also add inner exception details if available
                if (ex.InnerException != null)
                {
                    failedTests.Add($"  Inner: {ex.InnerException.Message}");
                }
            }
        }

        if (failedTests.Any())
        {
            var failureMessage = $"The following tests failed:{Environment.NewLine}{string.Join(Environment.NewLine, failedTests)}";
            Assert.Fail(failureMessage);
        }
    }

    private void FindUnreferencedProjectsRecursively(Dictionary<VisualStudioProjectReference, List<string>> projects,
        DirectoryInfo dir)
    {
        var projFiles = dir.EnumerateFiles("*.csproj");

        foreach (var projFile in projFiles)
        {
            if (projFile.Directory.FullName.Contains("CodeTutorials"))
                continue;

            var key = projects.Keys.SingleOrDefault(p => (p.Name + ".csproj").Equals(projFile.Name));
            if (key == null)
                Error($"FAIL:Unreferenced csproj file spotted :{projFile.FullName}");
            else
                projects[key].Add(projFile.FullName);
        }

        foreach (var subdir in dir.EnumerateDirectories())
            FindUnreferencedProjectsRecursively(projects, subdir);
    }

    private void ProcessFolderRecursive(IEnumerable<VisualStudioSolutionFolder> folders,
        DirectoryInfo currentPhysicalDirectory)
    {
        //Process root folders
        foreach (var solutionFolder in folders)
        {
            var physicalSolutionFolder = currentPhysicalDirectory.EnumerateDirectories()
                .SingleOrDefault(d => d.Name.Equals(solutionFolder.Name));

            if (physicalSolutionFolder == null)
            {
                Error(
                    $"FAIL: Solution Folder exists called {solutionFolder.Name} but there is no corresponding physical folder in {currentPhysicalDirectory.FullName}");
                continue;
            }

            foreach (var p in solutionFolder.ChildrenProjects)
                FindProjectInFolder(p, physicalSolutionFolder);

            if (solutionFolder.ChildrenFolders.Any())
                ProcessFolderRecursive(solutionFolder.ChildrenFolders, physicalSolutionFolder);
        }
    }

    private void FindProjectInFolder(VisualStudioProjectReference p, DirectoryInfo physicalSolutionFolder)
    {
        var physicalProjectFolder =
            physicalSolutionFolder.EnumerateDirectories().SingleOrDefault(f => f.Name.Equals(p.Name));

        if (physicalProjectFolder == null)
        {
            Error($"FAIL: Physical folder {p.Name} does not exist in directory {physicalSolutionFolder.FullName}");
        }
        else
        {
            var csProjFile = physicalProjectFolder.EnumerateFiles("*.csproj").SingleOrDefault(f => f.Name.Equals(
                $"{p.Name}.csproj"));
            if (csProjFile == null)
            {
                Error(
                    $"FAIL: .csproj file {p.Name}.csproj was not found in folder {physicalProjectFolder.FullName}");
            }
            else
            {
                var tidy = new CsProjFileTidy(csProjFile);

                foreach (var str in tidy.UntidyMessages)
                    Error(str);

                foreach (var found in tidy.csFilesFound
                             .Where(found => _csFilesFound.Any(otherFile =>
                                 Path.GetFileName(otherFile).Equals(Path.GetFileName(found)))).Where(found =>
                                 !IgnoreList.Contains(Path.GetFileName(found))))
                    Error($"Found 2+ files called {Path.GetFileName(found)}");

                _csFilesFound.AddRange(tidy.csFilesFound);
            }
        }
    }

    private readonly List<string> _errors = new();

    private void Error(string s)
    {
        Console.WriteLine(s);
        _errors.Add(s);
    }
}

public class CopyrightHeaderEvaluator
{
    public static void FindProblems(List<string> csFilesFound)
    {
        var suggestedNewFileContents = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        var copyrightIssues = new Dictionary<string, (string actual, string expected)>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var file in csFilesFound)
        {
            if (file.Contains(".Designer.cs") || EvaluateNamespacesAndSolutionFoldersTests.IgnoreList.Contains(file))
                continue;

            var changes = false;

            var sbSuggestedText = new StringBuilder();

            var text = File.ReadLines(file).First();

            if (!text.StartsWith("// Copyright (c) The University of Dundee 2018-20")
                && text !=
                @"// This code is adapted from https://www.codeproject.com/Articles/1182358/Using-Autocomplete-in-Windows-Console-Applications")
            {
                changes = true;
                var expectedCopyright = $"// Copyright (c) The University of Dundee 2018-{DateTime.Now.Year}";
                copyrightIssues.Add(file, (text, expectedCopyright));

                sbSuggestedText.AppendLine(expectedCopyright);
                sbSuggestedText.AppendLine(@"// This file is part of the Research Data Management Platform (RDMP).");
                sbSuggestedText.AppendLine(
                    @"// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.");
                sbSuggestedText.AppendLine(
                    @"// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.");
                sbSuggestedText.AppendLine(
                    @"// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.");
                sbSuggestedText.AppendLine();
                sbSuggestedText.AppendJoin(Environment.NewLine, text);
            }

            if (changes)
                suggestedNewFileContents.Add(file, sbSuggestedText.ToString());
        }

        foreach (var kvp in suggestedNewFileContents)
        {
            var actualContents = File.ReadAllText(kvp.Key);
            var suggestedContents = kvp.Value;
            Assert.That(actualContents, Is.EqualTo(suggestedContents), $"Copyright header mismatch in {kvp.Key}");
        }
    }
}

public partial class AutoCommentsEvaluator
{
    public static void FindProblems(List<string> csFilesFound)
    {
        var suggestedNewFileContents = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var f in csFilesFound)
        {
            if (f.Contains(".Designer.cs"))
                continue;

            var changes = false;

            var sbSuggestedText = new StringBuilder();

            var text = File.ReadAllLines(f);
            var areInSummary = false;
            var paraOpened = false;

            for (var i = 0; i < text.Length; i++)
            {
                //////////////////////////////////No Mapping Properties////////////////////////////////////////////////////
                if (text[i].Trim().Equals("[NoMappingToDatabase]"))
                {
                    var currentClassName = GetUniqueTypeName(Path.GetFileNameWithoutExtension(f));

                    var t = MEF.GetType(currentClassName);

                    //if the previous line isn't a summary comment
                    if (!text[i - 1].Trim().StartsWith("///"))
                    {
                        var next = text[i + 1];

                        var m = PublicRegex().Match(next);
                        if (m.Success)
                        {
                            var whitespace = m.Groups[1].Value;
                            var member = m.Groups[3].Value;

                            Assert.Multiple(() =>
                            {
                                Assert.That(string.IsNullOrWhiteSpace(whitespace));
                                Assert.That(t, Is.Not.Null, $"MEF.GetType() returned null for class '{currentClassName}' in file '{f}'. The assembly containing this type may not be loaded.");
                            });

                            if (t.GetProperty($"{member}_ID") != null)
                            {
                                changes = true;
                                sbSuggestedText.AppendLine(whitespace + $"/// <inheritdoc cref=\"{member}_ID\"/>");
                            }
                            else
                            {
                                sbSuggestedText.AppendLine(text[i]);
                                continue;
                            }
                        }
                    }
                }


                if (text[i].Trim().Equals("/// <summary>"))
                {
                    areInSummary = true;
                    paraOpened = false;
                }

                if (text[i].Trim().Equals("/// </summary>"))
                {
                    if (paraOpened)
                    {
                        //
                        sbSuggestedText.Insert(sbSuggestedText.Length - 2, "</para>");
                        paraOpened = false;
                    }

                    areInSummary = false;
                }

                //if we have a paragraph break in the summary comments and the next line isn't an end summary
                if (areInSummary && text[i].Trim().Equals("///") && !text[i + 1].Trim().Equals("/// </summary>"))
                {
                    if (paraOpened)
                    {
                        sbSuggestedText.Insert(sbSuggestedText.Length - 2, "</para>");
                        paraOpened = false;
                    }

                    //there should be a para tag
                    if (!text[i + 1].Contains("<para>") && text[i + 1].Contains("///"))
                    {
                        changes = true;

                        //add current line
                        sbSuggestedText.AppendLine(text[i]);

                        //add the para tag
                        var commentStart = text[i + 1].IndexOf("///", StringComparison.Ordinal);
                        if (commentStart >= 0 && text[i + 1].Length > commentStart + 4)
                        {
                            var nextLine = text[i + 1].Insert(commentStart + 4, "<para>");
                            sbSuggestedText.AppendLine(nextLine);
                        }
                        else
                        {
                            // Line too short to insert para tag, just add as-is
                            sbSuggestedText.AppendLine(text[i + 1]);
                        }
                        i++;
                        paraOpened = true;
                        continue;
                    }
                }

                sbSuggestedText.AppendLine(text[i]);
            }

            if (changes)
                suggestedNewFileContents.Add(f, sbSuggestedText.ToString());
        }


        //drag your debugger stack pointer to here to mess up all your files to match the suggestedNewFileContents :)
        foreach (var kvp in suggestedNewFileContents)
        {
            var actualContents = File.ReadAllText(kvp.Key);
            var suggestedContents = kvp.Value;
            Assert.That(actualContents, Is.EqualTo(suggestedContents), $"Auto-comment mismatch in {kvp.Key}");
        }
    }

    private static string GetUniqueTypeName(string typename)
    {
        return typename switch
        {
            "ColumnInfo" => "Rdmp.Core.Curation.Data.ColumnInfo",
            "IFilter" => "Rdmp.Core.Curation.Data.IFilter",
            _ => typename
        };
    }

    [GeneratedRegex("(.*)public\\b(.*)\\s+(.*)\\b")]
    private static partial Regex PublicRegex();
}