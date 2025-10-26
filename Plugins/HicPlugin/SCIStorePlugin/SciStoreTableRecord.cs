// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;

namespace SCIStorePlugin;

public class SciStoreTableRecord : IEquatable<SciStoreTableRecord>
{
    public string DatabaseName; // if different from Discipline
    public string HeaderTable;
    public string SamplesTable;
    public string ResultsTable;
    public string TestCodesTable;
    public string SampleTypesTable;

    public override bool Equals(object o)
    {
        if (o is null) return false;
        if (ReferenceEquals(this, o)) return true;
        if (o.GetType()!=GetType()) return false;
        if (o is not SciStoreTableRecord other) return false;
        return Equals(other);
    }

    public bool Equals(SciStoreTableRecord other)
    {
        if (other is null) return false;
        return string.Equals(DatabaseName, other.DatabaseName) && string.Equals(HeaderTable, other.HeaderTable) && string.Equals(SamplesTable, other.SamplesTable) && string.Equals(ResultsTable, other.ResultsTable) && string.Equals(TestCodesTable, other.TestCodesTable) && string.Equals(SampleTypesTable, other.SampleTypesTable);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DatabaseName, HeaderTable, SamplesTable, ResultsTable, TestCodesTable,
            SampleTypesTable);
    }
}