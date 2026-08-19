// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;

namespace Rdmp.Core.Tests.DesignPatternTests;

/// <summary>
/// Identifies vendored third party source which the repository hygiene tests must skip.
/// The externals directory at the repository root contains code we build from source but
/// do not author, e.g. the NPOI library (Apache 2.0 licensed, vendored as a git submodule),
/// and the build-standards directory is a shared-configuration git submodule.
/// RDMP conventions (copyright headers, namespace layout, solution membership,
/// documentation cross examination) do not apply to that code.
/// </summary>
public static class VendoredCode
{
    /// <summary>
    /// Name of the directory (at the repository root) which holds vendored third party source.
    /// </summary>
    public const string ExternalsDirectoryName = "externals";

    /// <summary>
    /// Directories (at the repository root) containing vendored code or shared configuration
    /// that repository hygiene rules do not apply to.
    /// </summary>
    private static readonly string[] VendoredDirectoryNames = { ExternalsDirectoryName, "build-standards" };

    /// <summary>
    /// True if the given absolute file or directory path lies underneath a vendored directory.
    /// </summary>
    public static bool IsVendored(string fullPath) =>
        VendoredDirectoryNames.Any(name =>
            fullPath.Contains($"{Path.DirectorySeparatorChar}{name}{Path.DirectorySeparatorChar}") ||
            fullPath.Contains($"/{name}/"));

    /// <summary>
    /// True if the given directory is itself one of the vendored directories.
    /// </summary>
    public static bool IsVendoredDirectory(DirectoryInfo dir) =>
        VendoredDirectoryNames.Contains(dir.Name, StringComparer.Ordinal);

    /// <summary>
    /// True if the given solution-relative csproj path (as written in the sln file) points
    /// into the externals directory.
    /// </summary>
    public static bool IsExternalsProject(string slnRelativePath) =>
        slnRelativePath.Replace('\\', '/').StartsWith($"{ExternalsDirectoryName}/", StringComparison.Ordinal);
}
