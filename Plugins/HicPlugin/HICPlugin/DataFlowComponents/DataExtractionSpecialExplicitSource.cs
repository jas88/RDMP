// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using FAnsi.Discovery.QuerySyntax;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction.Pipeline.Sources;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents;


public class DataExtractionSpecialExplicitSource : ExecuteDatasetExtractionSource
{
    [DemandsInitialization("The database you want a using statement put in front of")]
    public string DatabaseToUse { get; set; }

    [DemandsInitialization("The collation you want injected into join SQL")]
    public string Collation { get; set; }

    [DemandsInitialization(@"Sql to wrap columns that have an ANOTable_ID transform configured on them (or match AlsoANORegex) , use
{0} - ColumnSql e.g. UPPER([mytable]..[gp_code])
{1} - UnderlyingColumnRuntimeName e.g. gp_code
{2} - ProjectNumber e.g. 23")]
    public string ANOWrapFunction { get; set; }

    [DemandsInitialization(@"If you have an ANOWrapFunction function then columns matching this regex will also get wrapped")]
    public StandardRegex AlsoANORegex { get; set; }

    public override string HackExtractionSQL(string sql, IDataLoadEventListener listener)
    {
        var sb = new StringBuilder();

        if(!string.IsNullOrWhiteSpace(Collation))
            ((QueryBuilder) Request.QueryBuilder).AddCustomLine($"collate {Collation}", QueryComponent.JoinInfoJoin);

        if (!string.IsNullOrWhiteSpace(DatabaseToUse))
            sb.AppendLine($"USE {DatabaseToUse}");

        if (!string.IsNullOrWhiteSpace(ANOWrapFunction))
            ApplyANOWrap(listener);

        sb.AppendLine(Request.QueryBuilder.SQL);

        listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information,
            $"HACKED SQL{Environment.NewLine}------------------------------------------{Environment.NewLine}{sb}"));

        return sb.ToString();
    }

    private void ApplyANOWrap(IDataLoadEventListener listener)
    {
        Regex regex = null;
        if(AlsoANORegex != null)
            regex = new Regex(AlsoANORegex.Regex,RegexOptions.IgnoreCase);

        foreach (var queryTimeColumn in Request.QueryBuilder.SelectColumns)
        {
            if (queryTimeColumn.UnderlyingColumn is { ANOTable_ID: { } })
                ApplyANOWrap(queryTimeColumn, listener);
            else
            if(regex?.IsMatch(queryTimeColumn.IColumn.GetRuntimeName()) == true)
                ApplyANOWrap(queryTimeColumn, listener);
            else
                listener.OnNotify(this,
                    new NotifyEventArgs(ProgressEventType.Information, $"No Match:{queryTimeColumn}"));
        }

        Request.QueryBuilder.RegenerateSQL();
    }

    private void ApplyANOWrap(QueryTimeColumn queryTimeColumn, IDataLoadEventListener listener)
    {
        if(queryTimeColumn.IColumn is not ExtractableColumn ec)
        {
            listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Error,
                $"Column {queryTimeColumn.IColumn} matched ANO pattern or had ANO transform but wasn't an ExtractionInformation (it was a {queryTimeColumn.IColumn.GetType()})"));
            return;
        }

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"Match, Wrapping:{queryTimeColumn}"));

        var runtimeName = queryTimeColumn.IColumn.GetRuntimeName();

        ec.SelectSQL = string.Format(ANOWrapFunction, queryTimeColumn.IColumn.SelectSQL,
            queryTimeColumn.UnderlyingColumn.GetRuntimeName(),
            Request.Salt.GetSalt());

        if(string.IsNullOrWhiteSpace(queryTimeColumn.IColumn.Alias))
            ec.Alias = runtimeName;

        //IMPORTANT: Do not save this object!
    }
}