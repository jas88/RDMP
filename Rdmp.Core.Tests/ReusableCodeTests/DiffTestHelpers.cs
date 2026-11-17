// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode;
using Hunk = (int StartA, int deletedA, int StartB, int insertedB);

namespace Rdmp.Core.Tests.ReusableCodeTests;

/// <summary>
/// Code shared by the tests of <see cref="Diff.DiffText"/>: the rules that every result follows (see
/// <see cref="FindViolations"/>) and ways to build texts
/// </summary>
internal static class DiffTestHelpers
{
    /// <summary>
    /// Returns a description of each rule that <paramref name="items"/> breaks as the result of comparing
    /// <paramref name="before"/> with <paramref name="after"/>. The rules are: every change is well formed and inside
    /// both texts (R1); changes are in order and separated by unchanged lines, which pair up in order (R2, R4) and are
    /// equal (R3); as few lines as possible are changed (R5); the lines that both texts start with (R6) and then end
    /// with (R7) are not part of any change; and comparing the texts again gives the same result (R8).
    /// </summary>
    /// <param name="before">The first text</param>
    /// <param name="after">The second text</param>
    /// <param name="items">The result of comparing them</param>
    /// <param name="minimalEdits">The fewest lines that can be deleted or inserted, if known.  Otherwise it is
    /// computed from a longest common subsequence, which takes time proportional to the product of the line counts.</param>
    public static List<string> FindViolations(string before, string after, Diff.Item[] items, int? minimalEdits = null)
    {
        var problems = new List<string>();
        if (items == null)
        {
            problems.Add("The result is null");
            return problems;
        }

        var a = SplitLines(before);
        var b = SplitLines(after);

        var endA = 0;
        var endB = 0;
        for (var j = 0; j < items.Length; j++)
        {
            var item = items[j];

            if (item.deletedA < 0 || item.insertedB < 0 || item.deletedA + item.insertedB < 1)
                problems.Add($"R1: change {j} {item} has a negative count or changes nothing");

            if (item.StartA < 0 || item.StartB < 0 || item.StartA + item.deletedA > a.Length ||
                item.StartB + item.insertedB > b.Length)
                problems.Add($"R1: change {j} {item} is outside the texts ({a.Length} and {b.Length} lines)");

            CheckUnchanged(a, b, endA, endB, item.StartA - endA, item.StartB - endB, j == 0 ? 0 : 1,
                $"before change {j} {item}", problems);

            endA = item.StartA + item.deletedA;
            endB = item.StartB + item.insertedB;
        }

        CheckUnchanged(a, b, endA, endB, a.Length - endA, b.Length - endB, 0, "after the last change", problems);

        var edits = items.Sum(static i => (long)i.deletedA + i.insertedB);
        var fewest = minimalEdits ?? a.Length + b.Length - 2 * LongestCommonSubsequenceLength(a, b);
        if (edits != fewest)
            problems.Add($"R5: {edits} lines changed but {fewest} would do");

        var start = 0;
        while (start < a.Length && start < b.Length && a[start] == b[start])
            start++;

        var end = 0;
        while (end < Math.Min(a.Length, b.Length) - start && a[^(end + 1)] == b[^(end + 1)])
            end++;

        if (items.Length > 0)
        {
            var first = items[0];
            if (first.StartA < start || first.StartB < start)
                problems.Add($"R6: change {first} is inside the {start} lines that both texts start with");

            var last = items[^1];
            if (last.StartA + last.deletedA > a.Length - end || last.StartB + last.insertedB > b.Length - end)
                problems.Add($"R7: change {last} is inside the {end} lines that both texts end with");
        }

        if (!AsHunks(Diff.DiffText(before, after)).SequenceEqual(AsHunks(items)))
            problems.Add("R8: comparing the same texts again gave a different result");

        return problems;
    }

    /// <summary>
    /// Checks that the <paramref name="countA"/> lines of <paramref name="a"/> from <paramref name="fromA"/> and the
    /// <paramref name="countB"/> lines of <paramref name="b"/> from <paramref name="fromB"/>, which are outside every
    /// change, are the same number of lines (at least <paramref name="minimum"/>) and equal
    /// </summary>
    private static void CheckUnchanged(string[] a, string[] b, int fromA, int fromB, int countA, int countB,
        int minimum, string where, List<string> problems)
    {
        if (countA != countB || countA < minimum)
        {
            problems.Add($"R2: {countA} unchanged lines of the first text but {countB} of the second {where}");
            return;
        }

        if (fromA < 0 || fromB < 0 || fromA + countA > a.Length || fromB + countB > b.Length)
            return;

        for (var t = 0; t < countA; t++)
            if (a[fromA + t] != b[fromB + t])
            {
                problems.Add($"R3: unchanged lines {fromA + t} and {fromB + t} differ {where}");
                return;
            }
    }

    /// <summary>
    /// The textbook dynamic program for the length of a longest common subsequence, kept independent of the code
    /// under test
    /// </summary>
    private static int LongestCommonSubsequenceLength(string[] a, string[] b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        foreach (var lineA in a)
        {
            for (var j = 1; j <= b.Length; j++)
                current[j] = lineA == b[j - 1] ? previous[j - 1] + 1 : Math.Max(previous[j], current[j - 1]);

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    /// <summary>
    /// Splits a text into lines the way <see cref="Diff.DiffText"/> is specified to
    /// </summary>
    private static string[] SplitLines(string text) =>
        text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');

    public static Hunk[] AsHunks(Diff.Item[] items) =>
        items.Select(static i => (i.StartA, i.deletedA, i.StartB, i.insertedB)).ToArray();

    public static string Text(int lines, Func<int, string> line) =>
        string.Join("\n", Enumerable.Range(0, lines).Select(line));

    public static string RandomLetters(Random random, int lines, int letters) =>
        Text(lines, _ => ((char)('a' + random.Next(letters))).ToString());

    public static string Escape(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n");
}
