// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.VisualStudioSolutionFileProcessing;
using Rdmp.Core.Tests.DesignPatternTests.ClassFileEvaluation;
using Tests.Common;

namespace Rdmp.Core.Tests.DesignPatternTests;

/// <summary>
/// Repository hygiene checks driven by DataManagementPlatform.sln.  The solution is walked once per fixture (in
/// <see cref="WalkSolution"/>) collecting every .cs file belonging to a solution project, and each test then evaluates
/// one rule against that shared result.  None of these tests require a database.
/// </summary>
[TestFixture]
public class EvaluateNamespacesAndSolutionFoldersTests
{
    /// <summary>
    /// File names which may legitimately appear in more than one project
    /// </summary>
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

    private DirectoryInfo _solutionDir;
    private VisualStudioSolutionFile _sln;

    /// <summary>
    /// Every .cs file found in a solution project, in discovery order (see <see cref="FindProjectInFolder"/> for de-duplication)
    /// </summary>
    private readonly List<string> _csFilesFound = new();

    /// <summary>
    /// Problems with solution folders or projects not matching the physical directory layout
    /// </summary>
    private readonly List<string> _structureErrors = new();

    /// <summary>
    /// Problems with the content of csproj files
    /// </summary>
    private readonly List<string> _projectFileErrors = new();

    /// <summary>
    /// Problems with class names and namespaces not matching the file name and folder
    /// </summary>
    private readonly List<string> _classNamingErrors = new();

    [OneTimeSetUp]
    public void WalkSolution()
    {
        // Ensure all loaded assemblies' types are discoverable for the checks which use reflection
        MEF.RefreshTypes();

        _solutionDir = RepositoryLayout.FindRoot();
        _sln = new VisualStudioSolutionFile(_solutionDir,
            new FileInfo(Path.Join(_solutionDir.FullName, RepositoryLayout.SolutionFileName)));

        ProcessFolderRecursive(_sln.RootFolders, _solutionDir);

        foreach (var rootLevelProject in _sln.RootProjects)
            FindProjectInFolder(rootLevelProject, _solutionDir);
    }

    /// <summary>
    /// Solution folders and projects have matching physical folders, every csproj on disk is in the solution and no csproj
    /// appears in two places
    /// </summary>
    [Test]
    public void EvaluateSolutionStructure()
    {
        var errors = new List<string>(_structureErrors);
        ValidateProjectsOnDisk(errors);
        Assert.That(errors, Is.Empty, "Solution structure validation failed");
    }

    /// <summary>
    /// csproj files are tidy (e.g. do not use CopyToOutputDirectory Always)
    /// </summary>
    [Test]
    public void EvaluateProjectFileStructure() =>
        Assert.That(_projectFileErrors, Is.Empty, "Project file structure validation failed");

    /// <summary>
    /// Each .cs file has a single namespace matching its folder and its public class matches the file name
    /// </summary>
    [Test]
    public void EvaluateClassNamingAndNamespaces() =>
        Assert.That(_classNamingErrors, Is.Empty, "Class naming and namespace validation failed");

    /// <summary>
    /// Sanity check that the solution walk did not collect any file twice
    /// </summary>
    [Test]
    public void EvaluateDuplicateFiles()
    {
        var duplicatePaths = _csFilesFound.GroupBy(f => f)
            .Where(g => g.Count() > 1 && !IgnoreList.Contains(Path.GetFileName(g.Key)))
            .Select(g => $"Found duplicate file paths: {g.Key} (appears {g.Count()} times)");

        Assert.That(duplicatePaths, Is.Empty, "Duplicate files validation failed");
    }

    [Test]
    public void EvaluateInterfaceDeclarations() => InterfaceDeclarationsCorrect.FindProblems();

    [Test]
    public void EvaluateClassDocumentation() => new AllImportantClassesDocumented().FindProblems(_csFilesFound);

    [Test]
    public void EvaluateDocumentationCrossExamination() =>
        new DocumentationCrossExaminationTest(_solutionDir).FindProblems(_csFilesFound);

    [Test]
    public void EvaluateRelationshipProperties() =>
        new SuspiciousRelationshipPropertyUse().FindPropertyMisuse(_csFilesFound);

    [Test]
    public void EvaluateExplicitDatabaseNames() => ExplicitDatabaseNameChecker.FindProblems(_csFilesFound);

    /// <summary>
    /// RDMPForm and RDMPUserControl classes do not use RepositoryLocator in their constructors or LicenseManager.UsageMode
    /// </summary>
    [Test]
    public void EvaluateFormInitialization() =>
        new RDMPFormInitializationTests().FindUninitializedForms(_csFilesFound);

    [Test]
    public void EvaluateAutoComments() => AutoCommentsEvaluator.FindProblems(_csFilesFound);

    /// <summary>
    /// Every project in the solution (except the externals/ wrappers around vendored source) is found exactly once
    /// when traversing the repository, and there are no csproj files which are not in the solution
    /// </summary>
    private void ValidateProjectsOnDisk(List<string> errors)
    {
        var foundProjects = _sln.Projects
            .Where(project => !RepositoryLayout.IsExternalsProject(project.Path))
            .ToDictionary(project => project, _ => new List<string>());

        FindProjectsRecursively(foundProjects, _solutionDir, errors);

        foreach (var (project, locations) in foundProjects)
            switch (locations.Count)
            {
                case 0:
                    errors.Add(
                        $"FAIL: Did not find project {project.Name} while traversing solution directories and subdirectories");
                    break;
                case > 1:
                    errors.Add(
                        $"FAIL: Found 2+ copies of project {project.Name} while traversing solution directories and subdirectories:{Environment.NewLine}{string.Join(Environment.NewLine, locations)}");
                    break;
            }
    }

