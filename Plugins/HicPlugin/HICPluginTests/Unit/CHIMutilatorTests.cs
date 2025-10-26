// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.Linq;
using HICPlugin.Mutilators;
using NUnit.Framework;
using Rdmp.Core.Curation.Data.DataLoad;
using Tests.Common;

namespace HICPluginTests.Unit;

class CHIMutilatorTests:UnitTests
{
    [Test]
    public void Test_CHIMutilator_Construction()
    {
        var lmd = new LoadMetadata(Repository,"My lmd");
        var pt = new ProcessTask(Repository, lmd, LoadStage.AdjustRaw);

        pt.CreateArgumentsForClassIfNotExists(typeof (CHIMutilator));

        //property defaults to true
        var addZero = pt.ProcessTaskArguments.Single(static a => a.Name.Equals("TryAddingZeroToFront"));
        Assert.That(addZero.GetValueAsSystemType(), Is.EqualTo(true));
    }
}