// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.Databases;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Versioning;
using Rdmp.UI.LocationsMenu;

namespace Rdmp.UI.Tests.LocationsMenu;

/// <summary>
/// Tests that each dropdown and each Clear button of <see cref="ServerDefaultsUI"/> changes its own default server and
/// leaves the others alone.  All but one of the dropdowns share an event handler, as do all the Clear buttons.
/// </summary>
internal class ServerDefaultsUITests : UITests
{
    /// <summary>
    /// The controls <see cref="ServerDefaultsUI"/> has for one default, and what must have created a server for the
    /// dropdown to offer it (null if the dropdown offers every server)
    /// </summary>
    private sealed record ControlsFor(PermissableDefaults Default, string Dropdown, string ClearButton,
        IPatcher Patcher);

    private static readonly ControlsFor[] AllDefaults =
    {
        new(PermissableDefaults.LiveLoggingServer_ID, "ddDefaultLoggingServer", "btnClearLoggingServer",
            new LoggingDatabasePatcher()),
        new(PermissableDefaults.IdentifierDumpServer_ID, "ddDefaultIdentifierDump", "btnClearIdentifierDump",
            new IdentifierDumpDatabasePatcher()),
        new(PermissableDefaults.DQE, "ddDQEServer", "btnClearDQEServer", new DataQualityEnginePatcher()),
        new(PermissableDefaults.RAWDataLoadServer, "ddOverrideRawServer", "btnClearRAWServer", null),
        new(PermissableDefaults.ANOStore, "ddDefaultANOStore", "btnClearANOStore", new ANOStorePatcher()),
        new(PermissableDefaults.WebServiceQueryCachingServer_ID, "ddWebServiceQueryCacheServer",
            "btnClearWebServiceQueryCache", new QueryCachingPatcher()),
        new(PermissableDefaults.CohortIdentificationQueryCachingServer_ID, "ddCohortIdentificationQueryCacheServer",
            "btnClearCohortIdentificationQueryCache", new QueryCachingPatcher())
    };

    [Test]
    [UITimeout(50000)]
    public void Test_ServerDefaultsUI_ChoosingAServerSetsOnlyThatDefault()
    {
        var servers = CreateAServerForEachDefault("Chosen for");
        var expected = AllDefaults.ToDictionary(static d => d.Default, static _ => (ExternalDatabaseServer)null);
        SetDefaults(expected);

        using var ui = LaunchAndLoad();

        foreach (var d in AllDefaults)
        {
            var dropdown = GetControl<ComboBox>(d.Dropdown);
            Assert.That(dropdown.Items, Does.Contain(servers[d.Default]),
                $"{d.Dropdown} does not offer the server created for {d.Default}");

            dropdown.SelectedItem = servers[d.Default];

            expected[d.Default] = servers[d.Default];
            AssertDefaultsAre(expected, $"after choosing a server in {d.Dropdown}");
        }
    }

    [Test]
    [UITimeout(50000)]
    public void Test_ServerDefaultsUI_ClearClearsOnlyThatDefault()
    {
        var expected = CreateAServerForEachDefault("Cleared from");
        SetDefaults(expected);

        using var ui = LaunchAndLoad();

        foreach (var d in AllDefaults)
            Assert.That(GetControl<ComboBox>(d.Dropdown).SelectedItem, Is.EqualTo(expected[d.Default]),
                $"{d.Dropdown} should start on the default for {d.Default}");

        foreach (var d in AllDefaults)
        {
            Click(GetControl<Button>(d.ClearButton));

            expected[d.Default] = null;
            AssertDefaultsAre(expected, $"after clicking {d.ClearButton}");
            Assert.That(GetControl<ComboBox>(d.Dropdown).SelectedItem, Is.Null,
                $"{d.Dropdown} should be empty after clicking {d.ClearButton}");
        }
    }

    /// <summary>
    /// Creates a new server for each default, which the default's dropdown will offer.  The two query caching defaults
    /// get a server each although either dropdown offers both.
    /// </summary>
    /// <param name="namePrefix">Keeps the names apart from those of the other test's servers, which are still in the
    /// repository the tests share</param>
    private Dictionary<PermissableDefaults, ExternalDatabaseServer> CreateAServerForEachDefault(string namePrefix) =>
        AllDefaults.ToDictionary(static d => d.Default,
            d => new ExternalDatabaseServer(Repository, $"{namePrefix} {d.Default}", d.Patcher));

    /// <summary>
    /// Sets every default: the tests in this class share a repository, so each starts by saying what it expects
    /// </summary>
    private void SetDefaults(Dictionary<PermissableDefaults, ExternalDatabaseServer> defaults)
    {
        foreach (var (toSet, server) in defaults)
            Repository.SetDefault(toSet, server);
    }

    /// <summary>
    /// Launches <see cref="ServerDefaultsUI"/> and raises its Load event, which is when it fills in its dropdowns.
    /// Showing the Form would do that, but tests do not show Forms.
    /// </summary>
    private ServerDefaultsUI LaunchAndLoad()
    {
        var ui = new ServerDefaultsUI(ItemActivator);
        LastUserInterfaceLaunched = ui;

        // Loading also adds the Form to Application.OpenForms.  Only closing a Form that has been shown takes it out
        // again, so this one stays listed after the test disposes it.  Nothing in RDMP reads that list.
        var onLoad = typeof(Form).GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic,
            new[] { typeof(EventArgs) });
        Assert.That(onLoad, Is.Not.Null, "Form.OnLoad(EventArgs) not found");
        onLoad.Invoke(ui, BindingFlags.DoNotWrapExceptions, null, new object[] { EventArgs.Empty }, null);

        return ui;
    }

    private void AssertDefaultsAre(Dictionary<PermissableDefaults, ExternalDatabaseServer> expected, string when)
    {
        Assert.Multiple(() =>
        {
            foreach (var (toCheck, server) in expected)
                Assert.That(Repository.GetDefaultFor(toCheck), Is.EqualTo(server), $"Default for {toCheck} {when}");
        });
    }
}
