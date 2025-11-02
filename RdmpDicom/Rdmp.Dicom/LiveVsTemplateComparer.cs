// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using DicomTypeTranslation.TableCreation;
using FAnsi;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.ReusableLibraryCode.DataAccess;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rdmp.Dicom;

/// <summary>
/// Compares differences between an imaging template and a live data table on a server (what columns have been renamed, resized etc).
/// </summary>
public partial class LiveVsTemplateComparer
{
    public string TemplateSql {get;}
    public string LiveSql  {get;}

    public LiveVsTemplateComparer(ITableInfo table,ImageTableTemplateCollection templateCollection)
    {
        // locate the live table and script it as it stands today
        var discoveredTable = table.Discover(DataAccessContext.InternalDataProcessing);
        LiveSql = discoveredTable.ScriptTableCreation(false,false,false);

        LiveSql = TailorLiveSql(LiveSql,discoveredTable.Database.Server.DatabaseType);

        // The live table name e.g. CT_StudyTable
        var liveTableName = discoveredTable.GetRuntimeName();
        // Without the prefix e.g. StudyTable
        var liveTableNameWithoutPrefix = liveTableName[(liveTableName.IndexOf("_", StringComparison.Ordinal)+1)..];

        var template = templateCollection.Tables.FirstOrDefault(
            c=>c.TableName.Equals(liveTableName,StringComparison.CurrentCultureIgnoreCase) ||
               c.TableName.Equals(liveTableNameWithoutPrefix,StringComparison.CurrentCultureIgnoreCase)) ?? throw new Exception($"Could not find a Template called '{liveTableName}' or '{liveTableNameWithoutPrefix}'.  Templates in file were {string.Join(",",templateCollection.Tables.Select(t=>t.TableName))}");

        //script the template
        var creator = new ImagingTableCreation(discoveredTable.Database.Server.GetQuerySyntaxHelper());
        TemplateSql = creator.GetCreateTableSql(discoveredTable.Database,liveTableName,template, discoveredTable.Schema);

        TemplateSql  = TailorTemplateSql(TemplateSql );
    }
    private string TailorTemplateSql(string templateSql)
    {
        //condense all multiple spaces to single spaces
        templateSql = MultipleSpaceRegex().Replace(templateSql," ");

        return templateSql;
    }

    private string TailorLiveSql(string liveSql, DatabaseType databaseType)
    {
        // get rid of collation
        liveSql = CollateRegex().Replace(liveSql, "");

        // condense all multiple spaces to single spaces
        liveSql = MultipleSpaceRegex().Replace(liveSql, " ");

        return liveSql;
    }

    [GeneratedRegex("\\bCOLLATE \\w+",RegexOptions.CultureInvariant)]
    private static partial Regex CollateRegex();
    [GeneratedRegex("  +",RegexOptions.CultureInvariant)]
    private static partial Regex MultipleSpaceRegex();
}