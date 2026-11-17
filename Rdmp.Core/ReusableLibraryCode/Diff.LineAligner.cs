// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rdmp.Core.ReusableLibraryCode;

// The comparison itself, kept apart from the public members in Diff.cs
public static partial class Diff
{
    /// <summary>
    /// Finds a longest common subsequence of two texts, given as line numbers from <see cref="KeyLines"/>, and reports
    /// the lines between its members as changes.
    /// <para>Lines that both texts start with, then lines that both end with, are kept without searching. Of the lines
    /// left in the middle, those found only in one text can never be kept, so the search runs on the others: the search
    /// lines. It splits the problem at a middle snake (Myers section 4b) and recurses on the two halves.</para>
    /// <para>The lines in the middle start out changed, and the search marks the ones it keeps. The runs of changed lines
    /// are then moved over equal lines (see <see cref="SlideRuns"/>) before the changes are read off.</para>
    /// </summary>
    private sealed class LineAligner
    {
        // Line numbers of the two whole texts
        private readonly int[] _linesA;
        private readonly int[] _linesB;

        // The lines that both texts start with end at _start, and the lines that both end with begin at _endA and _endB
        private readonly int _start;
        private readonly int _endA;
        private readonly int _endB;

        // Which lines of the first text are deleted and which lines of the second are inserted
        private readonly bool[] _changedA;
        private readonly bool[] _changedB;

        // The search lines: _keysA[i] is the number of line _indexA[i] of the first text, and the same for B
        private readonly int[] _keysA;
        private readonly int[] _indexA;
        private readonly int[] _keysB;
        private readonly int[] _indexB;

        // The furthest x reached on each diagonal by the forwards and the backwards search, at index diagonal + _offset
        private readonly int[] _forward;
        private readonly int[] _backward;
        private readonly int _offset;

        public LineAligner(int[] linesA, int[] linesB, int keyCount)
        {
            _linesA = linesA;
            _linesB = linesB;
            (_start, _endA, _endB) = FindCommonStartAndEnd(linesA, linesB);

            // The lines in between are changed until the search keeps them
            _changedA = new bool[linesA.Length];
            _changedB = new bool[linesB.Length];
            Array.Fill(_changedA, true, _start, _endA - _start);
            Array.Fill(_changedB, true, _start, _endB - _start);

            var inBoth = FindLinesInBothMiddles(linesA, linesB, _start, _endA, _endB, keyCount);
            (_keysA, _indexA) = SelectSearchLines(linesA, _start, _endA, inBoth);
            (_keysB, _indexB) = SelectSearchLines(linesB, _start, _endB, inBoth);

            // No search goes beyond this many edits, see FindMiddleSnake
            _offset = (_keysA.Length + _keysB.Length + 1) / 2;
            _forward = new int[2 * _offset + 1];
            _backward = new int[2 * _offset + 1];
        }

        /// <summary>
        /// Returns how many lines both texts start with, and then where the lines that both end with (of those left)
        /// begin in each text
        /// </summary>
        private static (int Start, int EndA, int EndB) FindCommonStartAndEnd(int[] linesA, int[] linesB)
        {
            var start = 0;
            while (start < linesA.Length && start < linesB.Length && linesA[start] == linesB[start])
                start++;

            var endA = linesA.Length;
            var endB = linesB.Length;
            while (endA > start && endB > start && linesA[endA - 1] == linesB[endB - 1])
            {
                endA--;
                endB--;
            }

            return (start, endA, endB);
        }

        /// <summary>
        /// Returns, for each line number, whether that line occurs both in lines <c>[start, endA)</c> of the first text
        /// and in lines <c>[start, endB)</c> of the second. No other line in those ranges can be kept.
        /// </summary>
        private static bool[] FindLinesInBothMiddles(int[] linesA, int[] linesB, int start, int endA, int endB,
            int keyCount)
        {
            var inMiddleOfA = new bool[keyCount];
            for (var i = start; i < endA; i++)
                inMiddleOfA[linesA[i]] = true;

            var inBoth = new bool[keyCount];
            for (var i = start; i < endB; i++)
                inBoth[linesB[i]] = inMiddleOfA[linesB[i]];

            return inBoth;
        }

        private static (int[] Keys, int[] Index) SelectSearchLines(int[] lines, int from, int to, bool[] inBoth)
        {
            var keys = new List<int>();
            var index = new List<int>();

            for (var i = from; i < to; i++)
            {
                if (!inBoth[lines[i]])
                    continue;

                keys.Add(lines[i]);
                index.Add(i);
            }

            return (keys.ToArray(), index.ToArray());
        }

        public Item[] GetChanges()
        {
            Align(0, _keysA.Length, 0, _keysB.Length);

            SlideRuns(_linesA, _changedA, _start, _endA, ChangedBetweenKeptLines(_changedB));
            SlideRuns(_linesB, _changedB, _start, _endB, ChangedBetweenKeptLines(_changedA));

            return ReadChanges();
        }

