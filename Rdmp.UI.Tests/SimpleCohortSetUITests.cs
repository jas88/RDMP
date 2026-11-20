// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Reflection;
using NUnit.Framework;
using Rdmp.UI.Wizard;

namespace Rdmp.UI.Tests;

internal class SimpleCohortSetUITests : UITests
{
    [Test]
    [UITimeout(50000)]
    public void LockExtractionIdentifier_WithNoColumnSelected_DoesNothing()
    {
        var ui = new SimpleCohortSetUI();
        AndLaunch(ui);
        ui.SetupFor(ItemActivator);

        // no dataset has been chosen yet, so there is no column to lock in
        var click = typeof(SimpleCohortSetUI).GetMethod("btnLockExtractionIdentifier_Click",
            BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException("btnLockExtractionIdentifier_Click");

        Assert.DoesNotThrow(() => click.Invoke(ui, [null, EventArgs.Empty]));
    }
}
