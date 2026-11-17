// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.DataLoad.Modules.Attachers;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Rdmp.Core.Tests.DataLoad.Engine.Unit;

/// <summary>
/// Tests how <see cref="MDFAttacher"/> decides that a database file has already been copied (so need not be copied
/// again), without a database server.  The files' last write times are set explicitly: files written one after the
/// other may or may not get the same timestamp, depending on the file system's clock.
/// </summary>
internal class MDFAttacherFilesSimilarTests
{
    private static readonly DateTime LastWriteTimeUtc = new(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private DirectoryInfo _dir;

    [SetUp]
    public void CreateDirectory() =>
        _dir = Directory.CreateDirectory(Path.Join(TestContext.CurrentContext.WorkDirectory,
            $"{nameof(MDFAttacherFilesSimilarTests)}_{Guid.NewGuid():N}"));

    [TearDown]
    public void DeleteDirectory() => _dir.Delete(true);

    /// <summary>
    /// Large enough (at least 8K) for the start and end of the files to be compared
    /// </summary>
    private const int LargeLength = 3 * 4096;

    private static byte[] Content(int length) => Enumerable.Range(0, length).Select(static i => (byte)(i % 251)).ToArray();

    private string CreateFile(string name, byte[] content, DateTime lastWriteTimeUtc)
    {
        var path = Path.Join(_dir.FullName, name);
        File.WriteAllBytes(path, content);
        File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
        return path;
    }

    /// <summary>
    /// Calls FilesSimilar with a listener that throws on warnings, so a failure to compare the files fails the test
    /// rather than passing for 'not similar'
    /// </summary>
    private static bool FilesSimilar(string pathA, string pathB) =>
        new MDFAttacher().FilesSimilar(pathA, pathB, ThrowImmediatelyDataLoadEventListener.QuietPicky);

    [Test]
    public void FilesSimilar_SameContentAndLastWriteTime_True()
    {
        var a = CreateFile("a.mdf", Content(LargeLength), LastWriteTimeUtc);
        var b = CreateFile("b.mdf", Content(LargeLength), LastWriteTimeUtc);

        Assert.That(FilesSimilar(a, b), Is.True);
    }

    [Test]
    public void FilesSimilar_DifferentLastWriteTimes_False()
    {
        var a = CreateFile("a.mdf", Content(LargeLength), LastWriteTimeUtc);
        var b = CreateFile("b.mdf", Content(LargeLength), LastWriteTimeUtc.AddSeconds(1));

        Assert.That(FilesSimilar(a, b), Is.False);
    }

    [Test]
    public void FilesSimilar_SameButUnder8K_False()
    {
        // Just under 8K: both 4K blocks can still be read and match, so only the 8K minimum makes these not similar
        var a = CreateFile("a.mdf", Content(8191), LastWriteTimeUtc);
        var b = CreateFile("b.mdf", Content(8191), LastWriteTimeUtc);

        Assert.That(FilesSimilar(a, b), Is.False);
    }
}
