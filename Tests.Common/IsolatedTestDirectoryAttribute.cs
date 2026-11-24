// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tests.Common;

/// <summary>
/// Gives each test a directory of its own for the files it creates, and deletes it when the test ends.  Apply this attribute to a
/// test assembly, then call <see cref="ForCurrentTest"/> from a test or its SetUp.
/// 
/// <para>No two tests share a directory, so tests cannot see each other's files whatever names they use and whether or not they
/// run at the same time.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IsolatedTestDirectoryAttribute : TestActionAttribute
{
    private const string RootName = "IsolatedTestDirectories";

    private static readonly ConcurrentDictionary<string, DirectoryInfo> Directories = new();

    /// <summary>
    /// Runs after each test (not after each fixture)
    /// </summary>
    public override ActionTargets Targets => ActionTargets.Test;

    /// <summary>
    /// Returns the directory of the test that is currently running, creating it (empty) the first time it is asked for
    /// </summary>
    public static DirectoryInfo ForCurrentTest()
    {
        var test = TestContext.CurrentContext.Test;

        return Directories.GetOrAdd(test.ID, _ =>
        {
            // the ID is unique within a run, the Guid keeps apart runs that share a work directory
            var path = Path.Join(TestContext.CurrentContext.WorkDirectory, RootName, $"{test.ID}-{Guid.NewGuid():N}");
            return Directory.CreateDirectory(path);
        });
    }

    /// <inheritdoc/>
    public override void AfterTest(ITest test)
    {
        if (!Directories.TryRemove(test.Id, out var directory))
            return;

        try
        {
            directory.Refresh();
            if (directory.Exists)
                directory.Delete(true);
        }
        catch (IOException e)
        {
            // a file the test left open should not fail a test that passed, but it should not go unnoticed either
            TestContext.Out.WriteLine($"Could not delete test directory '{directory.FullName}': {e.Message}");
        }
        catch (UnauthorizedAccessException e)
        {
            TestContext.Out.WriteLine($"Could not delete test directory '{directory.FullName}': {e.Message}");
        }
    }
}
