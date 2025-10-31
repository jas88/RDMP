// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using FAnsi.Discovery;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.DatabaseManagement.Operations;

namespace Rdmp.Dicom.Attachers.Routing;

/// <summary>
/// Clones databases and tables using ColumnInfos, and records operations so the cloning can be undone.
/// </summary>
public class PersistentRawTableCreator : IDisposeAfterDataLoad
{
    readonly List<DiscoveredTable> _rawTables = new();

    public void CreateRAWTablesInDatabase(DiscoveredDatabase rawDb, IDataLoadJob job)
    {
        var namer = job.Configuration.DatabaseNamer;
        foreach (var tableInfo in job.RegularTablesToLoad)
        {
            var liveTable = tableInfo.Discover(DataAccessContext.DataLoad);

            var rawTableName = namer.GetName(liveTable.GetRuntimeName(),LoadBubble.Raw);

            var rawTable = rawDb.ExpectTable(rawTableName);

            if(rawTable.Exists())
                rawTable.Drop();

            var discardedColumns = tableInfo.PreLoadDiscardedColumns.Where(c => c.Destination == DiscardedColumnDestination.Dilute).ToArray();

            var clone = new TableInfoCloneOperation(job.Configuration,(TableInfo)tableInfo,LoadBubble.Raw,job);

            clone.CloneTable(liveTable.Database, rawDb, tableInfo.Discover(DataAccessContext.DataLoad), rawTableName, true,true, true, discardedColumns);

            var existingColumns = tableInfo.ColumnInfos.Select(c => c.GetRuntimeName(LoadStage.AdjustRaw)).ToArray();

            foreach (var preLoadDiscardedColumn in tableInfo.PreLoadDiscardedColumns)
            {
                //this column does not get dropped so will be in live TableInfo
                if (preLoadDiscardedColumn.Destination == DiscardedColumnDestination.Dilute)
                    continue;

                if (existingColumns.Any(e => e.Equals(preLoadDiscardedColumn.GetRuntimeName(LoadStage.AdjustRaw))))
                    throw new Exception(
                        $"There is a column called {preLoadDiscardedColumn.GetRuntimeName(LoadStage.AdjustRaw)} as both a PreLoadDiscardedColumn and in the TableInfo (live table), you should either drop the column from the live table or remove it as a PreLoadDiscarded column");

                //add all the preload discarded columns because they could be routed to ANO store or sent to oblivion
                AddColumnToTable(rawTable, preLoadDiscardedColumn.RuntimeColumnName, preLoadDiscardedColumn.SqlDataType, job);
            }

            _rawTables.Add(rawTable);

        }
    }

    private void AddColumnToTable(DiscoveredTable table, string desiredColumnName, string desiredColumnType, IDataLoadEventListener listener)
    {
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"Adding column '{desiredColumnName}' with datatype '{desiredColumnType}' to table '{table.GetFullyQualifiedName()}'"));
        table.AddColumn(desiredColumnName, desiredColumnType, true, 500);
    }

    public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
    {
        foreach (var rawTable in _rawTables)
            rawTable.Drop();
    }
}