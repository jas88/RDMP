// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tests.Common;

namespace Rdmp.Core.Tests.DesignPatternTests;

/// <summary>
/// Tests that verify all C# source files in the repository (except build output, vendored code and nested git checkouts
/// such as worktrees) have proper copyright headers.
/// </summary>
[TestFixture]
[Category("Unit")]
public class CopyrightHeaderTests
{
    private const string ExpectedPrefix = "// Copyright (c) The University of Dundee 2018-20";

    [Test]
    public void AllSourceFiles_HaveCopyrightHeaders()
    {
        var root = RepositoryLayout.FindRoot();

        var missingHeaders = RepositoryLayout.EnumerateFiles(root, "*.cs")
            .Where(static f => !f.Name.Contains(".Designer.cs"))
            .Where(static f => !HasCopyrightHeader(f.FullName))
            .Select(f => RepositoryLayout.GetRelativePath(root, f.FullName))
            .OrderBy(static f => f, StringComparer.Ordinal)
            .ToList();

        Assert.That(missingHeaders, Is.Empty,
            $"The following files do not start with the RDMP copyright header:{Environment.NewLine}{ExpectedHeader()}");
    }

    private static bool HasCopyrightHeader(string file)
    {
        var firstLine = File.ReadLines(file).FirstOrDefault() ?? string.Empty;
        return firstLine.StartsWith(ExpectedPrefix, StringComparison.Ordinal);
    }

    private static string ExpectedHeader() =>
        string.Join(Environment.NewLine,
            $"// Copyright (c) The University of Dundee 2018-{DateTime.Now.Year}",
            "// This file is part of the Research Data Management Platform (RDMP).",
            "// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.",
            "// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.",
            "// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.");
}
