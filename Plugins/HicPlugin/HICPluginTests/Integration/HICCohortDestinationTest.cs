// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Data;
using HICPlugin.DataFlowComponents;
using NUnit.Framework;
using Rdmp.Core.CohortCommitting.Pipeline;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace HICPluginTests.Integration;

public class HICCohortDestinationTest : TestsRequiringACohort
{
    [TestCase(true)]
    [TestCase(false)]
    public void UploadToTarget(bool expectToSucceed)
    {
        var p = new Project(RepositoryLocator.DataExportRepository, "p")
        {
            ProjectNumber = projectNumberInTestData
        };
        p.SaveToDatabase();

        //delete RDMP knowledge of the cohort
        _extractableCohort.DeleteInDatabase();

        var discoveredServer = _externalCohortTable.Discover().Server;
        using (var con = discoveredServer.GetConnection())
        {
            con.Open();

            var sql = $@"
drop procedure if exists fishfishfishproc1;
GO

create procedure fishfishfishproc1
                    @sourceTableName varchar(10),
                    @projectNumber int,
                    @description varchar(10)
                    as
                    begin
                        select {(expectToSucceed? "distinct id from "+definitionTableName:"0")} 
                    end";
            UsefulStuff.ExecuteBatchNonQuery(sql,con);
        }
            
        var def = new CohortDefinition(null, "ignoremecohort", 1, 12,_externalCohortTable);
        var request = new CohortCreationRequest(p, def, (DataExportRepository)RepositoryLocator.DataExportRepository, "ignoremeauditlog");

        var d = new HICCohortManagerDestination
        {
            NewCohortsStoredProcedure = "fishfishfishproc1",
            ExistingCohortsStoredProcedure = "fishfishfishproc2"
        };
        d.PreInitialize(request,ThrowImmediatelyDataLoadEventListener.Quiet);
        d.CreateExternalCohort = true;

        var dt = new DataTable("mytbl");
        dt.Columns.Add("PrivateID");
        dt.Rows.Add("101");
        dt.Rows.Add("102");

        d.ProcessPipelineData(dt, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
        var tomem = new ToMemoryDataLoadEventListener(false);

        if (expectToSucceed)
        {
            //actually does the send
            d.Dispose(tomem,null);

           Assert.That(cohortIDInTestData, Is.EqualTo(request.CohortCreatedIfAny.OriginID));
        }
        else
            Assert.Throws<Exception>(() => d.Dispose(tomem, null));

    }
}