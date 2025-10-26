// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.IO;
using System.Linq;
using DrsPlugin.Attachers;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace DrsPluginTests;

public class SourceArchiveTests
{
    [Test]
    public void TestFilesystemArchiveProvider()
    {
        var rootDirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var rootDir = Directory.CreateDirectory(rootDirPath);

        try
        {
            // Create some fake directories and files
            var subdir1 = rootDir.CreateSubdirectory("Subdir1");
            var subdir2 = rootDir.CreateSubdirectory("Subdir2");

            File.WriteAllText(Path.Combine(subdir1.FullName, "file1.txt"), "test");
            File.WriteAllText(Path.Combine(subdir1.FullName, "file2.txt"), "");
            File.WriteAllText(Path.Combine(subdir2.FullName, "file3.txt"), "");
            File.WriteAllText(Path.Combine(subdir1.FullName, "not-part-of-archive.foo"), "");

            var archiveProvider = new FilesystemArchiveProvider(rootDirPath, new[] {".txt"}, ThrowImmediatelyDataLoadEventListener.Quiet);
            var entryNames = archiveProvider.EntryNames.ToList();

            Assert.That(entryNames.Contains("file1.txt"), Is.True);
            Assert.That(entryNames.Contains("file2.txt"), Is.True);
            Assert.That(entryNames.Contains("file3.txt"), Is.True);
            Assert.That(entryNames.Contains("not-part-of-archive.foo"), Is.False);

            using var file1 = archiveProvider.EntryStreams.Single(kvp => kvp.Key == "file1.txt").Value;
            using var reader = new StreamReader(file1);
           Assert.That("test", Is.EqualTo(reader.ReadLine()));
        }
        finally
        {
            rootDir.Delete(true);
        }
    }
}