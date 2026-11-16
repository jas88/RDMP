// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.


namespace Rdmp.Core.Icons.IconProvision;

/// <summary>
/// Check and progress icons loaded from embedded resources.
/// </summary>
public static class ChecksAndProgressIcons
{
    private const string P = "Rdmp.Core.Icons.";

    public static readonly RdmpIcon Fail = EmbeddedIconHelper.GetRequired($"{P}Fail");
    public static readonly RdmpIcon FailEx = EmbeddedIconHelper.GetRequired($"{P}FailEx");
    public static readonly RdmpIcon Information = EmbeddedIconHelper.GetRequired($"{P}Information");
    public static readonly RdmpIcon Tick = EmbeddedIconHelper.GetRequired($"{P}Tick");
    public static readonly RdmpIcon Warning = EmbeddedIconHelper.GetRequired($"{P}Warning");
    public static readonly RdmpIcon WarningEx = EmbeddedIconHelper.GetRequired($"{P}WarningEx");
}
