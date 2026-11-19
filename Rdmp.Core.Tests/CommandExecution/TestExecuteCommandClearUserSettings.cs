// Copyright (c) The University of Dundee 2018-2023
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.CommandLine.Interactive.Picking;
using Rdmp.Core.ReusableLibraryCode.Settings;

namespace Rdmp.Core.Tests.CommandExecution;

internal sealed class TestExecuteCommandClearUserSettings : CommandCliTests
{
    private SharedUserSettings _originalSharedSettings;
    private string _sharedFolder;

    [SetUp]
    public void UseOwnSharedSettings()
    {
        // Clearing the settings also clears the shared settings, which must not be those of the user running the tests
        _originalSharedSettings = UserSettings.SharedSettings;
        _sharedFolder = Path.Join(TestContext.CurrentContext.WorkDirectory,
            $"{nameof(TestExecuteCommandClearUserSettings)}_{Guid.NewGuid():N}");
        UserSettings.SharedSettings = new SharedUserSettings(_sharedFolder);
    }

    [TearDown]
    public void RestoreSharedSettings()
    {
        UserSettings.SharedSettings = _originalSharedSettings;

        if (Directory.Exists(_sharedFolder))
            Directory.Delete(_sharedFolder, true);
    }

    [Test]
    public void Test_ClearUserSettings()
    {
        var invoker = GetInvoker();
        var activator = GetActivator();

        UserSettings.Wait5SecondsAfterStartupUI = false;
        UserSettings.LicenseAccepted = "AA-BB";

        invoker.ExecuteCommand(typeof(ExecuteCommandSetUserSetting), new CommandLineObjectPicker(new[] { nameof(UserSettings.Wait5SecondsAfterStartupUI), "true" }, activator));

        Assert.That(UserSettings.Wait5SecondsAfterStartupUI);
        invoker.ExecuteCommand(typeof(ExecuteCommandSetUserSetting), new CommandLineObjectPicker(new[] { nameof(UserSettings.Wait5SecondsAfterStartupUI), "false" }, activator));
        Assert.That(UserSettings.Wait5SecondsAfterStartupUI, Is.False);
        Assert.That(new SharedUserSettings(_sharedFolder).Get("LicenseAccepted"), Is.EqualTo("AA-BB"));

        invoker.ExecuteCommand(typeof(ExecuteCommandClearUserSettings), new CommandLineObjectPicker(System.Array.Empty<string>(), activator));

        Assert.Multiple(() =>
        {
            Assert.That(UserSettings.Wait5SecondsAfterStartupUI);

            // The acceptance of the licenses, which the shared settings keep, is cleared too
            Assert.That(UserSettings.LicenseAccepted, Is.Null);
            Assert.That(new SharedUserSettings(_sharedFolder).Get("LicenseAccepted"), Is.Null);
        });
    }
}
