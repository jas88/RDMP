// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.Logging;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Rdmp.Core.Tests.DataLoad.Engine.Unit;

[Category("Unit")]
public class DataLoadJobTests
{
    [Test]
    public void Constructor_WithoutLoadMetadata_Throws()
    {
        // the job reads the Catalogues to load, and their logging task, from the load metadata
        var ex = Assert.Throws<ArgumentNullException>(() => _ = new DataLoadJob(null, "Test job",
            Substitute.For<ILogManager>(), null, null, ThrowImmediatelyDataLoadEventListener.Quiet, null));

        Assert.That(ex?.ParamName, Is.EqualTo("loadMetadata"));
    }
}
