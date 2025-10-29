// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad.Modules.Attachers;
using Rdmp.Core.Repositories;
using System.Collections.Generic;
using System.Linq;
using Tests.Common;

namespace Rdmp.Core.Tests.DataLoad.Modules.Attachers
{
    public class AttacherMEFTest: UnitTests
    {

        [Test]
        public void AttacherMEFCreationTest()
        {
            var types = MEF.GetTypes<IAttacher>().Where(t => !typeof(RemoteAttacher).IsAssignableTo(t)).ToArray();
            List<string> AttacherPaths = types.Select(t => t.FullName).ToList();
            foreach (var path in AttacherPaths)
            {
                Assert.DoesNotThrow(() =>
                {
                    MEF.CreateA<IAttacher>(path);
                });
            }
        }
    }
}
