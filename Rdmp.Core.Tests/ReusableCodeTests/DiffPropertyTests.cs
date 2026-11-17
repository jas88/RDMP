// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode;
using static Rdmp.Core.Tests.ReusableCodeTests.DiffTestHelpers;

namespace Rdmp.Core.Tests.ReusableCodeTests;

/// <summary>
/// Tests that the results of <see cref="Diff.DiffText"/> follow all the rules (see
/// <see cref="DiffTestHelpers.FindViolations"/>) for inputs with several minimal results and for random inputs
/// </summary>
public class DiffPropertyTests
{
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

    [Test]
    public void DiffText_RandomShortInputs_FollowTheRules()
    {
        // Few letters give many repeated lines and many equally short results
        int[] alphabetSizes = [1, 2, 3, 5];
        var random = new Random(1986);

        AssertAllFollowTheRules(Enumerable.Range(0, 30_000).Select(_ =>
        {
            var letters = alphabetSizes[random.Next(alphabetSizes.Length)];
            return (RandomLetters(random, random.Next(1, 14), letters),
                RandomLetters(random, random.Next(1, 14), letters));
        }));
    }

    [Test]
    public void DiffText_RandomLongerInputs_FollowTheRules()
    {
        // Longer inputs, some with lines found in only one of the texts, need more levels of recursion
        var random = new Random(251);

        AssertAllFollowTheRules(Enumerable.Range(0, 2_000).Select(_ =>
        {
            var letters = random.Next(2, 27);
            return (RandomLetters(random, random.Next(1, 200), letters),
                RandomLetters(random, random.Next(1, 200), letters));
        }));
    }

    private static void AssertAllFollowTheRules(IEnumerable<(string Before, string After)> inputs)
    {
        var failures = new List<string>();
        var count = 0;
        var failureCount = 0;

        foreach (var (before, after) in inputs)
        {
            count++;
            var problems = FindViolations(before, after, Diff.DiffText(before, after));

            if (problems.Count == 0)
                continue;

            failureCount++;
            if (failures.Count < 10)
                failures.Add($"'{Escape(before)}' against '{Escape(after)}': {string.Join("; ", problems)}");
        }

        Assert.That(failures, Is.Empty, $"{failureCount} of {count} inputs gave a wrong result");
    }
}