    private static void FindProjectsRecursively(Dictionary<VisualStudioProjectReference, List<string>> projects,
        DirectoryInfo dir, List<string> errors)
    {
        if (!dir.FullName.Contains("CodeTutorials"))
            foreach (var projFile in dir.EnumerateFiles("*.csproj"))
            {
                var key = projects.Keys.SingleOrDefault(p => $"{p.Name}.csproj".Equals(projFile.Name));
                if (key == null)
                    errors.Add($"FAIL:Unreferenced csproj file spotted :{projFile.FullName}");
                else
                    projects[key].Add(projFile.FullName);
            }

        // Skips build output, nested git checkouts (worktrees, submodules) and vendored third party source
        foreach (var subdir in RepositoryLayout.EnumerateDirectories(dir, true))
            FindProjectsRecursively(projects, subdir, errors);
    }

    private void ProcessFolderRecursive(IEnumerable<VisualStudioSolutionFolder> folders,
        DirectoryInfo currentPhysicalDirectory)
    {
        // Vendored third party source (externals solution folder) is exempt from hygiene rules
        foreach (var solutionFolder in folders.Where(static f =>
                     !f.Name.Equals(RepositoryLayout.ExternalsDirectoryName, StringComparison.Ordinal)))
        {
            var physicalSolutionFolder = currentPhysicalDirectory.EnumerateDirectories()
                .SingleOrDefault(d => d.Name.Equals(solutionFolder.Name));

            if (physicalSolutionFolder == null)
            {
                _structureErrors.Add(
                    $"FAIL: Solution Folder exists called {solutionFolder.Name} but there is no corresponding physical folder in {currentPhysicalDirectory.FullName}");
                continue;
            }

            foreach (var p in solutionFolder.ChildrenProjects)
                FindProjectInFolder(p, physicalSolutionFolder);

            ProcessFolderRecursive(solutionFolder.ChildrenFolders, physicalSolutionFolder);
        }
    }

    private void FindProjectInFolder(VisualStudioProjectReference p, DirectoryInfo physicalSolutionFolder)
    {
        var csProjFile = IsConsolidatedPluginProject(p.Name)
            ? FindConsolidatedPluginProject(p, physicalSolutionFolder)
            : FindProject(p, physicalSolutionFolder);

        if (csProjFile == null)
            return;

        var tidy = new CsProjFileTidy(csProjFile);
        _projectFileErrors.AddRange(tidy.ProjectFileProblems);
        _classNamingErrors.AddRange(tidy.SourceFileProblems);

        // Plugin projects (Plugins, Plugins.UI, Plugins.Tests...) legitimately share one directory, so only collect
        // their files once (from the base Plugins project).  Other projects only add files not already collected.
        if (IsConsolidatedPluginProject(p.Name))
        {
            if (p.Name == "Plugins")
                _csFilesFound.AddRange(tidy.csFilesFound);
            return;
        }

        _csFilesFound.AddRange(tidy.csFilesFound.Where(f => !_csFilesFound.Contains(f)).ToList());
    }

    private FileInfo FindProject(VisualStudioProjectReference p, DirectoryInfo physicalSolutionFolder)
    {
        var physicalProjectFolder =
            physicalSolutionFolder.EnumerateDirectories().SingleOrDefault(f => f.Name.Equals(p.Name));

        if (physicalProjectFolder == null)
        {
            _structureErrors.Add(
                $"FAIL: Physical folder {p.Name} does not exist in directory {physicalSolutionFolder.FullName}");
            return null;
        }

        var csProjFile = FindCsProj(physicalProjectFolder.FullName, p.Name);
        if (csProjFile == null)
            _structureErrors.Add(
                $"FAIL: .csproj file {p.Name}.csproj was not found in folder {physicalProjectFolder.FullName}");

        return csProjFile;
    }

    /// <summary>
    /// The consolidated plugin projects live under a Plugins directory, i.e. Plugins\Plugins\Plugins.csproj,
    /// Plugins\Plugins.UI\Plugins.UI.csproj etc.  Also tolerates the csproj directly in Plugins, in the solution
    /// folder or in a subdirectory of the solution folder named after the project.
    /// </summary>
    private FileInfo FindConsolidatedPluginProject(VisualStudioProjectReference p, DirectoryInfo physicalSolutionFolder)
    {
        var pluginsDir = Path.Join(physicalSolutionFolder.FullName, "Plugins");

        var csProjFile = FindCsProj(Path.Join(pluginsDir, p.Name), p.Name)
                         ?? FindCsProj(pluginsDir, p.Name)
                         ?? FindCsProj(physicalSolutionFolder.FullName, p.Name)
                         ?? FindCsProj(Path.Join(physicalSolutionFolder.FullName, p.Name), p.Name);

        if (csProjFile == null)
            _structureErrors.Add(
                $"FAIL: .csproj file {p.Name}.csproj was not found in {physicalSolutionFolder.FullName} or its Plugins subdirectory");

        return csProjFile;
    }

    private static FileInfo FindCsProj(string directory, string projectName) =>
        Directory.Exists(directory)
            ? new DirectoryInfo(directory).EnumerateFiles("*.csproj")
                .SingleOrDefault(f => f.Name.Equals($"{projectName}.csproj"))
            : null;

    private static bool IsConsolidatedPluginProject(string projectName) =>
        projectName is "Plugins" or "Plugins.UI" or "Plugins.Tests" or "Plugins.UI.Tests";
}
