// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tests.Common;

/// <summary>
/// Locates the root of the RDMP checkout the tests are running from and enumerates the files in it for the repository
/// structure and documentation tests.
/// <para>Enumeration never descends into build output (bin, obj), tool state (.git, .claude, .vs, .idea, node_modules) or any
/// nested directory which is itself a git checkout (i.e. contains a .git file or directory), so that git worktrees and submodules
/// inside the repository (e.g. .claude/worktrees/*, externals/npoi, build-standards) are never mistaken for part of this checkout.</para>
/// <para>Optionally it also skips vendored code (the externals and build-standards directories) to which RDMP conventions do not apply.</para>
/// </summary>
public static class RepositoryLayout
{
    /// <summary>
    /// The solution file which marks the root of the repository
    /// </summary>
    public const string SolutionFileName = "DataManagementPlatform.sln";

    /// <summary>
    /// Name of the directory (at the repository root) which holds vendored third party source and the wrapper
    /// projects that build it (e.g. the Apache 2.0 licensed NPOI library)
    /// </summary>
    public const string ExternalsDirectoryName = "externals";

    private static readonly string[] VendoredDirectoryNames = { ExternalsDirectoryName, "build-standards" };

    private static readonly string[] IgnoredDirectoryNames =
        { ".git", ".claude", ".vs", ".idea", "node_modules", "bin", "obj" };

    /// <summary>
    /// Walks up from the test assembly directory to the directory containing <see cref="SolutionFileName"/>
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">If no parent directory contains the solution file</exception>
    public static DirectoryInfo FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
            dir = dir.Parent;

        return dir ?? throw new DirectoryNotFoundException(
            $"Could not find {SolutionFileName} in any parent directory of {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// True if <paramref name="dir"/> is the root of a git checkout (worktree, submodule or clone)
    /// </summary>
    public static bool IsGitCheckout(DirectoryInfo dir)
    {
        var dotGit = Path.Combine(dir.FullName, ".git");
        return File.Exists(dotGit) || Directory.Exists(dotGit);
    }

    /// <summary>
    /// True if <paramref name="dir"/> is vendored code (externals or build-standards) to which RDMP conventions do not apply
    /// </summary>
    public static bool IsVendoredDirectory(DirectoryInfo dir) =>
        VendoredDirectoryNames.Contains(dir.Name, StringComparer.Ordinal);

    /// <summary>
    /// True if a recursive scan of the repository should not descend into <paramref name="dir"/>.  The repository root
    /// itself should not be passed in (it is a git checkout).
    /// </summary>
    /// <param name="dir">A directory below the repository root</param>
    /// <param name="excludeVendored">True to also skip vendored code, see <see cref="IsVendoredDirectory"/></param>
    public static bool IsExcludedDirectory(DirectoryInfo dir, bool excludeVendored) =>
        IgnoredDirectoryNames.Contains(dir.Name, StringComparer.OrdinalIgnoreCase) ||
        (excludeVendored && IsVendoredDirectory(dir)) ||
        IsGitCheckout(dir);

    /// <summary>
    /// Returns the immediate subdirectories of <paramref name="dir"/> which are not excluded, see <see cref="IsExcludedDirectory"/>
    /// </summary>
    public static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo dir, bool excludeVendored) =>
        dir.EnumerateDirectories().Where(d => !IsExcludedDirectory(d, excludeVendored));

    /// <summary>
    /// Recursively enumerates files matching <paramref name="searchPattern"/> (case insensitive) under <paramref name="root"/>,
    /// without descending into excluded directories, see <see cref="IsExcludedDirectory"/>
    /// </summary>
    public static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo root, string searchPattern, bool excludeVendored)
    {
        var options = new EnumerationOptions
            { MatchCasing = MatchCasing.CaseInsensitive, IgnoreInaccessible = true, RecurseSubdirectories = false };

        var pending = new Stack<DirectoryInfo>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();

            foreach (var file in dir.EnumerateFiles(searchPattern, options))
                yield return file;

            foreach (var sub in EnumerateDirectories(dir, excludeVendored))
                pending.Push(sub);
        }
    }

    /// <summary>
    /// Returns <paramref name="fullPath"/> relative to <paramref name="root"/> using '/' as the separator
    /// </summary>
    public static string GetRelativePath(DirectoryInfo root, string fullPath) =>
        Path.GetRelativePath(root.FullName, fullPath).Replace('\\', '/');

    /// <summary>
    /// True if the given solution relative csproj path (as written in the sln file) points into the externals directory
    /// </summary>
    public static bool IsExternalsProject(string slnRelativePath) =>
        slnRelativePath.Replace('\\', '/').StartsWith($"{ExternalsDirectoryName}/", StringComparison.Ordinal);
}
