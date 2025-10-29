// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Rdmp.UI.AggregationUIs.Advanced
{
    public partial class AggregateGraphDateSelector : Form
    {
        public AggregateGraphDateSelector(string startDate = "", string endDate = "")
        {
          
            StartDate = startDate;
            EndDate = endDate;
            InitializeComponent();
            tbStartDate.Text = startDate;
            tbEndDate.Text = endDate;
            validateUpdate();
        }

        public string StartDate;
        public string EndDate;

        private bool validateDate(string date)
        {
            return DateRegex().Match(date.Trim()).Success;
        }


        private void validateUpdate()
        {
            var startDateValid = validateDate(tbStartDate.Text);
            var endDateValid = validateDate(tbEndDate.Text);
            if (!startDateValid)
            {
                tbStartDate.ForeColor = Color.Red;
            }
            else
            {
                tbStartDate.ForeColor = Color.Black;
            }
            if (!endDateValid)
            {
                tbEndDate.ForeColor = Color.Red;
            }
            else
            {
                tbEndDate.ForeColor = Color.Black;
            }
            if (!startDateValid || !endDateValid)
            {
                btnRefresh.Enabled = false;
            }
            else
            {
                btnRefresh.Enabled = true;
            }

        }

        private void AggregateGraphDateSelector_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void tbStartDate_TextChanged(object sender, EventArgs e)
        {
            StartDate = tbStartDate.Text;
            validateUpdate();

        }

        [GeneratedRegex("^'\\d{4}-(0?[1-9]|1[012])-(0?[1-9]|[12][0-9]|3[01])'$")]
        private static partial Regex DateRegex();

        private void tbEndDate_TextChanged(object sender, EventArgs e)
        {
            EndDate = tbEndDate.Text;
            validateUpdate();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
