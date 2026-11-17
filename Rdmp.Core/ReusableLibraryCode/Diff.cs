// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;

namespace Rdmp.Core.ReusableLibraryCode;

/// <summary>
/// Compares two texts line by line, e.g. to highlight what changed between two versions of some SQL or YAML shown side
/// by side.
/// <para>The changes found are a shortest edit script: as few deleted and inserted lines as possible. They are computed
/// with the greedy algorithm and its linear space refinement from E. W. Myers, "An O(ND) Difference Algorithm and Its
/// Variations", Algorithmica 1(2):251-266, 1986. For texts of N and M lines with D lines deleted or inserted this takes
/// O((N+M)D) time and O(N+M) memory.</para>
/// </summary>
public static partial class Diff
{
    /// <summary>
    /// One change between two texts: <see cref="deletedA"/> lines of the first text, starting at line
    /// <see cref="StartA"/>, are replaced by <see cref="insertedB"/> lines of the second text, starting at line
    /// <see cref="StartB"/>. Line numbers start at 0.
    /// <para>One of the two counts can be 0, but never both. For a pure insertion <see cref="StartA"/> is the line of the
    /// first text that the new lines go before (the number of lines when they go at the end). For a pure deletion
    /// <see cref="StartB"/> is the line of the second text that follows the place where the lines were removed.</para>
    /// </summary>
    public readonly struct Item
    {
        /// <summary>
        /// Index of the first line of the first text covered by this change
        /// </summary>
        public int StartA { get; }

        /// <summary>
        /// Number of lines of the first text removed by this change
        /// </summary>
        public int deletedA { get; }

        /// <summary>
        /// Index of the first line of the second text covered by this change
        /// </summary>
        public int StartB { get; }

        /// <summary>
        /// Number of lines of the second text added by this change
        /// </summary>
        public int insertedB { get; }

        /// <summary>
        /// Describes a change that replaces <paramref name="deletedA"/> lines of the first text, from line
        /// <paramref name="startA"/>, with <paramref name="insertedB"/> lines of the second text, from line
        /// <paramref name="startB"/>
        /// </summary>
        public Item(int startA, int deletedA, int startB, int insertedB)
        {
            StartA = startA;
            this.deletedA = deletedA;
            StartB = startB;
            this.insertedB = insertedB;
        }

        /// <inheritdoc/>
        public override string ToString() => $"({StartA},{deletedA},{StartB},{insertedB})";
    }

    /// <summary>
    /// Compares <paramref name="textA"/> with <paramref name="textB"/> and returns, in order, the changes that turn the
    /// lines of the first into the lines of the second. The array is empty when both have the same lines.
    /// <para>Carriage returns are removed wherever they are, and the rest is split at each line feed, so k line feeds
    /// give k + 1 lines and an empty text is one empty line. Two lines are equal only when they hold exactly the same
    /// characters (an ordinal, case sensitive comparison).</para>
    /// <para>The changes cover as few lines as possible. The lines that both texts start with, and then the lines that
    /// both end with, are never part of a change. Lines that are not part of any change pair up in order between the
    /// two texts, and two changes are always separated by at least one such line.</para>
    /// <para>When equal lines make several such results possible, a run of deleted or inserted lines is moved over the
    /// equal lines next to it to where it joins a change in the other text, so that an edited line next to a copy of
    /// its old content is one change. A run that cannot join one goes as far down as it can.</para>
    /// </summary>
    /// <param name="textA">The first (before) text</param>
    /// <param name="textB">The second (after) text</param>
    /// <returns>The changes in order, never null</returns>
    /// <exception cref="ArgumentNullException">If either text is null</exception>
    public static Item[] DiffText(string textA, string textB)
    {
        ArgumentNullException.ThrowIfNull(textA);
        ArgumentNullException.ThrowIfNull(textB);

        var keys = new Dictionary<string, int>(StringComparer.Ordinal);
        var linesA = KeyLines(textA, keys);
        var linesB = KeyLines(textB, keys);

        return new LineAligner(linesA, linesB, keys.Count).GetChanges();
    }

    /// <summary>
    /// Splits <paramref name="text"/> into lines as described on <see cref="DiffText"/> and returns a number for each
    /// line, the same number for equal lines. <paramref name="keys"/> holds the numbers given out so far, which run
    /// from 0 to one less than its count.
    /// </summary>
    private static int[] KeyLines(string text, Dictionary<string, int> keys)
    {
        var lines = text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        var result = new int[lines.Length];

        for (var i = 0; i < lines.Length; i++)
        {
            if (!keys.TryGetValue(lines[i], out var key))
            {
                key = keys.Count;
                keys.Add(lines[i], key);
            }

            result[i] = key;
        }

        return result;
    }
}
