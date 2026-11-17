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
/// Tests the messages <see cref="MDFAttacher"/> logs when it copies a database file into place, without a database
/// server.
/// </summary>
internal class MDFAttacherCopyIfNeededTests
{
    private DirectoryInfo _dir;

    [SetUp]
    public void CreateDirectory() =>
        _dir = Directory.CreateDirectory(Path.Join(TestContext.CurrentContext.WorkDirectory,
            $"{nameof(MDFAttacherCopyIfNeededTests)}_{Guid.NewGuid():N}"));

    [TearDown]
    public void DeleteDirectory() => _dir.Delete(true);

    [Test]
    public void CopyIfNeeded_DestinationExists_WarningNamesTheFile()
    {
        var src = Path.Join(_dir.FullName, "src.mdf");
        var dest = Path.Join(_dir.FullName, "dest.mdf");
        File.WriteAllText(src, "new");
        File.WriteAllText(dest, "old");

        var listener = new ToMemoryDataLoadEventListener(true);
        new MDFAttacher().CopyIfNeeded(src, dest, listener);

        var warnings = listener.GetAllMessagesByProgressEventType()[ProgressEventType.Warning]
            .Select(static e => e.Message);

        Assert.Multiple(() =>
        {
            Assert.That(warnings, Is.EqualTo(new[] { $"Overwriting existing database file '{dest}'" }));
            Assert.That(File.ReadAllText(dest), Is.EqualTo("new"));
        });
    }
}
