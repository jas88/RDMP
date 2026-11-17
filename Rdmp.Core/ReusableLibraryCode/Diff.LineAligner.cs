// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    /// </summary>
    private sealed class LineAligner
    {
        private readonly List<Item> _changes = new();

        // Where the lines that both texts end with begin
        private readonly int _endA;
        private readonly int _endB;

        // The search lines: _keysA[i] is the number of line _indexA[i] of the first text, and the same for B
        private readonly int[] _keysA;
        private readonly int[] _indexA;
        private readonly int[] _keysB;
        private readonly int[] _indexB;

        // The furthest x reached on each diagonal by the forwards and the backwards search, at index diagonal + _offset
        private readonly int[] _forward;
        private readonly int[] _backward;
        private readonly int _offset;

        // The first line of each text after the last pair of lines kept so far
        private int _nextA;
        private int _nextB;

        public LineAligner(int[] linesA, int[] linesB, int keyCount)
        {
            var start = 0;
            while (start < linesA.Length && start < linesB.Length && linesA[start] == linesB[start])
                start++;

            _endA = linesA.Length;
            _endB = linesB.Length;
            while (_endA > start && _endB > start && linesA[_endA - 1] == linesB[_endB - 1])
            {
                _endA--;
                _endB--;
            }

            _nextA = start;
            _nextB = start;

            // Bit 1 set: the line occurs in the middle of the first text, bit 2: in the middle of the second
            var occurs = new byte[keyCount];
            for (var i = start; i < _endA; i++)
                occurs[linesA[i]] |= 1;
            for (var i = start; i < _endB; i++)
                occurs[linesB[i]] |= 2;

            (_keysA, _indexA) = SelectSearchLines(linesA, start, _endA, occurs);
            (_keysB, _indexB) = SelectSearchLines(linesB, start, _endB, occurs);

            // No search goes beyond this many edits, see FindMiddleSnake
            _offset = (_keysA.Length + _keysB.Length + 1) / 2;
            _forward = new int[2 * _offset + 1];
            _backward = new int[2 * _offset + 1];
        }

        private static (int[] Keys, int[] Index) SelectSearchLines(int[] lines, int from, int to, byte[] occurs)
        {
            var keys = new List<int>();
            var index = new List<int>();

            for (var i = from; i < to; i++)
            {
                if (occurs[lines[i]] != 3)
                    continue;

                keys.Add(lines[i]);
                index.Add(i);
            }

            return (keys.ToArray(), index.ToArray());
        }

        public Item[] GetChanges()
        {
            Align(0, _keysA.Length, 0, _keysB.Length);

            // Whatever has not been kept before the common end is the last change
            AddChangeBefore(_endA, _endB);

            return _changes.ToArray();
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
                var lineA = _indexA[x + i];
                var lineB = _indexB[y + i];

                AddChangeBefore(lineA, lineB);
                _nextA = lineA + 1;
                _nextB = lineB + 1;
            }
        }

        /// <summary>
        /// Adds a change for the lines from the last kept pair up to (not including) line <paramref name="lineA"/> of the
        /// first text and line <paramref name="lineB"/> of the second, if there are any
        /// </summary>
        private void AddChangeBefore(int lineA, int lineB)
        {
            if (lineA > _nextA || lineB > _nextB)
                _changes.Add(new Item(_nextA, lineA - _nextA, _nextB, lineB - _nextB));
        }
    }
}
