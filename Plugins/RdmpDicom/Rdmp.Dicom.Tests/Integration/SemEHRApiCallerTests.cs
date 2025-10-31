// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using FAnsi.Discovery;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Versioning;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Databases;
using Rdmp.Core.QueryCaching.Aggregation;
using Rdmp.Dicom.ExternalApis;
using Rdmp.Core.ReusableLibraryCode.Checks;
using System.Threading;
using Rdmp.Core.CohortCreation.Execution;
using Tests.Common;
using DatabaseType = FAnsi.DatabaseType;

namespace Rdmp.Dicom.Tests.Integration;

public class SemEHRApiCallerTests : DatabaseTests
{
    private CachedAggregateConfigurationResultsManager SetupCache(DatabaseType dbType, out DiscoveredDatabase cacheDb)
    {
        cacheDb = GetCleanedServer(dbType);
        var creator = new MasterDatabaseScriptExecutor(cacheDb);
        var patcher = new QueryCachingPatcher();

        creator.CreateAndPatchDatabase(patcher, new AcceptAllCheckNotifier());

        var eds = new ExternalDatabaseServer(CatalogueRepository, "cache", patcher);
        eds.SetProperties(cacheDb);

        return new CachedAggregateConfigurationResultsManager(eds);
    }


    [RequiresSemEHR]
    [TestCase(DatabaseType.MicrosoftSQLServer)]
    public void TalkToApi(DatabaseType dbType)
    {
        var cacheMgr = SetupCache(dbType, out var cacheDb);
        var caller = new SemEHRApiCaller();

        var cata = new Catalogue(CatalogueRepository, $"{PluginCohortCompiler.ApiPrefix}cata");
        var cic = new CohortIdentificationConfiguration(CatalogueRepository, "my cic");
        cic.CreateRootContainerIfNotExists();

        var ac = new AggregateConfiguration(CatalogueRepository, cata, "blah");
        cic.RootCohortAggregateContainer.AddChild(ac, 0);

        var semEHRConfiguration = new SemEHRConfiguration()
        {
            Url = RequiresSemEHR.SemEhrTestUrl + "/api/search_anns/myQuery/",
            Query = "C0205076",
            ValidateServerCert = false
        };

        caller.Run(ac, cacheMgr, semEHRConfiguration, CancellationToken.None);

        var resultTable = cacheMgr.GetLatestResultsTableUnsafe(ac, AggregateOperation.IndexedExtractionIdentifierList);

        Assert.That(resultTable, Is.Not.Null);

        var tbl = cacheDb.ExpectTable(resultTable.GetRuntimeName());
        Assert.That(tbl.GetDataTable().Rows, Has.Count.EqualTo(75));
    }
}