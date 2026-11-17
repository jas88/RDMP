// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode;
using Hunk = (int StartA, int deletedA, int StartB, int insertedB);

namespace Rdmp.Core.Tests.ReusableCodeTests;

/// <summary>
/// Tests for <see cref="Diff.DiffText"/>: inputs with only one minimal result, inputs with several, random inputs and
/// large inputs. Every result is also checked against the rules that any correct result follows, see
/// <see cref="FindViolations"/>.
/// </summary>
public class DiffTests
{
    #region Inputs with only one minimal result

    private static TestCaseData Case(string id, string before, string after, params Hunk[] expected) =>
        new TestCaseData(before, after, expected).SetArgDisplayNames(id);

    private static IEnumerable<TestCaseData> ExactCases()
    {
        // Line model and line endings
        yield return Case("T01", "", "");
        yield return Case("T02", "alpha\nbeta\ngamma", "alpha\nbeta\ngamma");
        yield return Case("T03", "", "x", (0, 1, 0, 1));
        yield return Case("T04", "x", "", (0, 1, 0, 1));
        yield return Case("T05", "", "x\ny", (0, 1, 0, 2));
        yield return Case("T06", "", "\nx", (1, 0, 1, 1));
        yield return Case("T07", "a\nb", "a\nb\n", (2, 0, 2, 1));
        yield return Case("T08", "a\nb\n", "a\nb", (2, 1, 2, 0));
        yield return Case("T09", "a\r\nb\r\n", "a\nb\n");
        yield return Case("T10", "a\rb", "ab");
        yield return Case("T11", "a\rb", "a\nb", (0, 1, 0, 2));
        yield return Case("T12", "a\r\nb\nc\r\n", "a\nb\r\nc\n");
        yield return Case("T13", "a\r\nb\r\nc", "a\nB\nc", (1, 1, 1, 1));
        yield return Case("T14", "a\r", "a");
        yield return Case("T15", "a\r\r\nb", "a\nb");
        yield return Case("T16", "\r", "");
        yield return Case("T17", "", "\r\n", (1, 0, 1, 1));
        yield return Case("T18", "\n", "", (1, 1, 1, 0));
        yield return Case("T19", "", "\n", (1, 0, 1, 1));
        yield return Case("T20", "\n\n", "\n", (2, 1, 2, 0));
        yield return Case("T21", "a" + (char)0x2028 + "b", "a\nb", (0, 1, 0, 2));
        yield return Case("T22", "a\u0085b", "a\nb", (0, 1, 0, 2));
        yield return Case("T23", "a\fb", "a\nb", (0, 1, 0, 2));
        yield return Case("T24", "\n", "\nx\n", (1, 0, 1, 1));

        // Line equality
        yield return Case("T25", "SELECT 1", "select 1", (0, 1, 0, 1));
        yield return Case("T26", "a b", "a  b", (0, 1, 0, 1));
        yield return Case("T27", "a ", "a", (0, 1, 0, 1));
        yield return Case("T28", "a\tb", "a b", (0, 1, 0, 1));
        yield return Case("T29", "caf\u00e9", "cafe\u0301", (0, 1, 0, 1));
        yield return Case("T30", "a \nb", "a\nb", (0, 1, 0, 1));
        yield return Case("T31", " \n\t", "\n", (0, 2, 0, 2));

        // Edits
        yield return Case("T32", "a\nb\nc", "a\nb\nX\nc", (2, 0, 2, 1));
        yield return Case("T33", "a\nb\nX\nc", "a\nb\nc", (2, 1, 2, 0));
        yield return Case("T34", "b\nc", "a\nb\nc", (0, 0, 0, 1));
        yield return Case("T35", "a\nb", "a\nb\nc", (2, 0, 2, 1));
        yield return Case("T36", "a\nb\nc", "b\nc", (0, 1, 0, 0));
        yield return Case("T37", "a\nb\nc", "a\nb", (2, 1, 2, 0));
        yield return Case("T38", "a\nb\nc", "a\nX\nc", (1, 1, 1, 1));
        yield return Case("T39", "a\nb\nc\nd", "a\nX\nY\nZ\nd", (1, 2, 1, 3));
        yield return Case("T40", "1\n2\n3\n4\n5\n6\n7", "1\nX\n3\n4\n5\n6\nY\n7", (1, 1, 1, 1), (6, 0, 6, 1));
        yield return Case("T41", "p\nq\nr", "s\nt", (0, 3, 0, 2));
        yield return Case("T42", "a\nb\nc", "x\ny\nz", (0, 3, 0, 3));
        yield return Case("T43", "a\n\nb", "a\nb", (1, 1, 1, 0));
        yield return Case("T44", "a\nb\nc", "X\nb\nY", (0, 1, 0, 1), (2, 1, 2, 1));
        yield return Case("T45", "a\nb\nc", "a\nX\nb\nY\nc", (1, 0, 1, 1), (2, 0, 3, 1));
        yield return Case("T46", "a", "x\na\ny", (0, 0, 0, 1), (1, 0, 2, 1));
        yield return Case("T47", "a\nb\nc\nd", "a\nc\nX\nd", (1, 1, 1, 0), (3, 0, 2, 1));
        yield return Case("T48", "1\n2\n3\n4\n5\n6", "1\nB\n3\nD\n5\nF", (1, 1, 1, 1), (3, 1, 3, 1), (5, 1, 5, 1));
        yield return Case("T49", "a\nb\nc\nd", "X\nb\nc\nY\nZ", (0, 1, 0, 1), (3, 1, 3, 2));
        yield return Case("T50", "a\nb", "a\nX\nY", (1, 1, 1, 2));
        yield return Case("T51", "a\nb\n", "a\nB\n", (1, 1, 1, 1));

        // Repeated lines, where leaving the common start and end alone decides the result
        yield return Case("T52", "a\nb", "a\nb\nb", (2, 0, 2, 1));
        yield return Case("T53", "a", "a\na", (1, 0, 1, 1));
        yield return Case("T54", "x\nx", "x", (1, 1, 1, 0));
        yield return Case("T55", "{\na\n}", "{\na\n}\n{\nb\n}", (3, 0, 3, 3));
        yield return Case("T56", Text(1000, _ => "x"), Text(999, _ => "x"), (999, 1, 999, 0));

        // Realistic RDMP content
        yield return Case("T57", "SELECT\n  [db]..[t].[chi],\n  [db]..[t].[age]\nFROM [db]..[t]",
            "SELECT\n  [db]..[t].[chi],\n  [db]..[t].[age],\n  [db]..[t].[sex]\nFROM [db]..[t]", (2, 1, 2, 2));
        yield return Case("T58", "Name: Biochemistry\nDescription: old\nFolder: \\",
            "Name: Biochemistry\nDescription: new\nFolder: \\", (1, 1, 1, 1));
        yield return Case("T59", "Biochemistry", "byochemistry", (0, 1, 0, 1));
        yield return Case("T60", "<Null>", "x", (0, 1, 0, 1));
        yield return Case("T61", "SELECT a\nFROM t", "SELECT a\nFROM t\nWHERE a > 1", (2, 0, 2, 1));
        yield return Case("T62",
            "SELECT\n  [t].[chi],\n  [t].[dob],\n  [t].[postcode]\nFROM [t]\nWHERE [t].[hb_extract] = 'T'\nORDER BY [t].[chi]",
            "SELECT\n  [t].[chi],\n  [t].[postcode],\n  [t].[sex]\nFROM [t]\nWHERE [t].[hb_extract] = 'T'\n  AND [t].[dob] > '2000-01-01'\nORDER BY [t].[chi]",
            (2, 2, 2, 2), (6, 0, 6, 1));
        yield return Case("T63", "{\nx\n}\n{\ny\n}", "{\nx\n}\n{\nz\n}", (4, 1, 4, 1));

        // Other characters that are not line breaks, and characters that must not cause an exception
        yield return Case("E01", "a\vb", "a\nb", (0, 1, 0, 2));
        yield return Case("E02", "a\u2029b", "a\nb", (0, 1, 0, 2));
        yield return Case("E03", "a\0b\nc", "a\0b\nd", (1, 1, 1, 1));
        yield return Case("E04", "\uD800", "\uDC00", (0, 1, 0, 1));
        yield return Case("E05", "x\n\uD800y", "x\n\uD800y");
    }

