// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using FAnsi;
using Rdmp.Core.Icons;

namespace Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

/// <summary>
/// Provides overlay and basic icons for all DatabaseTypes
/// </summary>
public class DatabaseTypeIconProvider
{
    private readonly RdmpIcon _msBig;
    private readonly RdmpIcon _msOverlay;

    private readonly RdmpIcon _oraBig;
    private readonly RdmpIcon _oraOverlay;

    private readonly RdmpIcon _mysBig;
    private readonly RdmpIcon _mysOverlay;

    private readonly RdmpIcon _postgresBig;
    private readonly RdmpIcon _postgresOverlay;

    private readonly RdmpIcon _unknownBig;
    private readonly RdmpIcon _unknownOverlay;

    public DatabaseTypeIconProvider()
    {
        _msBig = DatabaseProviderIcons.Microsoft;
        _msOverlay = DatabaseProviderIcons.MicrosoftOverlay;

        _mysBig = DatabaseProviderIcons.MySql;
        _mysOverlay = DatabaseProviderIcons.MySqlOverlay;

        _oraBig = DatabaseProviderIcons.Oracle;
        _oraOverlay = DatabaseProviderIcons.OracleOverlay;

        _postgresBig = DatabaseProviderIcons.PostgreSql;
        _postgresOverlay = DatabaseProviderIcons.PostgreSqlOverlay;

        _unknownBig = DatabaseProviderIcons.Unknown;
        _unknownOverlay = DatabaseProviderIcons.UnknownOverlay;
    }

    public RdmpIcon GetOverlay(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.MicrosoftSQLServer => _msOverlay,
            DatabaseType.MySql => _mysOverlay,
            DatabaseType.Oracle => _oraOverlay,
            DatabaseType.PostgreSql => _postgresOverlay,
            _ => _unknownOverlay
        };
    }

    public RdmpIcon GetImage(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.MicrosoftSQLServer => _msBig,
            DatabaseType.MySql => _mysBig,
            DatabaseType.Oracle => _oraBig,
            DatabaseType.PostgreSql => _postgresBig,
            _ => _unknownBig
        };
    }
}