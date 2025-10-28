// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Rdmp.UI.SimpleControls.MultiSelectChips
{
    public partial class FreeFormTextChipDisplay : UserControl
    {
        private string _value;


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                flowLayoutPanel1.Controls.Clear();
                var splitValues = _value.Split(',');
                foreach (var splitValue in splitValues.Where(sv => !string.IsNullOrWhiteSpace(sv)))
                {
                    flowLayoutPanel1.Controls.Add(new Chip(splitValue, Remove));
                }
            }
        }

        private int Remove(string value)
        {
            Value = string.Join(',', Value.Split(",").Where(v => v != value));
            return 1;
        }

        public FreeFormTextChipDisplay()
        {
            InitializeComponent();
        }

        private void textBox1_KeyPressed(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                if (!_value.Split(',').Contains(textBox1.Text))
                {
                    Value = Value + $"{(Value.Length > 0 ? "," : "")}{textBox1.Text.Trim()}";
                }
                textBox1.Text = "";
            }
        }
    }
}
