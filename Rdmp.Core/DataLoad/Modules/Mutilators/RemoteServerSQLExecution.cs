// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using FAnsi.Discovery;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Engine.Mutilators;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rdmp.Core.DataLoad.Modules.Mutilators
{
    internal class RemoteServerSQLExecution : IPluginMutilateDataTables
    {

        [DemandsInitialization("The Remote Database server to run this sql on")]
        public ExternalDatabaseServer RemoteServer { get; set; }

        [DemandsInitialization("Run the following SQL when this component is run in the DLE", DemandType = DemandType.SQL,
    Mandatory = true)]
        public string Sql { get; set; }

        public void Check(ICheckNotifier notifier)
        {
            if (!RemoteServer.Exists())
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Remote Server unavailable", CheckResult.Fail));
            }
        }

        public void Initialize(DiscoveredDatabase dbInfo, LoadStage loadStage)
        {
        }

        public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
        {
        }

        public ExitCodeType Mutilate(IDataLoadJob job)
        {
            var db = RemoteServer.Discover(ReusableLibraryCode.DataAccess.DataAccessContext.DataLoad);
            try
            {
                using (var conn = db.Server.GetConnection())
                {
                    conn.Open();
                    var cmd = db.Server.GetCommand(Sql, conn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, e.Message));
                return ExitCodeType.Error;
            }
            return ExitCodeType.Success;
        }
    }
}
