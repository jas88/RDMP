// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Settings;

namespace Rdmp.Core.Tests.ReusableCodeTests;

/// <summary>
/// Tests the settings which every RDMP program of a user shares, such as the hash of the licenses they accepted.  The tests use
/// their own folder, so that they never record an acceptance for the user running them.
/// </summary>
public class SharedUserSettingsTests
{
    private const string Key = "LicenseAccepted";
    private string _folder;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Join(TestContext.CurrentContext.WorkDirectory,
            $"{nameof(SharedUserSettingsTests)}_{Guid.NewGuid():N}");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Test]
    public void Get_NothingStored_ReturnsNull()
    {
        Assert.That(new SharedUserSettings(_folder).Get(Key), Is.Null);
    }

    [Test]
    public void Set_CreatesTheFolder_AndEveryProgramSeesTheValue()
    {
        new SharedUserSettings(_folder).Set(Key, "AA-BB");

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(Path.Join(_folder, Key)), Is.EqualTo("AA-BB"));
            Assert.That(new SharedUserSettings(_folder).Get(Key), Is.EqualTo("AA-BB"));
        });
    }

    [Test]
    public void Set_Null_RemovesTheValue()
    {
        var settings = new SharedUserSettings(_folder);
        settings.Set(Key, "AA-BB");
        settings.Set(Key, null);

        Assert.Multiple(() =>
        {
            Assert.That(settings.Get(Key), Is.Null);
            Assert.DoesNotThrow(() => settings.Set(Key, null));
        });
    }

    [Test]
    public void NoFolder_StoresNothing()
    {
        var settings = new SharedUserSettings(null);

        Assert.Multiple(() =>
        {
            Assert.That(settings.Get(Key), Is.Null);
            Assert.Throws<IOException>(() => settings.Set(Key, "AA-BB"));

            // Removing a setting succeeds, as there is none
            Assert.DoesNotThrow(() => settings.Set(Key, null));
        });
    }

    [Test]
    public void ForCurrentUser_DoesNotDependOnWhereTheProgramIs()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        var folder = SharedUserSettings.ForCurrentUser().Folder;

        if (string.IsNullOrEmpty(localApplicationData))
        {
            // A user with no local application data folder has nowhere to store shared settings
            Assert.That(folder, Is.Null);
            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(folder, Is.EqualTo(Path.Join(localApplicationData, "RDMP")));
            Assert.That(Path.IsPathRooted(folder));
            Assert.That(folder, Does.Not.StartWith(AppContext.BaseDirectory));
        });
    }
}
