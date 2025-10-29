// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.Core.Curation.DataHelper.RegexRedaction;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.TestsAndSetup.ServicePropogation;
using System;
using System.Drawing;
using System.Text.RegularExpressions;


namespace Rdmp.UI.SimpleDialogs.RegexRedactionConfigurationForm
{
    public partial class CreateNewRegexRedactionConfigurationUI : RDMPForm
    {
        private readonly IActivateItems _activator;

        public CreateNewRegexRedactionConfigurationUI(IActivateItems activator) : base(activator)
        {
            _activator = activator;
            InitializeComponent();
            btnCreate.Enabled = false;
            tbName.TextChanged += OnChange;
            tbRegexPattern.TextChanged += OnChange;
            tbRedactionString.TextChanged += OnChange;
            btnCancel.Click += Cancel;
            btnCreate.Click += Create;
            lblError.Text = "";
            lblError.Visible = false;
        }

        private void Cancel(object sender, EventArgs e)
        {
            Close();
        }
        private void Create(object sender, EventArgs e)
        {
            var config = new RegexRedactionConfiguration(_activator.RepositoryLocator.CatalogueRepository, tbName.Text, new Regex(tbRegexPattern.Text), tbRedactionString.Text, tbDescription.Text);
            config.SaveToDatabase();
            _activator.Publish(config);
            Close();
        }
        private bool ValidRegex()
        {
            var regexString = tbRegexPattern.Text;
            if (string.IsNullOrWhiteSpace(regexString)) return false;
            try
            {
                Regex.Match("", regexString);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        private void OnChange(object sender, EventArgs e)
        {
            var validRegex = ValidRegex();
            if (validRegex)
            {
                lblError.Text = "";
                lblError.Visible = false;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(tbRegexPattern.Text))
                {
                    lblError.Text = "Regex Pattern is invalid";
                    lblError.Visible = true;
                    lblError.ForeColor = Color.Red;
                }
            }
            if (!string.IsNullOrWhiteSpace(tbName.Text)
                && validRegex && !string.IsNullOrWhiteSpace(tbRedactionString.Text))
            {
                btnCreate.Enabled = true;
            }
            else
            {
                btnCreate.Enabled = false;
            }
        }
    }
}
