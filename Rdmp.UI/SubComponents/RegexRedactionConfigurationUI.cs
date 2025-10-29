// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.UI.TestsAndSetup.ServicePropogation;
using System;
using System.ComponentModel;
using System.Windows.Forms;
using Rdmp.UI.Refreshing;
using Rdmp.UI.ItemActivation;
using Rdmp.Core.Curation.DataHelper.RegexRedaction;

namespace Rdmp.UI.SubComponents;
public partial class RegexRedactionConfigurationUI : RegexRedactionConfigurationUI_Design, IRefreshBusSubscriber
{
    public RegexRedactionConfigurationUI()
    {
        InitializeComponent();
    }

    public override void SetDatabaseObject(IActivateItems activator, RegexRedactionConfiguration databaseObject)
    {
        base.SetDatabaseObject(activator, databaseObject);

        Bind(tbName, "Text", "Name", static c => c.Name);
        Bind(tbRegexPattern, "Text", "RegexPattern", static c => c.RegexPattern);
        Bind(tbRedactionString, "Text", "RedactionString", static c => c.RedactionString);
        Bind(tbDescription, "Text", "Description", static c => c.Description);
        var s = GetObjectSaverButton();
        s.SetupFor(this, databaseObject, activator);
        GetObjectSaverButton()?.Enable(false);
    }


    public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
    {
    }

    private void label1_Click(object sender, EventArgs e)
    {

    }

    private void label4_Click(object sender, EventArgs e)
    {

    }

    private void label5_Click(object sender, EventArgs e)
    {

    }
}
[TypeDescriptionProvider(
    typeof(AbstractControlDescriptionProvider<RegexRedactionConfigurationUI_Design, UserControl>))]
public abstract class
    RegexRedactionConfigurationUI_Design : RDMPSingleDatabaseObjectControl<RegexRedactionConfiguration>
{
}