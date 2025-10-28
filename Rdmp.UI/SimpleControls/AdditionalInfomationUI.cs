// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.UI.TestsAndSetup.ServicePropogation;
using System;
using System.ComponentModel;

namespace Rdmp.UI.SimpleControls
{
    public partial class AdditionalInfomationUI : RDMPUserControl
    {
        private string _text = null;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TooltipText
        {
            get => _text;
            set
            {
                _text = value;
                toolTip1.SetToolTip(pictureBox1, _text);

            }
        }

        public AdditionalInfomationUI()
        {
            InitializeComponent();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Activator.Show(_text);
        }
    }
}
