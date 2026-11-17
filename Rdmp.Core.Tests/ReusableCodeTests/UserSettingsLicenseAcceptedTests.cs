// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Licenses;
using Rdmp.Core.ReusableLibraryCode.Settings;

namespace Rdmp.Core.Tests.ReusableCodeTests;

/// <summary>
/// Tests how the acceptance of the licenses is recorded in the shared settings, or in the program's own settings when the shared ones cannot
/// be written.  The tests use their own shared settings folder, so that they never change the acceptance of the user running them.
/// </summary>
public class UserSettingsLicenseAcceptedTests
{
    private const string Current = "CURRENT";
    private const string Old = "OLD";

    private SharedUserSettings _originalSharedSettings;
    private string _folder;

    [SetUp]
    public void SetUp()
    {
        _originalSharedSettings = UserSettings.SharedSettings;
        _folder = Path.Join(TestContext.CurrentContext.WorkDirectory,
            $"{nameof(UserSettingsLicenseAcceptedTests)}_{Guid.NewGuid():N}");
    }

    [TearDown]
    public void TearDown()
    {
        // Forget anything a test recorded in this test program's own settings
        UserSettings.SharedSettings = new SharedUserSettings(_folder);
        UserSettings.LicenseAccepted = null;

        UserSettings.SharedSettings = _originalSharedSettings;

        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [TestCase(Current, null, Current)]
    [TestCase(null, Current, Current)]
    [TestCase(Current, Old, Current)]
    [TestCase(Old, Current, Current)]
    [TestCase(Old, null, Old)]
    [TestCase(null, Old, Old)]
    [TestCase(Old, "OLDER", Old)]
    [TestCase(null, null, null)]
    public void ChooseLicenseAccepted_EitherStoreCanHaveTheCurrentAcceptance(string shared, string own, string expected)
    {
        Assert.That(UserSettings.ChooseLicenseAccepted(shared, own, Current), Is.EqualTo(expected));
    }

    [Test]
    public void RecordLicenseAccepted_SharedSettingsWritable_ForgetsTheOwnCopy()
    {
        string shared = null;
        var own = Old;

        UserSettings.RecordLicenseAccepted(Current, hash => shared = hash, hash => own = hash);

        Assert.Multiple(() =>
        {
            Assert.That(shared, Is.EqualTo(Current));
            Assert.That(own, Is.Null);
        });
    }

    [TestCase(typeof(IOException))]
    [TestCase(typeof(UnauthorizedAccessException))]
    public void RecordLicenseAccepted_SharedSettingsUnwritable_NewAcceptanceWinsOverTheStaleSharedOne(Type error)
    {
        // The shared settings still hold an acceptance of older licenses and cannot be changed
        const string shared = Old;
        string own = null;

        UserSettings.RecordLicenseAccepted(Current, _ => throw (Exception)Activator.CreateInstance(error),
            hash => own = hash);

        Assert.Multiple(() =>
        {
            Assert.That(own, Is.EqualTo(Current));
            Assert.That(UserSettings.ChooseLicenseAccepted(shared, own, Current), Is.EqualTo(Current));
        });
    }

    [Test]
    public void RecordLicenseAccepted_OtherErrors_AreNotHidden()
    {
        Assert.Throws<InvalidOperationException>(() =>
            UserSettings.RecordLicenseAccepted(Current, static _ => throw new InvalidOperationException(), static _ => { }));
    }

    [Test]
    public void LicenseAccepted_IsStoredInTheSharedSettings()
    {
        var currentHash = new License(License.ThirdPartyLicenses).GetHashOfLicense();
        UserSettings.SharedSettings = new SharedUserSettings(_folder);

        UserSettings.LicenseAccepted = currentHash;

        Assert.Multiple(() =>
        {
            // As another RDMP program sees it
            Assert.That(new SharedUserSettings(_folder).Get("LicenseAccepted"), Is.EqualTo(currentHash));
            Assert.That(UserSettings.LicenseAccepted, Is.EqualTo(currentHash));
        });
    }

    [Test]
    public void LicenseAccepted_SharedSettingsUnavailable_IsStoredInTheProgramsOwnSettings()
    {
        var currentHash = new License(License.ThirdPartyLicenses).GetHashOfLicense();
        UserSettings.SharedSettings = new SharedUserSettings(null);

        UserSettings.LicenseAccepted = currentHash;

        Assert.That(UserSettings.LicenseAccepted, Is.EqualTo(currentHash));
    }
}
