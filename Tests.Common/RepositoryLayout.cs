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
/// inside the repository (e.g. .claude/worktrees/*, externals/npoi, build-standards) are never mistaken for part of this checkout.
/// Symbolic links and junctions are not followed, so a scan cannot leave the checkout.</para>
/// <para>Hidden files and directories are otherwise included: directories starting with a dot count as hidden on Linux and macOS
/// but not on Windows, and content such as the Markdown in .github must be checked identically on every platform.</para>
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
    /// Options for listing the immediate content of one directory.  Only reparse points (symbolic links, junctions) are skipped;
    /// the default of also skipping hidden and system entries is deliberately not used (see the class summary), directories
    /// which must be skipped are identified by <see cref="IsExcludedDirectory"/> instead.
    /// </summary>
    private static readonly EnumerationOptions SingleDirectoryOptions = new()
    {
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = true,
        MatchCasing = MatchCasing.CaseInsensitive,
        RecurseSubdirectories = false
    };

    /// <summary>
    /// Walks up from the test assembly directory to the directory containing <see cref="SolutionFileName"/>
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">If no parent directory contains the solution file</exception>
    public static DirectoryInfo FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Join(dir.FullName, SolutionFileName)))
            dir = dir.Parent;

        return dir ?? throw new DirectoryNotFoundException(
            $"Could not find {SolutionFileName} in any parent directory of {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// True if <paramref name="dir"/> is the root of a git checkout (worktree, submodule or clone)
    /// </summary>
    public static bool IsGitCheckout(DirectoryInfo dir)
    {
        var dotGit = Path.Join(dir.FullName, ".git");
        return File.Exists(dotGit) || Directory.Exists(dotGit);
    }

    /// <summary>
    /// True if <paramref name="dir"/> is vendored code (externals or build-standards at the repository root) to which RDMP
    /// conventions do not apply
    /// </summary>
    public static bool IsVendoredDirectory(DirectoryInfo dir) =>
        VendoredDirectoryNames.Contains(dir.Name, StringComparer.Ordinal) &&
        dir.Parent != null && File.Exists(Path.Join(dir.Parent.FullName, SolutionFileName));

    /// <summary>
    /// True if a recursive scan of the repository should not descend into <paramref name="dir"/>.  Symbolic links and junctions
    /// are never followed (see <see cref="EnumerateDirectories"/>).  The repository root
    /// itself should not be passed in (it is a git checkout).
    /// </summary>
    /// <param name="dir">A directory below the repository root</param>
    /// <param name="excludeVendored">True to also skip vendored code, see <see cref="IsVendoredDirectory"/></param>
    public static bool IsExcludedDirectory(DirectoryInfo dir, bool excludeVendored) =>
        IgnoredDirectoryNames.Contains(dir.Name, StringComparer.OrdinalIgnoreCase) ||
        (excludeVendored && IsVendoredDirectory(dir)) ||
        IsGitCheckout(dir);

    /// <summary>
    /// Returns the immediate subdirectories of <paramref name="dir"/> which are not excluded (see <see cref="IsExcludedDirectory"/>),
    /// skipping inaccessible directories and reparse points (symbolic links, junctions) so a scan cannot escape the checkout
    /// </summary>
    public static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo dir, bool excludeVendored) =>
        dir.EnumerateDirectories("*", SingleDirectoryOptions).Where(d => !IsExcludedDirectory(d, excludeVendored));

    /// <summary>
    /// Recursively enumerates files matching <paramref name="searchPattern"/> (case insensitive) under <paramref name="root"/>,
    /// without descending into excluded directories (see <see cref="EnumerateDirectories"/>).  Files which are symbolic links
    /// are skipped.
    /// </summary>
    public static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo root, string searchPattern, bool excludeVendored)
    {
        var pending = new Stack<DirectoryInfo>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();

            foreach (var file in dir.EnumerateFiles(searchPattern, SingleDirectoryOptions))
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