        /// <summary>
        /// Keeps, in order, the members of a longest common subsequence of search lines <c>[aLo, aHi)</c> of the first
        /// text and <c>[bLo, bHi)</c> of the second
        /// </summary>
        private void Align(int aLo, int aHi, int bLo, int bHi)
        {
            // Matching lines at the start can be kept straight away, and those at the end once the rest is done
            var firstA = aLo;
            var firstB = bLo;
            while (aLo < aHi && bLo < bHi && _keysA[aLo] == _keysB[bLo])
            {
                aLo++;
                bLo++;
            }

            Keep(firstA, firstB, aLo - firstA);

            var tail = 0;
            while (aLo < aHi - tail && bLo < bHi - tail && _keysA[aHi - 1 - tail] == _keysB[bHi - 1 - tail])
                tail++;

            aHi -= tail;
            bHi -= tail;

            // If either side is empty now, nothing else here can be kept
            if (aLo < aHi && bLo < bHi)
            {
                var (x, y, u, v) = FindMiddleSnake(aLo, aHi, bLo, bHi);
                Align(aLo, x, bLo, y);
                Keep(x, y, u - x);
                Align(u, aHi, v, bHi);
            }

            Keep(aHi, bHi, tail);
        }

        /// <summary>
        /// Finds a snake (a run of matching lines, possibly empty) half way along a shortest edit script for search lines
        /// <c>[aLo, aHi)</c> of the first text and <c>[bLo, bHi)</c> of the second, both of which must be non-empty. A
        /// forwards search from the start and a backwards search from the end each go one edit further at a time, as in
        /// Myers section 4b, until they overlap on a diagonal.
        /// </summary>
        /// <returns>Where the snake starts, (X, Y), and ends, (U, V), as indexes of search lines</returns>
        private (int X, int Y, int U, int V) FindMiddleSnake(int aLo, int aHi, int bLo, int bHi)
        {
            var n = aHi - aLo;
            var m = bHi - bLo;
            var delta = n - m;
            var deltaIsOdd = (delta & 1) != 0;

            // The searches meet after at most half of the n + m edits that the longest script has
            var maxD = (n + m + 1) / 2;

            // Diagonal k of the forwards search holds the points (x, x - k), counted from (aLo, bLo). The backwards search
            // runs forwards over both ranges reversed: its point (x, y) is (n - x, m - y) going forwards, and its
            // diagonal c is diagonal delta - c of the forwards search.
            for (var d = 0; d <= maxD; d++)
            {
                for (var k = -d; k <= d; k += 2)
                {
                    var x = SnakeStart(_forward, d, k, n, m);
                    if (x < 0)
                    {
                        _forward[_offset + k] = -1;
                        continue;
                    }

                    var startX = x;
                    var y = x - k;
                    while (x < n && y < m && _keysA[aLo + x] == _keysB[bLo + y])
                    {
                        x++;
                        y++;
                    }

                    _forward[_offset + k] = x;

                    // When delta is odd the searches first overlap here, on a diagonal the backwards search reached
                    // with d - 1 edits
                    if (deltaIsOdd && Math.Abs(delta - k) < d)
                    {
                        var backX = _backward[_offset + delta - k];
                        if (backX >= 0 && x + backX >= n)
                            return (aLo + startX, bLo + startX - k, aLo + x, bLo + y);
                    }
                }

                for (var c = -d; c <= d; c += 2)
                {
                    var x = SnakeStart(_backward, d, c, n, m);
                    if (x < 0)
                    {
                        _backward[_offset + c] = -1;
                        continue;
                    }

                    var startX = x;
                    var y = x - c;
                    while (x < n && y < m && _keysA[aHi - 1 - x] == _keysB[bHi - 1 - y])
                    {
                        x++;
                        y++;
                    }

                    _backward[_offset + c] = x;

                    // When delta is even the searches first overlap here, on a diagonal the forwards search reached
                    // with d edits
                    if (!deltaIsOdd && Math.Abs(delta - c) <= d)
                    {
                        var forwardX = _forward[_offset + delta - c];
                        if (forwardX >= 0 && forwardX + x >= n)
                            return (aHi - x, bHi - y, aHi - startX, bHi - startX + c);
                    }
                }
            }

            throw new UnreachableException("The forwards and backwards searches did not meet");
        }

        /// <summary>
        /// Returns the x at which a furthest reaching path of <paramref name="d"/> edits on diagonal <paramref name="k"/>
        /// makes its last edit, ready to follow the snake there. The path extends the furthest reaching path of one edit
        /// fewer on a neighbouring diagonal (Myers lemma 2). Returns -1 if no such path stays inside the n by m grid.
        /// </summary>
        private int SnakeStart(int[] furthest, int d, int k, int n, int m)
        {
            if (d == 0)
                return 0;

            // Delete a line, moving across from diagonal k - 1
            var across = -1;
            if (k > -d)
            {
                var x = furthest[_offset + k - 1];
                if (x >= 0 && x < n)
                    across = x + 1;
            }

            // Insert a line, moving down from diagonal k + 1
            var down = -1;
            if (k < d)
            {
                var x = furthest[_offset + k + 1];
                if (x >= 0 && x - (k + 1) < m)
                    down = x;
            }

            return Math.Max(across, down);
        }

