// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;
using Oracle.ManagedDataAccess.Client;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Exceptions;
using Rdmp.Core.ReusableLibraryCode.Settings;

namespace Rdmp.Core.DataLoad.Triggers.Implementations;

/// <inheritdoc/>
internal class OracleTriggerImplementer : MySqlTriggerImplementer
{
    /// <inheritdoc cref="TriggerImplementer(DiscoveredTable,bool)"/>
    public OracleTriggerImplementer(DiscoveredTable table, bool createDataLoadRunIDAlso = true) : base(table,
        createDataLoadRunIDAlso)
    {
    }

    public override string CreateTrigger(ICheckNotifier notifier)
    {
        var creationSql = base.CreateTrigger(notifier);

        var sql = $@"CREATE OR REPLACE TRIGGER {GetTriggerName()}
BEFORE UPDATE ON {_table.GetFullyQualifiedName()}
FOR EACH ROW
{CreateTriggerBody()}";

        using var con = _server.GetConnection();
        con.Open();

        using var cmd = _server.GetCommand(sql, con);
        cmd.CommandTimeout = UserSettings.ArchiveTriggerTimeout;
        cmd.ExecuteNonQuery();

        return creationSql;
    }

    public override void DropTrigger(out string problemsDroppingTrigger, out string thingsThatWorkedDroppingTrigger)
    {
        problemsDroppingTrigger = "";
        thingsThatWorkedDroppingTrigger = "";

        try
        {
            using var con = _server.GetConnection();
            con.Open();

            using var cmd = _server.GetCommand($"DROP TRIGGER {GetTriggerName()}", con);
            cmd.CommandTimeout = UserSettings.ArchiveTriggerTimeout;
            cmd.ExecuteNonQuery();

            thingsThatWorkedDroppingTrigger = $"Dropped trigger {GetTriggerName()}";
        }
        catch (Exception exception)
        {
            //this is not a problem really since it is likely that DLE chose to recreate the trigger because it was FUBARed or missing, this is just belt and braces try and drop anything that is lingering, whether or not it is there
            problemsDroppingTrigger += $"Failed to drop Trigger:{exception.Message}{Environment.NewLine}";
        }
    }

    protected override string GetTriggerName()
    {
        // Oracle has a 30-character identifier limit for trigger names
        var tableName = QuerySyntaxHelper.MakeHeaderNameSensible(_table.GetRuntimeName());
        var triggerName = $"{tableName}_ONUPDATE";

        // Truncate to 30 characters if needed (Oracle limit)
        if (triggerName.Length > 30)
            triggerName = triggerName.Substring(0, 30);

        return triggerName;
    }

    public override TriggerStatus GetTriggerStatus()
    {
        try
        {
            using var con = _server.GetConnection();
            con.Open();

            using var cmd = _server.GetCommand(
                $"select status from all_triggers where trigger_name = UPPER('{GetTriggerName()}')", con);
            using var r = cmd.ExecuteReader();

            if (r.Read())
            {
                var status = r["status"].ToString();
                return status switch
                {
                    "ENABLED" => TriggerStatus.Enabled,
                    "DISABLED" => TriggerStatus.Disabled,
                    _ => TriggerStatus.Disabled // Treat invalid/broken triggers as disabled
                };
            }

            return TriggerStatus.Missing;
        }
        catch (Exception)
        {
            // If we can't query the trigger status, assume it's missing/broken
            return TriggerStatus.Missing;
        }
    }

    protected override string GetTriggerBody()
    {
        try
        {
            using var con = _server.GetConnection();
            con.Open();

            using var cmd =
                _server.GetCommand(
                    $"select trigger_body from all_triggers where trigger_name = UPPER('{GetTriggerName()}') AND status = 'ENABLED'", con);
            ((OracleCommand)cmd).InitialLONGFetchSize = -1;
            var r = cmd.ExecuteReader();

            while (r.Read())
                return (string)r["trigger_body"];
        }
        catch (Exception)
        {
            // If there's an error (e.g., trigger is invalid), return null to indicate it needs recreation
            return null;
        }

        return null;
    }

    protected override void AddValidFrom(DiscoveredTable table, IQuerySyntaxHelper syntaxHelper)
    {
        _table.AddColumn(SpecialFieldNames.ValidFrom, " DATE DEFAULT CURRENT_TIMESTAMP", true,
            UserSettings.ArchiveTriggerTimeout);
    }

    protected override string CreateTriggerBody()
    {
        var syntax = _table.GetQuerySyntaxHelper();

        return $@"BEGIN
    INSERT INTO {_archiveTable.GetFullyQualifiedName()} ({string.Join(",", _columns.Select(c => syntax.EnsureWrapped(c.GetRuntimeName())))},hic_validTo,hic_userID,hic_status) VALUES ({string.Join(",", _columns.Select(c => $":old.{syntax.EnsureWrapped(c.GetRuntimeName())}"))},CURRENT_DATE,USER,'U');

  :new.{syntax.EnsureWrapped(SpecialFieldNames.ValidFrom)} := sysdate;


  END";
    }

    protected override void AssertTriggerBodiesAreEqual(string sqlThen, string sqlNow)
    {
        sqlNow ??= "";
        sqlThen ??= "";

        if (!sqlNow.Trim(';', ' ', '\t').Equals(sqlThen.Trim(';', ' ', '\t')))
            throw new ExpectedIdenticalStringsException("Sql body for trigger doesn't match expected sql", sqlThen,
                sqlNow);
    }
}