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
/// Tests that <see cref="Diff.DiffText"/> handles large texts correctly and within generous time limits, which are there
/// to catch a slower algorithm rather than to measure speed
/// </summary>
public class DiffSizeTests
{
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
}
