// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using NUnit.Framework;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data;
using Tests.Common;
using Rdmp.Core.DataLoad.Modules.Attachers;
using System.Linq;

namespace Rdmp.Core.Tests.CommandExecution
{
    public class ExecuteCommandCloneLoadMetadataTests: DatabaseTests
    {

        [Test]
        public void TestCloneLoadMetadata()
        {
            var lmd1 = new LoadMetadata(CatalogueRepository, "MyLmd");
            lmd1.Description = "Desc!";
            var cata = new Catalogue(CatalogueRepository, "myCata")
            {
                LoggingDataTask = "B"
            };
            cata.SaveToDatabase();
            lmd1.LinkToCatalogue(cata);
            var pt1 = new ProcessTask(CatalogueRepository, lmd1, LoadStage.Mounting)
            {
                ProcessTaskType = ProcessTaskType.Attacher,
                LoadStage = LoadStage.Mounting,
                Path = typeof(AnySeparatorFileAttacher).FullName
            };
            pt1.SaveToDatabase();

            pt1.CreateArgumentsForClassIfNotExists(typeof(AnySeparatorFileAttacher));
            var pta = pt1.ProcessTaskArguments.Single(pt => pt.Name == "Separator");
            pta.SetValue(",");
            pta.SaveToDatabase();
            LoadMetadata clonedLmd;
            clonedLmd = lmd1.Clone();
            Assert.That(clonedLmd.ProcessTasks.Count(), Is.EqualTo(1));
            Assert.That(clonedLmd.Description, Is.EqualTo(lmd1.Description));
            Assert.That(clonedLmd.ProcessTasks.First().ProcessTaskArguments.First().Value, Is.EqualTo(lmd1.ProcessTasks.First().ProcessTaskArguments.First().Value));
        }
    }
}