        /// <summary>
        /// Keeps <paramref name="length"/> pairs of matching search lines, starting with search line <paramref name="x"/>
        /// of the first text and <paramref name="y"/> of the second
        /// </summary>
        private void Keep(int x, int y, int length)
        {
            for (var i = 0; i < length; i++)
            {
                _changedA[_indexA[x + i]] = false;
                _changedB[_indexB[y + i]] = false;
            }
        }

        /// <summary>
        /// Returns, for each r from 0 to the number of kept lines in <paramref name="changed"/>, whether the text has a
        /// changed line between its kept lines r - 1 and r (before the first kept line when r is 0, and after the last
        /// one when r is the number of kept lines)
        /// </summary>
        private static bool[] ChangedBetweenKeptLines(bool[] changed)
        {
            var result = new bool[changed.Count(static isChanged => !isChanged) + 1];
            var kept = 0;
            foreach (var isChanged in changed)
            {
                if (isChanged)
                    result[kept] = true;
                else
                    kept++;
            }

            return result;
        }

        /// <summary>
        /// Moves each run of changed lines of one text over the equal lines next to it: to the lowest place where it joins
        /// a change of the other text between the same two kept lines, or else as far down as it can go. Runs that meet
        /// become one run. GNU diff and git tidy their results in the same way.
        /// <para>Each move swaps a kept line for an equal changed line at the other end of the run, so the kept lines read
        /// the same and the number of changed lines does not change: the result is still a shortest edit script. Runs stay
        /// within <c>[lo, hi)</c>, so the lines that both texts start and end with are never changed.</para>
        /// </summary>
        /// <param name="lines">Line numbers of the text, from <see cref="KeyLines"/></param>
        /// <param name="changed">Which lines of the text are changed, updated in place</param>
        /// <param name="lo">The first line that can be changed</param>
        /// <param name="hi">The line after the last one that can be changed</param>
        /// <param name="otherChanged">For the other text, from <see cref="ChangedBetweenKeptLines"/></param>
        private static void SlideRuns(int[] lines, bool[] changed, int lo, int hi, bool[] otherChanged)
        {
            // The lines before lo are all kept
            var kept = lo;
            var i = lo;

            while (i < hi)
            {
                if (!changed[i])
                {
                    kept++;
                    i++;
                    continue;
                }

                var end = i + 1;
                while (end < hi && changed[end])
                    end++;

                (i, kept) = PlaceRun(lines, changed, lo, hi, otherChanged, i, end, kept);
            }
        }

        /// <summary>
        /// Moves the run of changed lines <c>[start, end)</c>, which has <paramref name="kept"/> kept lines before it, as
        /// described on <see cref="SlideRuns"/>
        /// </summary>
        /// <returns>The end of the run where it is placed, and the number of kept lines before it there</returns>
        private static (int End, int Kept) PlaceRun(int[] lines, bool[] changed, int lo, int hi, bool[] otherChanged,
            int start, int end, int kept)
        {
            // Slide to the top and then to the bottom of the equal lines around the run, joining the runs it meets, until
            // it stops growing
            int length;
            int topKept;
            do
            {
                length = end - start;

                // Up, while the kept line above is equal to the last changed line, which becomes kept instead
                while (start > lo && lines[start - 1] == lines[end - 1])
                {
                    changed[--start] = true;
                    changed[--end] = false;
                    kept--;

                    while (start > lo && changed[start - 1])
                        start--;
                }

                topKept = kept;

                // Down, while the kept line below is equal to the first changed line
                while (end < hi && lines[start] == lines[end])
                {
                    changed[start++] = false;
                    changed[end++] = true;
                    kept++;

                    while (end < hi && changed[end])
                        end++;
                }
            }
            while (end - start != length);

            // The run can sit anywhere from topKept to kept kept lines down without meeting another run. Go back up to the
            // lowest of those places where the other text has a change between the same two kept lines, if there is one
            for (var target = kept; target >= topKept; target--)
            {
                if (!otherChanged[target])
                    continue;

                for (; kept > target; kept--)
                {
                    changed[--start] = true;
                    changed[--end] = false;
                }

                break;
            }

            return (end, kept);
        }

        /// <summary>
        /// Reads the changes off the changed lines: the changed lines between two pairs of kept lines are one change
        /// </summary>
        private Item[] ReadChanges()
        {
            var changes = new List<Item>();
            var a = 0;
            var b = 0;

            while (true)
            {
                var startA = a;
                var startB = b;
                while (a < _changedA.Length && _changedA[a])
                    a++;
                while (b < _changedB.Length && _changedB[b])
                    b++;

                if (a > startA || b > startB)
                    changes.Add(new Item(startA, a - startA, startB, b - startB));

                // Lines a and b are now a pair of kept lines, unless the texts have ended
                if (a == _changedA.Length || b == _changedB.Length)
                    return changes.ToArray();

                a++;
                b++;
            }
        }
    }
}
