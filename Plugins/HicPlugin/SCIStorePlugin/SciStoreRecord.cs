// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Annotations;

namespace SCIStorePlugin;

public sealed class SciStoreRecord : IEquatable<SciStoreRecord>
{
    public string CHI;

    public string LabNumber
    {
        get => _labNumber;
        set => _labNumber = UsefulStuff.RemoveIllegalFilenameCharacters(value);
    }

    public string TestReportID
    {
        get => _testReportId;
        set => _testReportId = UsefulStuff.RemoveIllegalFilenameCharacters(value);
    }

    public string ReportType;
    public string patientid;
    public string testid;
    public string name;
    private string _labNumber;
    private string _testReportId;

    public string Dept { get; set; }

    public static bool operator ==([CanBeNull] SciStoreRecord left, [CanBeNull] SciStoreRecord right)
    {
        return Equals(left, right);
    }

    public static bool operator !=([CanBeNull] SciStoreRecord left, [CanBeNull] SciStoreRecord right)
    {
        return !Equals(left, right);
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj) || obj is SciStoreRecord other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_labNumber, _testReportId);
    }

    public bool Equals(SciStoreRecord other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _labNumber == other._labNumber && _testReportId == other._testReportId;
    }
}