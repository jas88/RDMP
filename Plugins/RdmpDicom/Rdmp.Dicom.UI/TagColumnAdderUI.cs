// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Windows.Forms;
using Rdmp.Core.Curation.Data;
using Rdmp.Dicom.TagPromotionSchema;
using System.ComponentModel;

namespace Rdmp.Dicom.UI;

public partial class TagColumnAdderUI : Form
{
    private readonly TableInfo _tableInfo;

    public TagColumnAdderUI(TableInfo tableInfo)
    {
        _tableInfo = tableInfo;
        InitializeComponent();

        cbxTag.AutoCompleteSource = AutoCompleteSource.ListItems;
        cbxTag.DataSource = TagColumnAdder.GetAvailableTags();
    }

    private void cbxTag_SelectedIndexChanged(object sender, EventArgs e)
    {
        ragSmiley1.Reset();
        try
        {
            var keyword = cbxTag.Text;
            var type = TagColumnAdder.GetDataTypeForTag(keyword, _tableInfo.GetQuerySyntaxHelper().TypeTranslater);

            var multiplicity = TagColumnAdder.GetTag(keyword).ValueMultiplicity;
            lblMultiplicity.Text = multiplicity == null ? "(Multiplicity:None)" : $"(Multiplicity: Min {multiplicity.Minimum} Max {multiplicity.Maximum} M {multiplicity.Multiplicity})";

            tbDataType.Text = type;
        }
        catch (Exception exception)
        {
            ragSmiley1.Fatal(exception);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ColumnName { get; private set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ColumnDataType { get; private set; }

    private void btnOk_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;



        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void cbxTag_TextChanged(object sender, EventArgs e)
    {
        ColumnName = cbxTag.Text;
    }

    private void tbDataType_TextChanged(object sender, EventArgs e)
    {
        ColumnDataType = tbDataType.Text;
    }
}