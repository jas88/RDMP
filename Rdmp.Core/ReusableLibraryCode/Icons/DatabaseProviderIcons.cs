// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Icons;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Rdmp.Core.ReusableLibraryCode.Icons;

/// <summary>
/// Database provider icons loaded from embedded resources.
/// </summary>
public static class DatabaseProviderIcons
{
    private const string P = "Rdmp.Core.ReusableLibraryCode.Icons.DatabaseProviderIcons.";

    public static readonly Image<Rgba32> Microsoft = EmbeddedIconHelper.Get($"{P}Microsoft");
    public static readonly Image<Rgba32> MicrosoftOverlay = EmbeddedIconHelper.Get($"{P}MicrosoftOverlay");
    public static readonly Image<Rgba32> MySql = EmbeddedIconHelper.Get($"{P}MySql");
    public static readonly Image<Rgba32> MySqlOverlay = EmbeddedIconHelper.Get($"{P}MySqlOverlay");
    public static readonly Image<Rgba32> Oracle = EmbeddedIconHelper.Get($"{P}Oracle");
    public static readonly Image<Rgba32> OracleOverlay = EmbeddedIconHelper.Get($"{P}OracleOverlay");
    public static readonly Image<Rgba32> PostgreSql = EmbeddedIconHelper.Get($"{P}PostgreSql");
    public static readonly Image<Rgba32> PostgreSqlOverlay = EmbeddedIconHelper.Get($"{P}PostgreSqlOverlay");
    public static readonly Image<Rgba32> Unknown = EmbeddedIconHelper.Get($"{P}Unknown");
    public static readonly Image<Rgba32> UnknownOverlay = EmbeddedIconHelper.Get($"{P}UnknownOverlay");
}