    [TestCaseSource(nameof(ExactCases))]
    public void DiffText_GivesTheOnlyMinimalResult(string before, string after, Hunk[] expected)
    {
        var items = Diff.DiffText(before, after);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(AsHunks(items), Is.EqualTo(expected));
            Assert.That(FindViolations(before, after, items), Is.Empty);
        }
    }

    [Test]
    public void DiffText_VeryLongLines()
    {
        var line = new string('x', 1_000_000);
        var before = $"{line}\nend";
        var after = $"{line}\nEND";

        var items = Diff.DiffText(before, after);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(AsHunks(items), Is.EqualTo(new Hunk[] { (1, 1, 1, 1) }));
            Assert.That(FindViolations(before, after, items), Is.Empty);
        }
    }

    [Test]
    public void DiffText_NullText_Throws()
    {
        using (Assert.EnterMultipleScope())
        {
            // T64 and T65
            Assert.That(() => Diff.DiffText(null, "x"), Throws.ArgumentNullException);
            Assert.That(() => Diff.DiffText("x", null), Throws.ArgumentNullException);
        }
    }

    #endregion

    #region Inputs with several minimal results

    private static IEnumerable<TestCaseData> TieCases()
    {
        yield return new TestCaseData("a\nb\nc\nd", "c\nd\na\nb", 4).SetArgDisplayNames("U1");
        yield return new TestCaseData("a\nb", "b\na", 2).SetArgDisplayNames("U2");
        yield return new TestCaseData("x\na\ny", "a\nx\ny", 2).SetArgDisplayNames("U3");

        // The example in Myers' paper
        yield return new TestCaseData("A\nB\nC\nA\nB\nB\nA", "C\nB\nA\nB\nA\nC", 5).SetArgDisplayNames("U4");
    }

    [TestCaseSource(nameof(TieCases))]
    public void DiffText_SeveralMinimalResults_GivesOneOfThem(string before, string after, int edits)
    {
        var items = Diff.DiffText(before, after);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items.Sum(static i => i.deletedA + i.insertedB), Is.EqualTo(edits));
            Assert.That(FindViolations(before, after, items), Is.Empty);
        }
    }

    #endregion

    #region Random inputs

    [Test]
    public void DiffText_RandomShortInputs_FollowTheRules()
    {
        // Few letters give many repeated lines and many equally short results
        int[] alphabetSizes = [1, 2, 3, 5];
        var random = new Random(1986);

        AssertRandomInputsFollowTheRules(30_000, () =>
        {
            var letters = alphabetSizes[random.Next(alphabetSizes.Length)];
            return (RandomLetters(random, random.Next(1, 14), letters),
                RandomLetters(random, random.Next(1, 14), letters));
        });
    }

    [Test]
    public void DiffText_RandomLongerInputs_FollowTheRules()
    {
        // Longer inputs, some with lines found in only one of the texts, need more levels of recursion
        var random = new Random(251);

        AssertRandomInputsFollowTheRules(2_000, () =>
        {
            var letters = random.Next(2, 27);
            return (RandomLetters(random, random.Next(1, 200), letters),
                RandomLetters(random, random.Next(1, 200), letters));
        });
    }

    private static void AssertRandomInputsFollowTheRules(int iterations, Func<(string Before, string After)> next)
    {
        var failures = new List<string>();
        var failureCount = 0;

        for (var i = 0; i < iterations; i++)
        {
            var (before, after) = next();
            var problems = FindViolations(before, after, Diff.DiffText(before, after));

            if (problems.Count == 0)
                continue;

            failureCount++;
            if (failures.Count < 10)
                failures.Add($"'{Escape(before)}' against '{Escape(after)}': {string.Join("; ", problems)}");
        }

        Assert.That(failures, Is.Empty, $"{failureCount} of {iterations} random inputs gave a wrong result");
    }

    private static string RandomLetters(Random random, int lines, int letters) =>
        Text(lines, _ => ((char)('a' + random.Next(letters))).ToString());

    #endregion

    #region Large inputs

    [Test]
    public void DiffText_L01_OneChangedLine()
    {
        var before = Text(10_000, static i => $"SELECT line {i}");
        var after = Text(10_000, static i => i == 5000 ? "CHANGED" : $"SELECT line {i}");

        AssertLargeInput(before, after, 2, (5000, 1, 5000, 1));
    }

    [Test]
    public void DiffText_L02_ScatteredChangedLines()
    {
        var before = Text(10_000, static i => $"SELECT line {i}");
        var after = Text(10_000, static i => i % 100 == 50 ? $"CHANGED {i}" : $"SELECT line {i}");
        var expected = Enumerable.Range(0, 100).Select(static j => (Hunk)(100 * j + 50, 1, 100 * j + 50, 1)).ToArray();

        AssertLargeInput(before, after, 2, expected);
    }

    [Test]
    public void DiffText_L03_DifferentTexts2000() =>
        AssertLargeInput(Text(2000, static i => $"A line {i}"), Text(2000, static i => $"B line {i}"), 2,
            (0, 2000, 0, 2000));

    [Test]
    public void DiffText_L04_DifferentTexts5000() =>
        AssertLargeInput(Text(5000, static i => $"A line {i}"), Text(5000, static i => $"B line {i}"), 2,
            (0, 5000, 0, 5000));

    [Test]
    public void DiffText_L05_SameText()
    {
        // Built twice so that the two texts are different string instances
        AssertLargeInput(Text(10_000, static i => $"SELECT line {i}"), Text(10_000, static i => $"SELECT line {i}"), 2);
    }

    [Test]
    public void DiffText_L06_RandomChangedLines()
    {
        const int count = 20_000;
        var random = new Random(6);
        var changed = Enumerable.Range(0, count).Select(_ => random.NextDouble() < 0.01).ToArray();

        var before = Text(count, static i => $"X line {i}");
        var after = Text(count, i => changed[i] ? $"X line {i} changed" : $"X line {i}");

        // One change for each run of consecutive changed lines (every changed line is different)
        var expected = new List<Hunk>();
        var line = 0;
        while (line < count)
        {
            if (!changed[line])
            {
                line++;
                continue;
            }

            var start = line;
            while (line < count && changed[line])
                line++;

            expected.Add((start, line - start, start, line - start));
        }

        Assert.That(expected, Is.Not.Empty);
        AssertLargeInput(before, after, 2, expected.ToArray());
    }

    [Test]
    public void DiffText_L07_DifferentTexts10000() =>
        AssertLargeInput(Text(10_000, static i => $"A line {i}"), Text(10_000, static i => $"B line {i}"), 5,
            (0, 10_000, 0, 10_000));

    [Test]
    public void DiffText_L08_OneChangedLineIn100000()
    {
        var before = Text(100_000, static i => $"Z line {i}");
        var after = Text(100_000, static i => i == 50_000 ? "changed" : $"Z line {i}");

        AssertLargeInput(before, after, 2, (50_000, 1, 50_000, 1));
    }

    [Test]
    public void DiffText_L09_EmptyAgainstLargeText() =>
        AssertLargeInput("", Text(10_000, static i => $"B line {i}"), 2, (0, 1, 0, 10_000));

    [Test]
    public void DiffText_L10_RandomLettersFollowTheRules()
    {
        var random = new Random(10);
        var before = RandomLetters(random, 3000, 3);
        var after = RandomLetters(random, 3000, 3);

        var (items, elapsed) = TimedDiff(before, after);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
            Assert.That(FindViolations(before, after, items), Is.Empty);
        }
    }

    [Test]
    public void DiffText_L11_DifferentTexts20000() =>
        AssertLargeInput(Text(20_000, static i => $"A line {i}"), Text(20_000, static i => $"B line {i}"), 10,
            (0, 20_000, 0, 20_000));

    [Test]
    public void DiffText_ReversedLines_FollowTheRules()
    {
        // Every line is in both texts but only one of them can be kept, so the search has to go through about twice as
        // many edits as there are lines
        const int count = 10_000;
        var before = Text(count, static i => $"line {i}");
        var after = Text(count, static i => $"line {count - 1 - i}");

        var (items, elapsed) = TimedDiff(before, after);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
            Assert.That(FindViolations(before, after, items, 2 * (count - 1)), Is.Empty);
        }
    }

    private static void AssertLargeInput(string before, string after, double limitInSeconds, params Hunk[] expected)
    {
        var (items, elapsed) = TimedDiff(before, after);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(AsHunks(items), Is.EqualTo(expected));
            Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(limitInSeconds)));
            Assert.That(FindViolations(before, after, items, expected.Sum(static h => h.deletedA + h.insertedB)),
                Is.Empty);
        }
    }

    private static (Diff.Item[] Items, TimeSpan Elapsed) TimedDiff(string before, string after)
    {
        // A small first call, so that the time measured does not include compiling the code
        Diff.DiffText("warm\nup", "warm\nup\ncall");

        var stopwatch = Stopwatch.StartNew();
        var items = Diff.DiffText(before, after);
        stopwatch.Stop();

        return (items, stopwatch.Elapsed);
    }

    #endregion

    #region Rules that every result follows

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
    private static List<string> FindViolations(string before, string after, Diff.Item[] items, int? minimalEdits = null)
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

    #endregion

    private static Hunk[] AsHunks(Diff.Item[] items) =>
        items.Select(static i => (i.StartA, i.deletedA, i.StartB, i.insertedB)).ToArray();

    private static string Text(int lines, Func<int, string> line) =>
        string.Join("\n", Enumerable.Range(0, lines).Select(line));

    private static string Escape(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n");
}
