// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Rdmp.Core.Icons.IconProvision;

/// <summary>
/// FamFamFam Silk icons loaded from embedded resources.
/// </summary>
public static class FamFamFamIcons
{
    private const string P = "Rdmp.Core.Icons.famfamfam.";

    public static readonly Image<Rgba32> add = EmbeddedIconHelper.GetRequired($"{P}add");
    public static readonly Image<Rgba32> application_home = EmbeddedIconHelper.GetRequired($"{P}application_home");
    public static readonly Image<Rgba32> arrow_refresh = EmbeddedIconHelper.GetRequired($"{P}arrow_refresh");
    public static readonly Image<Rgba32> Back = EmbeddedIconHelper.GetRequired($"{P}back");
    public static readonly Image<Rgba32> bin_closed = EmbeddedIconHelper.GetRequired($"{P}bin_closed");
    public static readonly Image<Rgba32> cancel = EmbeddedIconHelper.GetRequired($"{P}cancel");
    public static readonly Image<Rgba32> cog = EmbeddedIconHelper.GetRequired($"{P}cog");
    public static readonly Image<Rgba32> delete = EmbeddedIconHelper.GetRequired($"{P}delete");
    public static readonly Image<Rgba32> delete_multi = EmbeddedIconHelper.GetRequired($"{P}delete_multi");
    public static readonly Image<Rgba32> disk = EmbeddedIconHelper.GetRequired($"{P}disk");
    public static readonly Image<Rgba32> flag_red = EmbeddedIconHelper.GetRequired($"{P}flag_red");
    public static readonly Image<Rgba32> Forward = EmbeddedIconHelper.GetRequired($"{P}forward");
    public static readonly Image<Rgba32> GreenFace = EmbeddedIconHelper.GetRequired($"{P}GreenFace");
    public static readonly Image<Rgba32> help = EmbeddedIconHelper.GetRequired($"{P}help");
    public static readonly Image<Rgba32> link = EmbeddedIconHelper.GetRequired($"{P}link");
    public static readonly Image<Rgba32> link_break = EmbeddedIconHelper.GetRequired($"{P}link_break");
    public static readonly Image<Rgba32> lock_break = EmbeddedIconHelper.GetRequired($"{P}lock_break");
    public static readonly Image<Rgba32> magnifier = EmbeddedIconHelper.GetRequired($"{P}magnifier");
    public static readonly Image<Rgba32> page_white_get = EmbeddedIconHelper.GetRequired($"{P}page_white_get");
    public static readonly Image<Rgba32> page_white_put = EmbeddedIconHelper.GetRequired($"{P}page_white_put");
    public static readonly Image<Rgba32> page_white_word = EmbeddedIconHelper.GetRequired($"{P}page_white_word");
    public static readonly Image<Rgba32> pencil_go = EmbeddedIconHelper.GetRequired($"{P}pencil_go");
    public static readonly Image<Rgba32> phone = EmbeddedIconHelper.GetRequired($"{P}phone");
    public static readonly Image<Rgba32> picture_save = EmbeddedIconHelper.GetRequired($"{P}picture_save");
    public static readonly Image<Rgba32> RedFace = EmbeddedIconHelper.GetRequired($"{P}RedFace");
    public static readonly Image<Rgba32> Redo = EmbeddedIconHelper.GetRequired($"{P}Redo");
    public static readonly Image<Rgba32> stop = EmbeddedIconHelper.GetRequired($"{P}stop");
    public static readonly Image<Rgba32> text_align_left = EmbeddedIconHelper.GetRequired($"{P}text_align_left");
    public static readonly Image<Rgba32> text_list_bullets = EmbeddedIconHelper.GetRequired($"{P}text_list_bullets");
    public static readonly Image<Rgba32> tick = EmbeddedIconHelper.GetRequired($"{P}Tick");
    public static readonly Image<Rgba32> time = EmbeddedIconHelper.GetRequired($"{P}time");
    public static readonly Image<Rgba32> Undo = EmbeddedIconHelper.GetRequired($"{P}Undo");
    public static readonly Image<Rgba32> wand = EmbeddedIconHelper.GetRequired($"{P}wand");
    public static readonly Image<Rgba32> YellowFace = EmbeddedIconHelper.GetRequired($"{P}YellowFace");
}
