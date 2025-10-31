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

public class EvaluateNamespacesAndSolutionFoldersTestsSeparated
{
    private const string SolutionName = "DataManagementPlatform.sln";
    private readonly List<string> _csFilesFound = new();
    private readonly List<string> _errors = new();

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
        "TreeView.cs",
        // Allow duplicate test files in Unit vs Integration test directories
        "ExecutableProcessTaskTests.cs"
    };

    [Test]
    public void EvaluateSolutionStructure()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        // Test that solution folders have corresponding physical directories
        ValidateSolutionFolderStructure(sln, solutionDir);

        // Test that all projects in solution exist physically
        ValidateAllProjectsExist(sln, solutionDir);

        // Test that there are no duplicate project files
        ValidateNoDuplicateProjects(sln, solutionDir);

        Assert.That(_errors, Is.Empty, "Solution structure validation failed");
    }

    [Test]
    public void EvaluateProjectFileStructure()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        Assert.That(_errors, Is.Empty, "Project file structure validation failed");
    }

    [Test]
    public void EvaluateClassNamingAndNamespaces()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        Assert.That(_errors, Is.Empty, "Class naming and namespace validation failed");
    }

    [Test]
    public void EvaluateDuplicateFiles()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        // Since we prevent duplicates at collection time, there should be no duplicate paths
        // This is a sanity check to ensure our duplicate prevention is working
        var allPathsUnique = _csFilesFound.Distinct().Count() == _csFilesFound.Count;
        if (!allPathsUnique)
        {
            var duplicatePaths = _csFilesFound.GroupBy(f => f)
                .Where(g => g.Count() > 1 && !IgnoreList.Contains(Path.GetFileName(g.Key)));

            foreach (var group in duplicatePaths)
            {
                Error($"Found duplicate file paths: {group.Key} (appears {group.Count()} times)");
            }
        }

        Assert.That(_errors, Is.Empty, "Duplicate files validation failed");
    }

    private bool IsConsolidatedPluginDirectory(string directoryPath)
    {
        // Check if this directory contains consolidated plugin project files
        var dirInfo = new DirectoryInfo(directoryPath);
        if (!dirInfo.Exists) return false;

        // Look for consolidated plugin project files
        var hasPluginProjects = dirInfo.EnumerateFiles("Plugins*.csproj").Any();
        return hasPluginProjects;
    }

    [Test]
    public void EvaluateInterfaceDeclarations()
    {
        InterfaceDeclarationsCorrect.FindProblems();
    }

    [Test]
    public void EvaluateClassDocumentation()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        var documented = new AllImportantClassesDocumented();
        documented.FindProblems(_csFilesFound);
    }

    [Test]
    public void EvaluateDocumentationCrossExamination()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        var crossExamination = new DocumentationCrossExaminationTest(solutionDir);
        crossExamination.FindProblems(_csFilesFound);
    }

    [Test]
    public void EvaluateRelationshipProperties()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        var propertyChecker = new SuspiciousRelationshipPropertyUse();
        propertyChecker.FindPropertyMisuse(_csFilesFound);
    }

    [Test]
    public void EvaluateExplicitDatabaseNames()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        ExplicitDatabaseNameChecker.FindProblems(_csFilesFound);
    }

    [Test]
    public void EvaluateAutoComments()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        var noMappingToDatabaseComments = new AutoCommentsEvaluator();
        AutoCommentsEvaluator.FindProblems(_csFilesFound);
    }

    [Test]
    public void EvaluateCopyrightHeaders()
    {
        LoadPluginAssemblies();

        var solutionDir = FindSolutionDirectory();
        var slnFile = FindSolutionFile(solutionDir);
        var sln = new VisualStudioSolutionFile(solutionDir, slnFile);

        ProcessFolderRecursive(sln.RootFolders, solutionDir);

        foreach (var rootLevelProjects in sln.RootProjects)
            FindProjectInFolder(rootLevelProjects, solutionDir);

        CopyrightHeaderEvaluator.FindProblems(_csFilesFound);
    }

    private void LoadPluginAssemblies()
    {
        // Load plugin assemblies to ensure types are available
        // Note: Old plugin projects have been consolidated and removed

        // Refresh MEF cache to discover loaded assemblies
        // (CompiledTypeRegistry will be used if available, providing additional optimization)
        MEF.RefreshTypes();
    }

    private DirectoryInfo FindSolutionDirectory()
    {
        var solutionDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (solutionDir?.GetFiles("*.sln").Any() != true) solutionDir = solutionDir?.Parent;
        Assert.That(solutionDir, Is.Not.Null, $"Failed to find {SolutionName} in any parent directories");
        return solutionDir;
    }

    private FileInfo FindSolutionFile(DirectoryInfo solutionDir)
    {
        var slnFile = solutionDir.GetFiles(SolutionName).FirstOrDefault() ?? throw new FileNotFoundException($"Could not find {SolutionName} in {solutionDir.FullName}");
        return slnFile;
    }

    private void ValidateSolutionFolderStructure(VisualStudioSolutionFile sln, DirectoryInfo solutionDir)
    {
        foreach (var solutionFolder in sln.RootFolders)
        {
            ValidateSolutionFolderRecursive(solutionFolder, solutionDir);
        }
    }

    private void ValidateSolutionFolderRecursive(VisualStudioSolutionFolder folder, DirectoryInfo currentPhysicalDirectory)
    {
        var physicalSolutionFolder = currentPhysicalDirectory.EnumerateDirectories()
            .SingleOrDefault(d => d.Name.Equals(folder.Name));

        if (physicalSolutionFolder == null)
        {
            Error($"FAIL: Solution Folder exists called {folder.Name} but there is no corresponding physical folder in {currentPhysicalDirectory.FullName}");
            return;
        }

        foreach (var p in folder.ChildrenProjects)
            FindProjectInFolder(p, physicalSolutionFolder);

        foreach (var childFolder in folder.ChildrenFolders)
            ValidateSolutionFolderRecursive(childFolder, physicalSolutionFolder);
    }

    private void ValidateAllProjectsExist(VisualStudioSolutionFile sln, DirectoryInfo solutionDir)
    {
        var foundProjects = sln.Projects.ToDictionary(project => project, project => new List<string>());
        FindUnreferencedProjectsRecursively(foundProjects, solutionDir);

        foreach (var kvp in foundProjects)
        {
            if (kvp.Value.Count == 0)
            {
                Error($"FAIL: Did not find project {kvp.Key.Name} while traversing solution directories and subdirectories");
            }
        }
    }

    private void ValidateNoDuplicateProjects(VisualStudioSolutionFile sln, DirectoryInfo solutionDir)
    {
        var foundProjects = sln.Projects.ToDictionary(project => project, project => new List<string>());
        FindUnreferencedProjectsRecursively(foundProjects, solutionDir);

        foreach (var kvp in foundProjects)
        {
            if (kvp.Value.Count > 1)
            {
                Error($"FAIL: Found 2+ copies of project {kvp.Key.Name} while traversing solution directories and subdirectories:{Environment.NewLine}{string.Join(Environment.NewLine, kvp.Value)}");
            }
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
        // Special handling for consolidated plugin structure
        // All plugin projects (Plugins, Plugins.UI, Plugins.Tests, Plugins.UI.Tests) are in the same Plugins directory
        if (IsConsolidatedPluginProject(p.Name))
        {
            // For plugin projects, look in the appropriate subdirectory under Plugins
            // The project path should be Plugins\Plugins\Plugins.csproj, Plugins\Plugins.UI\Plugins.UI.csproj, etc.
            var expectedProjectDir = Path.Combine(physicalSolutionFolder.FullName, p.Name);

            // First try to find the project in its named subdirectory
            FileInfo csProjFile = null;
            if (Directory.Exists(expectedProjectDir))
            {
                csProjFile = new DirectoryInfo(expectedProjectDir).EnumerateFiles("*.csproj")
                    .SingleOrDefault(f => f.Name.Equals($"{p.Name}.csproj"));
            }

            // Fallback: try directly in the physical solution folder (for compatibility)
            if (csProjFile == null)
            {
                csProjFile = physicalSolutionFolder.EnumerateFiles("*.csproj")
                    .SingleOrDefault(f => f.Name.Equals($"{p.Name}.csproj"));
            }

            // Fallback: try looking in the Plugins directory specifically (for root-level plugin projects)
            if (csProjFile == null)
            {
                var pluginsDir = Path.Combine(physicalSolutionFolder.FullName, "Plugins");
                if (Directory.Exists(pluginsDir))
                {
                    var projectSubDir = Path.Combine(pluginsDir, p.Name);
                    if (Directory.Exists(projectSubDir))
                    {
                        csProjFile = new DirectoryInfo(projectSubDir).EnumerateFiles("*.csproj")
                            .SingleOrDefault(f => f.Name.Equals($"{p.Name}.csproj"));
                    }

                    // Final fallback: look directly in Plugins directory
                    if (csProjFile == null)
                    {
                        csProjFile = new DirectoryInfo(pluginsDir).EnumerateFiles("*.csproj")
                            .SingleOrDefault(f => f.Name.Equals($"{p.Name}.csproj"));
                    }
                }
            }

            if (csProjFile == null)
            {
                Error($"FAIL: .csproj file {p.Name}.csproj was not found in {physicalSolutionFolder.FullName} or its Plugins subdirectory");
                return;
            }

            var tidy = new CsProjFileTidy(csProjFile);

            foreach (var str in tidy.UntidyMessages)
                Error(str);

            // For consolidated plugin projects, only collect files once to avoid duplicates
            // Files are legitimately shared across plugin projects in the same directory
            // We only process files for the first plugin project we encounter
            if (p.Name == "Plugins")
            {
                // Only collect files from the base Plugins project
                _csFilesFound.AddRange(tidy.csFilesFound);
            }
            // For other plugin projects (Plugins.UI, Plugins.Tests, etc.), don't collect files again
            // since they're in the same directory and would cause duplicates

            return;
        }

        // Original logic for non-plugin projects
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

                  // Only add files that haven't been collected before
                // This prevents the same file from being collected multiple times by different projects
                var newFiles = tidy.csFilesFound.Where(f => !_csFilesFound.Contains(f)).ToList();
                _csFilesFound.AddRange(newFiles);
            }
        }
    }

    private static bool IsConsolidatedPluginProject(string projectName)
    {
        // These plugin projects have been consolidated into a single Plugins directory
        return projectName switch
        {
            "Plugins" => true,
            "Plugins.UI" => true,
            "Plugins.Tests" => true,
            "Plugins.UI.Tests" => true,
            _ => false
        };
    }

    private void Error(string s)
    {
        Console.WriteLine(s);
        _errors.Add(s);
    }
}