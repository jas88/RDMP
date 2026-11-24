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
using static Rdmp.Core.Tests.ReusableCodeTests.DiffTestHelpers;
using Hunk = (int StartA, int deletedA, int StartB, int insertedB);

namespace Rdmp.Core.Tests.ReusableCodeTests;

/// <summary>
/// Tests <see cref="Diff.DiffText"/> on large texts: that its results are right, and that the time and memory it takes
/// grow no faster than they should as the texts get longer. Each growth check compares measurements taken in the same
/// test, so it holds however fast the machine is, and when code coverage slows everything down.
/// </summary>
public class DiffSizeTests
{
    #region Results for large texts

    [Test]
    public void DiffText_L01_OneChangedLine() => AssertLargeInput(OneChangedLine(10_000), (5000, 1, 5000, 1));

    [Test]
    public void DiffText_L02_ScatteredChangedLines() =>
        AssertLargeInput(ScatteredChangedLines(10_000),
            Enumerable.Range(0, 100).Select(static j => (100 * j + 50, 1, 100 * j + 50, 1)).ToArray());

    [Test]
    public void DiffText_L03_DifferentTexts2000() => AssertLargeInput(DifferentTexts(2000), (0, 2000, 0, 2000));

    [Test]
    public void DiffText_L04_DifferentTexts5000() => AssertLargeInput(DifferentTexts(5000), (0, 5000, 0, 5000));

    [Test]
    public void DiffText_L05_SameText()
    {
        // Built twice so that the two texts are different string instances
        AssertLargeInput((Text(10_000, static i => $"SELECT line {i}"), Text(10_000, static i => $"SELECT line {i}")));
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
        AssertLargeInput((before, after), expected.ToArray());
    }

    [Test]
    public void DiffText_L07_DifferentTexts10000() =>
        AssertLargeInput(DifferentTexts(10_000), (0, 10_000, 0, 10_000));

    [Test]
    public void DiffText_L08_OneChangedLineIn100000() =>
        AssertLargeInput(
            (Text(100_000, static i => $"Z line {i}"), Text(100_000, static i => i == 50_000 ? "changed" : $"Z line {i}")),
            (50_000, 1, 50_000, 1));

    [Test]
    public void DiffText_L09_EmptyAgainstLargeText() =>
        AssertLargeInput(("", Text(10_000, static i => $"B line {i}")), (0, 1, 0, 10_000));

    [Test]
    public void DiffText_L10_RandomLettersFollowTheRules()
    {
        var random = new Random(10);
        var before = RandomLetters(random, 3000, 3);
        var after = RandomLetters(random, 3000, 3);

        Assert.That(FindViolations(before, after, Diff.DiffText(before, after)), Is.Empty);
    }

    [Test]
    public void DiffText_L11_DifferentTexts20000() =>
        AssertLargeInput(DifferentTexts(20_000), (0, 20_000, 0, 20_000));

    [Test]
    public void DiffText_ReversedLines_FollowTheRules()
    {
        // Only one line can be kept, so the search goes through about twice as many edits as there are lines. The time
        // this takes grows with the square of the line count, and code coverage makes the search many times slower, so
        // the text is no longer than the largest one in the growth check for reversed lines below.
        const int count = 2000;
        var (before, after) = ReversedLines(count);

        Assert.That(FindViolations(before, after, Diff.DiffText(before, after), 2 * (count - 1)), Is.Empty);
    }

