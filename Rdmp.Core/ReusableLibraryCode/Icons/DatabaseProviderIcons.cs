// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Icons;

namespace Rdmp.Core.ReusableLibraryCode.Icons;

/// <summary>
/// Database provider icons loaded from embedded resources.
/// </summary>
public static class DatabaseProviderIcons
{
    private const string P = "Rdmp.Core.ReusableLibraryCode.Icons.DatabaseProviderIcons.";

    public static readonly RdmpIcon Microsoft = EmbeddedIconHelper.GetRequired($"{P}Microsoft");
    public static readonly RdmpIcon MicrosoftOverlay = EmbeddedIconHelper.GetRequired($"{P}MicrosoftOverlay");
    public static readonly RdmpIcon MySql = EmbeddedIconHelper.GetRequired($"{P}MySql");
    public static readonly RdmpIcon MySqlOverlay = EmbeddedIconHelper.GetRequired($"{P}MySqlOverlay");
    public static readonly RdmpIcon Oracle = EmbeddedIconHelper.GetRequired($"{P}Oracle");
    public static readonly RdmpIcon OracleOverlay = EmbeddedIconHelper.GetRequired($"{P}OracleOverlay");
    public static readonly RdmpIcon PostgreSql = EmbeddedIconHelper.GetRequired($"{P}PostgreSql");
    public static readonly RdmpIcon PostgreSqlOverlay = EmbeddedIconHelper.GetRequired($"{P}PostgreSqlOverlay");
    public static readonly RdmpIcon Unknown = EmbeddedIconHelper.GetRequired($"{P}Unknown");
    public static readonly RdmpIcon UnknownOverlay = EmbeddedIconHelper.GetRequired($"{P}UnknownOverlay");
}
