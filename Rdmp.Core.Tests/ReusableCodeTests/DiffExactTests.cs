// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode;
using static Rdmp.Core.Tests.ReusableCodeTests.DiffTestHelpers;
using Hunk = (int StartA, int deletedA, int StartB, int insertedB);

namespace Rdmp.Core.Tests.ReusableCodeTests;

/// <summary>
/// Tests that <see cref="Diff.DiffText"/> gives exactly the expected changes, for inputs where only one result follows
/// all the rules (see <see cref="DiffTestHelpers.FindViolations"/>), and that it rejects null
/// </summary>
public class DiffExactTests
{
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
        yield return Case("E02", "a" + (char)0x2029 + "b", "a\nb", (0, 1, 0, 2));
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
}
