// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using BrightIdeasSoftware;
using NUnit.Framework;
using Rdmp.Core.Providers.Nodes;
using Rdmp.UI.Collections;
using System;
using System.Linq;
using System.Windows.Forms;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core;

namespace Rdmp.UI.Tests;

internal class ArbitraryFolderNodeTests : UITests
{
    [Test]
    [UITimeout(50000)]
    public void Test_ArbitraryFolderNode_CommandGetter_Throwing()
    {
        var tlv = new TreeListView();
        var common = new RDMPCollectionCommonFunctionality();
        common.SetUp(RDMPCollection.None, tlv, ItemActivator, null, null);

        var node = new ArbitraryFolderNode("my node", 0);

        var menu1 = common.GetMenuIfExists(node);
        Assert.That(menu1, Is.Not.Null);
        var baseItems = menu1.Items.Cast<ToolStripItem>().Select(i => i.Text).ToList();

        // Baseline menu should have Tree submenu, ShowKeywordHelp
        Assert.That(baseItems, Does.Contain("Tree"), "Expected Tree submenu in baseline menu");
        Assert.That(baseItems.Any(t => t?.Contains("Keyword Help") == true), Is.True, "Expected ShowKeywordHelp in baseline menu");

        //set the menu to have one command in it
        node.CommandGetter = () => new IAtomicCommand[] { new ImpossibleCommand("Do Nothing") };

        var menu2 = common.GetMenuIfExists(node);
        var updatedItems = menu2.Items.Cast<ToolStripItem>().ToList();

        // Expect "Do Nothing" command from CommandGetter
        Assert.That(updatedItems.Any(i => i.Text == "Do Nothing"), Is.True,
            "Expected 'Do Nothing' command from CommandGetter in menu");

        // Expect a separator between CommandGetter commands (bucket -1) and other items (bucket 0+)
        // CommandGetter commands get Weight -1.0f, which creates bucket -1, causing OrderMenuItems
        // to add a separator before items in bucket 0.
        var separatorCount = updatedItems.Count(i => i is ToolStripSeparator);
        var baseSeparatorCount = menu1.Items.Cast<ToolStripItem>().Count(i => i is ToolStripSeparator);
        Assert.That(separatorCount, Is.EqualTo(baseSeparatorCount + 1),
            "Expected one additional separator between CommandGetter commands and other menu items");

        //what happens if the delegate crashes?
        node.CommandGetter = () => throw new NotSupportedException("It went wrong!");

        Assert.DoesNotThrow(() => common.GetMenuIfExists(node));

        AssertErrorWasShown(ExpectedErrorType.GlobalErrorCheckNotifier,
            "Failed to build menu for my node of Type Rdmp.Core.Providers.Nodes.ArbitraryFolderNode");
    }
}