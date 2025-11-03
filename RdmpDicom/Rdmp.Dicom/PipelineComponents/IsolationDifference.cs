// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;

namespace Rdmp.Dicom.PipelineComponents;

/// <summary>
/// Represents a difference between a master record and an isolated record, tracking which columns conflict for primary key collision resolution
/// </summary>
public class IsolationDifference
{
    public string Pk { get; set; }

    public int RowIndex { get; set; }

    public bool IsMaster { get; set; }

    public List<string> ConflictingColumns { get; set; } = new();

    public IsolationDifference(int rowIndex, string pk , bool isMaster)
    {
        RowIndex = rowIndex;
        Pk = pk;
        IsMaster = isMaster;
    }

}