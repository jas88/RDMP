// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.Data;
using System.Windows.Forms;

namespace HICPluginInteractive.UIComponents;

/// <summary>
/// Allows you to view DataTable data, with option to specify current cell.
/// </summary>
public partial class ExtractDataTableViewer : UserControl
{
    private readonly string _colName;
    private readonly int? _rowIndex;

    public ExtractDataTableViewer(DataTable source, string caption, string colName, int? rowIndex)
    {
        _colName = colName;
        _rowIndex = rowIndex;
        InitializeComponent();
            
        Text = caption;
        dataGridView1.DataSource = source;
    }

    private void ExtractDataTableViewer_Load(object sender, System.EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_colName) && _rowIndex.HasValue)
            dataGridView1.CurrentCell = dataGridView1[_colName, _rowIndex.Value];
    }
}