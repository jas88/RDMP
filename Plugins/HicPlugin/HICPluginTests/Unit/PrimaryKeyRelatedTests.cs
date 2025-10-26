// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using NUnit.Framework;
using SCIStorePlugin.Data;

namespace SCIStorePluginTests.Unit;

public class PrimaryKeyRelatedTests
{
    [Test]
    public void TestIdenticallity_Identical()
    {
        var r1 = new SciStoreResult
        {
            LabNumber = "fish"
        };
        var r2 = new SciStoreResult
        {
            LabNumber = "fish"
        };


        Assert.That(r1.IsIdenticalTo(r2),Is.True);
    }
    [Test]
    public void TestIdenticallity_NotIdentical()
    {
        var r1 = new SciStoreResult
        {
            LabNumber = "fish",
            ReadCodeValue = "234"
        };
        var r2 = new SciStoreResult
        {
            LabNumber = "fish",
            ReadCodeValue = "2asd"
        };


        Assert.That(r1.IsIdenticalTo(r2), Is.False);
    }
}