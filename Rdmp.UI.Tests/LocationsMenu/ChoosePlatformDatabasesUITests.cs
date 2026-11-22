// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using Rdmp.Core.CommandLine.DatabaseCreation;
using Rdmp.UI.LocationsMenu;

namespace Rdmp.UI.Tests.LocationsMenu;

/// <summary>
/// Tests the seed, number of people and number of rows boxes that <see cref="ChoosePlatformDatabasesUI"/> has for the
/// example datasets.  They share a text changed handler: each must set only its own number, turning red and
/// leaving the number alone when what is typed is not a number.
/// </summary>
internal class ChoosePlatformDatabasesUITests : UITests
{
    /// <summary>
    /// Each box and the private field that holds its number, which only creating the databases reads
    /// </summary>
    private static readonly (string Box, string Field)[] NumberBoxes =
    {
        ("tbSeed", "_seed"),
        ("tbPeopleCount", "_peopleCount"),
        ("tbRowCount", "_rowCount")
    };

    [Test]
    [UITimeout(50000)]
    public void Test_ChoosePlatformDatabasesUI_EachNumberBoxSetsOnlyItsOwnNumber()
    {
        using var ui = new ChoosePlatformDatabasesUI(RepositoryLocator);

        //pretend like we launched it
        LastUserInterfaceLaunched = ui;

        // the boxes are for the example datasets of new platform databases
        Click(GetControl<Button>("btnCreateNew"));
        Click(GetControl<CheckBox>("cbCreateExampleDatasets"));

        var expected = new Dictionary<string, int>
        {
            { "_seed", 500 },
            { "_peopleCount", ExampleDatasetsCreation.NumberOfPeople },
            { "_rowCount", ExampleDatasetsCreation.NumberOfRowsPerDataset }
        };
        AssertNumbersAre(ui, expected, "when launched");

        var typed = 1000;
        foreach (var (boxName, field) in NumberBoxes)
        {
            var box = GetControl<TextBox>(boxName);
            Assert.That(box.Enabled, $"{boxName} should be enabled once example datasets are wanted");

            box.Text = "lots";
            Assert.That(box.ForeColor, Is.EqualTo(Color.Red), $"Colour of {boxName} when it is not a number");
            AssertNumbersAre(ui, expected, $"after typing a word into {boxName}");

            typed++;
            box.Text = typed.ToString();
            expected[field] = typed;
            Assert.That(box.ForeColor, Is.EqualTo(Color.Black), $"Colour of {boxName} when it is a number");
            AssertNumbersAre(ui, expected, $"after typing {typed} into {boxName}");
        }
    }

    private static void AssertNumbersAre(ChoosePlatformDatabasesUI ui, Dictionary<string, int> expected, string when)
    {
        Assert.Multiple(() =>
        {
            foreach (var (fieldName, number) in expected)
            {
                var field = typeof(ChoosePlatformDatabasesUI).GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, $"ChoosePlatformDatabasesUI has no field called {fieldName}");

                Assert.That(field?.GetValue(ui), Is.EqualTo(number), $"{fieldName} {when}");
            }
        });
    }
}
