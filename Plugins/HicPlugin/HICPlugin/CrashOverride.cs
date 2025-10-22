﻿using System.Collections.Generic;
using Rdmp.Core.ReusableLibraryCode.Checks;
using FAnsi.Discovery;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.DataLoad;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.Curation;

namespace HICPlugin;

/// <summary>
/// Overrides the default crash behaviour of the RDMP which is to leave remnants (RAW/STAGING) intact for inspection debugging.  This component will predict the staging database and then
/// nuke it
/// </summary>
class CrashOverride : IPluginAttacher
{

    [DemandsInitialization("Attempts to delete all tables relevant to the load in RAW database in the even that the data load crashes",DemandType.Unspecified,true)]
    public bool BurnRAW { get; set; }
    [DemandsInitialization("Attempts to delete all tables relevant to the load in STAGING database in the even that the data load crashes", DemandType.Unspecified, true)]
    public bool BurnSTAGING { get; set; }


    private DiscoveredDatabase _stagingDatabase;
    readonly List<string> _stagingTableNamesToNuke = new();

    private DiscoveredDatabase _rawDatabase;
    readonly List<string> _rawTableNamesToNuke = new();

    public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
    {
        if (exitCode is not (ExitCodeType.Abort or ExitCodeType.Error)) return;
        if(BurnSTAGING)
            DropTables(_stagingDatabase, _stagingTableNamesToNuke, postLoadEventsListener);

        if(BurnRAW)
            DropTables(_rawDatabase, _rawTableNamesToNuke, postLoadEventsListener);
    }

    private void DropTables(DiscoveredDatabase discoveredDatabase, List<string> tables, IDataLoadEventListener postLoadEventsListener)
    {
        if (discoveredDatabase.Exists())
        {
            postLoadEventsListener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Found database {discoveredDatabase}"));
            foreach (var t in tables)
            {
                var tbl = discoveredDatabase.ExpectTable(t);
                if (tbl.Exists())
                {
                    tbl.Drop();
                    postLoadEventsListener.OnNotify(this,
                        new NotifyEventArgs(ProgressEventType.Information, $"Dropped table {t}"));
                }
                else
                    postLoadEventsListener.OnNotify(this,
                        new NotifyEventArgs(ProgressEventType.Warning,
                            $"Did not see table {t} in database {discoveredDatabase.GetRuntimeName()}"));
            }
        }
        else
            postLoadEventsListener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Could not find database {discoveredDatabase} for error cleanup"));
    }

    public bool DisposeImmediately { get; set; }
    public void Check(ICheckNotifier notifier)
    {
    }

    public ExitCodeType Attach(IDataLoadJob job,GracefulCancellationToken token)
    {
        foreach (var t in job.LookupTablesToLoad)
            _stagingTableNamesToNuke.Add(t.GetRuntimeName(LoadStage.AdjustStaging));

        foreach (var t in job.LookupTablesToLoad)
            _rawTableNamesToNuke.Add(t.GetRuntimeName(LoadStage.AdjustRaw));

        _stagingDatabase = job.LoadMetadata.GetDistinctLiveDatabaseServer().ExpectDatabase("DLE_STAGING");


        return ExitCodeType.Success;
    }

    public void Initialize(ILoadDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
    {
        _rawDatabase = dbInfo;
    }

    public ILoadDirectory LoadDirectory { get; set; }
    public bool RequestsExternalDatabaseCreation { get; }
}