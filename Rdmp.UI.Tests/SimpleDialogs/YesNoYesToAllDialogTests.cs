// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using Rdmp.UI.SimpleDialogs;

namespace Rdmp.UI.Tests.SimpleDialogs;

/// <summary>
/// Tests that each of the four buttons of <see cref="YesNoYesToAllDialog"/>, which share a click handler, gives its own
/// answer and closes the dialog, and that only the two 'To All' buttons make the dialog answer later questions itself
/// </summary>
internal class YesNoYesToAllDialogTests : UITests
{
    [Test]
    [UITimeout(50000)]
    public void Test_YesNoYesToAllDialog_EachButtonGivesItsOwnAnswerAndClosesTheDialog()
    {
        AssertClicking("Yes", DialogResult.Yes, false, false);
        AssertClicking("Yes To All", DialogResult.Yes, true, false);
        AssertClicking("No", DialogResult.No, false, false);
        AssertClicking("No To All", DialogResult.No, false, true);
    }

    /// <summary>
    /// Clicks the button labelled <paramref name="buttonText"/> on a new dialog.  The dialog is not shown: a click
    /// handler closes it, so it must not be shown again afterwards either.
    /// </summary>
    /// <param name="buttonText">The label of the button to click</param>
    /// <param name="expectedAnswer">The answer the click should give</param>
    /// <param name="yesToAll">True if the dialog should now answer Yes to later questions without asking</param>
    /// <param name="noToAll">True if the dialog should now answer No to later questions without asking</param>
    private void AssertClicking(string buttonText, DialogResult expectedAnswer, bool yesToAll, bool noToAll)
    {
        using var dialog = new YesNoYesToAllDialog();

        //pretend like we launched it
        LastUserInterfaceLaunched = dialog;

        // the buttons have no Name, the dialog creates them in code
        Click(GetControl<Button>().Single(b => b.Text == buttonText));

        Assert.Multiple(() =>
        {
            Assert.That(dialog.DialogResult, Is.EqualTo(expectedAnswer), $"Answer given by '{buttonText}'");

            // asking a later question is the only public way to see these, and would show the dialog
            Assert.That(GetFlag(dialog, "YesToAllClicked"), Is.EqualTo(yesToAll),
                $"Whether '{buttonText}' answers Yes to later questions");
            Assert.That(GetFlag(dialog, "NoToAllClicked"), Is.EqualTo(noToAll),
                $"Whether '{buttonText}' answers No to later questions");

            Assert.That(dialog.IsDisposed, $"'{buttonText}' should have closed the dialog");
        });
    }

    /// <summary>
    /// Returns the value of the private bool field of <paramref name="dialog"/> called <paramref name="fieldName"/>
    /// </summary>
    private static object GetFlag(YesNoYesToAllDialog dialog, string fieldName)
    {
        var field = typeof(YesNoYesToAllDialog).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"YesNoYesToAllDialog has no field called {fieldName}");

        return field?.GetValue(dialog);
    }
}
