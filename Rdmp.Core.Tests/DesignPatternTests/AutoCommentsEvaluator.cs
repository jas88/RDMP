// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Rdmp.Core.Repositories;

namespace Rdmp.Core.Tests.DesignPatternTests;

/// <summary>
/// Checks that [NoMappingToDatabase] properties with a corresponding _ID property inherit its documentation and that
/// paragraph breaks in summary comments use para tags
/// </summary>
public static partial class AutoCommentsEvaluator
{
    public static void FindProblems(List<string> csFilesFound)
    {
        var suggestedNewFileContents = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        var sbSuggestedText = new StringBuilder();

        foreach (var f in csFilesFound.Where(static f => !f.Contains(".Designer.cs")))
        {
            var changes = false;

            sbSuggestedText.Clear();

            var text = File.ReadAllLines(f);
            var areInSummary = false;
            var paraOpened = false;

            for (var i = 0; i < text.Length; i++)
            {
                //////////////////////////////////No Mapping Properties////////////////////////////////////////////////////
                if (text[i].Trim().Equals("[NoMappingToDatabase]"))
                {
                    var currentClassName = GetUniqueTypeName(Path.GetFileNameWithoutExtension(f));

                    var t = MEF.GetType(currentClassName);

                    //if the previous line isn't a summary comment
                    if (i > 0 && i + 1 < text.Length && !text[i - 1].Trim().StartsWith("///"))
                    {
                        var next = text[i + 1];

                        var m = PublicRegex().Match(next);
                        if (m.Success)
                        {
                            var whitespace = m.Groups[1].Value;
                            var member = m.Groups[3].Value;

                            Assert.Multiple(() =>
                            {
                                Assert.That(string.IsNullOrWhiteSpace(whitespace));
                                Assert.That(t, Is.Not.Null, $"MEF.GetType() returned null for class '{currentClassName}' in file '{f}'. The assembly containing this type may not be loaded.");
                            });

                            if (t.GetProperty($"{member}_ID") != null)
                            {
                                changes = true;
                                sbSuggestedText.AppendLine(whitespace + $"/// <inheritdoc cref=\"{member}_ID\"/>");
                            }
                            else
                            {
                                sbSuggestedText.AppendLine(text[i]);
                                continue;
                            }
                        }
                    }
                }


                if (text[i].Trim().Equals("/// <summary>"))
                {
                    areInSummary = true;
                    paraOpened = false;
                }

                if (text[i].Trim().Equals("/// </summary>"))
                {
                    if (paraOpened)
                    {
                        //
                        sbSuggestedText.Insert(sbSuggestedText.Length - Environment.NewLine.Length, "</para>");
                        paraOpened = false;
                    }

                    areInSummary = false;
                }

                //if we have a paragraph break in the summary comments and the next line isn't an end summary
                if (areInSummary && i + 1 < text.Length && text[i].Trim().Equals("///") && !text[i + 1].Trim().Equals("/// </summary>"))
                {
                    if (paraOpened)
                    {
                        sbSuggestedText.Insert(sbSuggestedText.Length - Environment.NewLine.Length, "</para>");
                        paraOpened = false;
                    }

                    //there should be a para tag
                    if (!text[i + 1].Contains("<para>") && text[i + 1].Contains("///"))
                    {
                        changes = true;

                        //add current line
                        sbSuggestedText.AppendLine(text[i]);

                        //add the para tag
                        var commentStart = text[i + 1].IndexOf("///", StringComparison.Ordinal);
                        if (commentStart >= 0 && text[i + 1].Length > commentStart + 4)
                        {
                            var nextLine = text[i + 1].Insert(commentStart + 4, "<para>");
                            sbSuggestedText.AppendLine(nextLine);
                        }
                        else
                        {
                            // Line too short to insert para tag, just add as-is
                            sbSuggestedText.AppendLine(text[i + 1]);
                        }
                        i++;
                        paraOpened = true;
                        continue;
                    }
                }

                sbSuggestedText.AppendLine(text[i]);
            }

            if (changes)
                suggestedNewFileContents.Add(f, sbSuggestedText.ToString());
        }


        //drag your debugger stack pointer to here to mess up all your files to match the suggestedNewFileContents :)
        foreach (var kvp in suggestedNewFileContents)
        {
            var actualContents = File.ReadAllText(kvp.Key);
            var suggestedContents = kvp.Value;
            Assert.That(actualContents, Is.EqualTo(suggestedContents), $"Auto-comment mismatch in {kvp.Key}");
        }
    }

    private static string GetUniqueTypeName(string typename)
    {
        return typename switch
        {
            "ColumnInfo" => "Rdmp.Core.Curation.Data.ColumnInfo",
            "IFilter" => "Rdmp.Core.Curation.Data.IFilter",
            _ => typename
        };
    }

    [GeneratedRegex("(.*)public\\b(.*)\\s+(.*)\\b")]
    private static partial Regex PublicRegex();
}
