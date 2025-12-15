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

    public static readonly Image<Rgba32> add = EmbeddedIconHelper.Get($"{P}add");
    public static readonly Image<Rgba32> application_home = EmbeddedIconHelper.Get($"{P}application_home");
    public static readonly Image<Rgba32> arrow_refresh = EmbeddedIconHelper.Get($"{P}arrow_refresh");
    public static readonly Image<Rgba32> Back = EmbeddedIconHelper.Get($"{P}back");
    public static readonly Image<Rgba32> bin_closed = EmbeddedIconHelper.Get($"{P}bin_closed");
    public static readonly Image<Rgba32> cancel = EmbeddedIconHelper.Get($"{P}cancel");
    public static readonly Image<Rgba32> cog = EmbeddedIconHelper.Get($"{P}cog");
    public static readonly Image<Rgba32> delete = EmbeddedIconHelper.Get($"{P}delete");
    public static readonly Image<Rgba32> delete_multi = EmbeddedIconHelper.Get($"{P}delete_multi");
    public static readonly Image<Rgba32> disk = EmbeddedIconHelper.Get($"{P}disk");
    public static readonly Image<Rgba32> flag_red = EmbeddedIconHelper.Get($"{P}flag_red");
    public static readonly Image<Rgba32> Forward = EmbeddedIconHelper.Get($"{P}forward");
    public static readonly Image<Rgba32> GreenFace = EmbeddedIconHelper.Get($"{P}GreenFace");
    public static readonly Image<Rgba32> help = EmbeddedIconHelper.Get($"{P}help");
    public static readonly Image<Rgba32> link = EmbeddedIconHelper.Get($"{P}link");
    public static readonly Image<Rgba32> link_break = EmbeddedIconHelper.Get($"{P}link_break");
    public static readonly Image<Rgba32> lock_break = EmbeddedIconHelper.Get($"{P}lock_break");
    public static readonly Image<Rgba32> magnifier = EmbeddedIconHelper.Get($"{P}magnifier");
    public static readonly Image<Rgba32> page_white_get = EmbeddedIconHelper.Get($"{P}page_white_get");
    public static readonly Image<Rgba32> page_white_put = EmbeddedIconHelper.Get($"{P}page_white_put");
    public static readonly Image<Rgba32> page_white_word = EmbeddedIconHelper.Get($"{P}page_white_word");
    public static readonly Image<Rgba32> pencil_go = EmbeddedIconHelper.Get($"{P}pencil_go");
    public static readonly Image<Rgba32> phone = EmbeddedIconHelper.Get($"{P}phone");
    public static readonly Image<Rgba32> picture_save = EmbeddedIconHelper.Get($"{P}picture_save");
    public static readonly Image<Rgba32> RedFace = EmbeddedIconHelper.Get($"{P}RedFace");
    public static readonly Image<Rgba32> Redo = EmbeddedIconHelper.Get($"{P}Redo");
    public static readonly Image<Rgba32> stop = EmbeddedIconHelper.Get($"{P}stop");
    public static readonly Image<Rgba32> text_align_left = EmbeddedIconHelper.Get($"{P}text_align_left");
    public static readonly Image<Rgba32> text_list_bullets = EmbeddedIconHelper.Get($"{P}text_list_bullets");
    public static readonly Image<Rgba32> tick = EmbeddedIconHelper.Get($"{P}Tick");
    public static readonly Image<Rgba32> time = EmbeddedIconHelper.Get($"{P}time");
    public static readonly Image<Rgba32> Undo = EmbeddedIconHelper.Get($"{P}Undo");
    public static readonly Image<Rgba32> wand = EmbeddedIconHelper.Get($"{P}wand");
    public static readonly Image<Rgba32> YellowFace = EmbeddedIconHelper.Get($"{P}YellowFace");
}