    private static void AssertLargeInput((string Before, string After) texts, params Hunk[] expected)
    {
        var items = Diff.DiffText(texts.Before, texts.After);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(AsHunks(items), Is.EqualTo(expected));
            Assert.That(
                FindViolations(texts.Before, texts.After, items, expected.Sum(static h => h.deletedA + h.insertedB)),
                Is.Empty);
        }
    }

    #endregion

    #region Growth of time and memory

    // Each growth check measures texts of n and of 8n lines, and allows up to 2.5 times the growth expected. That is
    // still well short of what the next power of n would give, which is 8 times as much again.
    private const int LengthFactor = 8;
    private const double Allowance = 2.5;
    private const double LinearLimit = Allowance * LengthFactor;
    private const double QuadraticLimit = Allowance * LengthFactor * LengthFactor;

    private static TestCaseData Family(string name, Func<int, (string Before, string After)> texts) =>
        new TestCaseData(texts).SetArgDisplayNames(name);

    private static IEnumerable<TestCaseData> FewChangedLines()
    {
        // As in L1 and L8
        yield return Family("one changed line", OneChangedLine);

        // As in L2 and L6. Lines found in only one text are left out of the search.
        yield return Family("scattered changed lines", ScatteredChangedLines);

        // As in L3, L4, L7, L9 and L11, which therefore take linear time too
        yield return Family("no line in common", DifferentTexts);

        // Every line is in both texts and the same number of lines change, so this times the search itself
        yield return Family("swapped lines", SwappedLines);
    }

    [TestCaseSource(nameof(FewChangedLines))]
    public void DiffText_FewChangedLines_TimeAndMemoryGrowLinearly(Func<int, (string Before, string After)> texts)
    {
        var (time, memory) = MeasureGrowth(texts, 2500);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(time, Is.LessThan(LinearLimit), "Time grew faster than linearly");
            Assert.That(memory, Is.LessThan(LinearLimit), "Memory grew faster than linearly");
        }
    }

    private static IEnumerable<TestCaseData> ManyChangedLines()
    {
        yield return Family("reversed lines", ReversedLines);

        // As in L10
        yield return Family("random letters", RandomThreeLetters);
    }

    [TestCaseSource(nameof(ManyChangedLines))]
    public void DiffText_ManyChangedLines_TimeGrowsQuadraticallyAndMemoryLinearly(
        Func<int, (string Before, string After)> texts)
    {
        // The number of changed lines D grows with the texts, so the O((N+M)D) search takes quadratic time. Memory is
        // still O(N+M).
        var (time, memory) = MeasureGrowth(texts, 250);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(time, Is.LessThan(QuadraticLimit), "Time grew faster than quadratically");
            Assert.That(memory, Is.LessThan(LinearLimit), "Memory grew faster than linearly");
        }
    }

    /// <summary>
    /// Compares <see cref="Diff.DiffText"/> on the texts made for <paramref name="lines"/> lines and for
    /// <see cref="LengthFactor"/> times as many, and returns how many times longer the larger takes and how many times
    /// more memory it allocates. After warming up, each is timed five times, alternately, and the fastest time of each
    /// is used, so that a slow or busy machine affects both alike.
    /// </summary>
    private static (double Time, double Memory) MeasureGrowth(Func<int, (string Before, string After)> texts, int lines)
    {
        var smaller = lines;
        var larger = lines * LengthFactor;
        var small = texts(smaller);
        var large = texts(larger);

        // Enough calls for the runtime to optimise the code before it is measured
        for (var i = 0; i < 50; i++)
            Diff.DiffText("x\na\nb\nGO\nGO\ny", "x\nb\na\nGO \nGO\ny2");

        Measure(small);
        Measure(large);

        var (smallTicks, smallBytes) = (long.MaxValue, long.MaxValue);
        var (largeTicks, largeBytes) = (long.MaxValue, long.MaxValue);
        for (var i = 0; i < 5; i++)
        {
            var (ticks, bytes) = Measure(small);
            (smallTicks, smallBytes) = (Math.Min(smallTicks, ticks), Math.Min(smallBytes, bytes));

            (ticks, bytes) = Measure(large);
            (largeTicks, largeBytes) = (Math.Min(largeTicks, ticks), Math.Min(largeBytes, bytes));
        }

        var time = (double)largeTicks / Math.Max(1, smallTicks);
        var memory = (double)largeBytes / Math.Max(1, smallBytes);

        TestContext.Out.WriteLine(
            $"{smaller} lines: {Milliseconds(smallTicks):F2} ms, {smallBytes / 1024} KB. {larger} lines: " +
            $"{Milliseconds(largeTicks):F2} ms, {largeBytes / 1024} KB. Growth: time {time:F1}, memory {memory:F1}");

        return (time, memory);
    }

    private static (long Ticks, long Bytes) Measure((string Before, string After) texts)
    {
        // Clear away earlier garbage, so that collecting it is not counted
        GC.Collect(0);

        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();

        Diff.DiffText(texts.Before, texts.After);

        stopwatch.Stop();
        return (stopwatch.ElapsedTicks, GC.GetAllocatedBytesForCurrentThread() - bytes);
    }

    private static double Milliseconds(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    #endregion

    #region Texts

    private static (string Before, string After) OneChangedLine(int lines) =>
        (Text(lines, static i => $"SELECT line {i}"), Text(lines, i => i == lines / 2 ? "CHANGED" : $"SELECT line {i}"));

    private static (string Before, string After) ScatteredChangedLines(int lines) =>
        (Text(lines, static i => $"SELECT line {i}"),
            Text(lines, static i => i % 100 == 50 ? $"CHANGED {i}" : $"SELECT line {i}"));

    private static (string Before, string After) DifferentTexts(int lines) =>
        (Text(lines, static i => $"A line {i}"), Text(lines, static i => $"B line {i}"));

    // Twenty pairs of neighbouring lines swapped, spread evenly through the text, so that 40 lines change
    private static (string Before, string After) SwappedLines(int lines)
    {
        var gap = lines / 21;
        return (Text(lines, static i => $"line {i}"), Text(lines, i =>
        {
            var pair = i / gap;
            if (pair is < 1 or > 20)
                return $"line {i}";

            return (i % gap) switch
            {
                0 => $"line {i + 1}",
                1 => $"line {i - 1}",
                _ => $"line {i}"
            };
        }));
    }

    private static (string Before, string After) ReversedLines(int lines) =>
        (Text(lines, static i => $"line {i}"), Text(lines, i => $"line {lines - 1 - i}"));

    // Random lines of the letters a, b and c
    private static (string Before, string After) RandomThreeLetters(int lines)
    {
        var random = new Random(lines);
        return (RandomLetters(random, lines, 3), RandomLetters(random, lines, 3));
    }

    #endregion
}
