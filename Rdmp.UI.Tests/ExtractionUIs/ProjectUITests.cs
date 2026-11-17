// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;

namespace Rdmp.UI.Tests.ExtractionUIs;

/// <summary>
/// Tests that double clicking an extraction configuration's description or separator in <see cref="ProjectUI.ProjectUI"/>
/// asks for the new value through the activator, saves what is typed and changes nothing when the user cancels
/// </summary>
internal class ProjectUITests : UITests
{
    private const string OldDescription = "Old description";
    private const string OldSeparator = ",";

    private const string DescriptionPrompt = "Enter a Description for the Extraction:";
    private const string SeparatorPrompt = "Choose a character(s) separator";

    private ExtractionConfiguration _config;
    private DataGridView _grid;

    [Test]
    [UITimeout(50000)]
    public void Test_ProjectUI_DoubleClickDescription_SavesTypedText()
    {
        LaunchProjectUI();
        ItemActivator.TypeTextResponse = "New description";

        DoubleClickCell("Description");

        AssertAskedFor("Description", DescriptionPrompt, 1000, OldDescription);
        Assert.Multiple(() =>
        {
            Assert.That(_config.Description, Is.EqualTo("New description"));
            Assert.That(_config.HasLocalChanges().Evaluation, Is.EqualTo(ChangeDescription.NoChanges));
            Assert.That(CellValue("Description"), Is.EqualTo("New description"));
        });
    }

    [Test]
    [UITimeout(50000)]
    public void Test_ProjectUI_DoubleClickDescription_CancelChangesNothing()
    {
        LaunchProjectUI();
        ItemActivator.TypeTextResponse = null;

        DoubleClickCell("Description");

        AssertAskedFor("Description", DescriptionPrompt, 1000, OldDescription);
        Assert.Multiple(() =>
        {
            Assert.That(_config.Description, Is.EqualTo(OldDescription));
            Assert.That(_config.HasLocalChanges().Evaluation, Is.EqualTo(ChangeDescription.NoChanges));
            Assert.That(CellValue("Description"), Is.EqualTo(OldDescription));
        });
    }

    [Test]
    [UITimeout(50000)]
    public void Test_ProjectUI_DoubleClickSeparator_SavesTypedText()
    {
        LaunchProjectUI();
        ItemActivator.TypeTextResponse = "|";

        DoubleClickCell("Separator");

        AssertAskedFor("Separator", SeparatorPrompt, 3, OldSeparator);
        Assert.Multiple(() =>
        {
            Assert.That(_config.Separator, Is.EqualTo("|"));
            Assert.That(_config.HasLocalChanges().Evaluation, Is.EqualTo(ChangeDescription.NoChanges));
            Assert.That(CellValue("Separator"), Is.EqualTo("|"));
        });
    }

    [Test]
    [UITimeout(50000)]
    public void Test_ProjectUI_DoubleClickSeparator_CancelChangesNothing()
    {
        LaunchProjectUI();
        ItemActivator.TypeTextResponse = null;

        DoubleClickCell("Separator");

        AssertAskedFor("Separator", SeparatorPrompt, 3, OldSeparator);
        Assert.Multiple(() =>
        {
            Assert.That(_config.Separator, Is.EqualTo(OldSeparator));
            Assert.That(_config.HasLocalChanges().Evaluation, Is.EqualTo(ChangeDescription.NoChanges));
            Assert.That(CellValue("Separator"), Is.EqualTo(OldSeparator));
        });
    }

    /// <summary>
    /// Launches <see cref="ProjectUI.ProjectUI"/> for a new project with one saved extraction configuration
    /// </summary>
    private void LaunchProjectUI()
    {
        _config = WhenIHaveA<ExtractionConfiguration>();
        _config.Description = OldDescription;
        _config.Separator = OldSeparator;
        _config.SaveToDatabase();

        AndLaunch<ProjectUI.ProjectUI>((Project)_config.Project);

        _grid = GetControl<DataGridView>().Single(static g => g.Name == "dataGridView1");
        Assert.That(_grid.Rows, Has.Count.EqualTo(1), "Expected one row, for the project's extraction configuration");
    }

    /// <summary>
    /// Double clicks the cell in the extraction configuration's row and the given column, as the user would
    /// </summary>
    private void DoubleClickCell(string columnName)
    {
        // DataGridView raises CellDoubleClick from its protected OnCellDoubleClick when the user double clicks a cell
        var onCellDoubleClick = typeof(DataGridView).GetMethod("OnCellDoubleClick",
            BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(DataGridViewCellEventArgs) });
        Assert.That(onCellDoubleClick, Is.Not.Null, "DataGridView.OnCellDoubleClick(DataGridViewCellEventArgs) not found");

        onCellDoubleClick.Invoke(_grid,
            new object[] { new DataGridViewCellEventArgs(_grid.Columns[columnName].Index, RowIndex()) });
    }

    private int RowIndex() =>
        _grid.Rows.Cast<DataGridViewRow>().Single(r => r.Cells["ID"].Value?.ToString() == _config.ID.ToString())
            .Index;

    private object CellValue(string columnName) => _grid.Rows[RowIndex()].Cells[columnName].Value;

    private void AssertAskedFor(string title, string prompt, int maxLength, string initialText)
    {
        Assert.That(ItemActivator.LastTypeTextRequest, Is.Not.Null, "The user was not asked to type anything");

        var (args, actualMaxLength, actualInitialText, requireSaneHeaderText) = ItemActivator.LastTypeTextRequest.Value;
        Assert.Multiple(() =>
        {
            Assert.That(args.WindowTitle, Is.EqualTo(title));
            Assert.That(args.EntryLabel, Is.EqualTo(prompt));
            Assert.That(actualMaxLength, Is.EqualTo(maxLength));

            // ActivateItems shows a multi line editor for longer maximum lengths, these prompts are single line
            Assert.That(actualMaxLength, Is.LessThanOrEqualTo(BasicActivateItems.MultiLineLengthThreshold));
            Assert.That(actualInitialText, Is.EqualTo(initialText));
            Assert.That(requireSaneHeaderText, Is.False);
        });
    }
}
