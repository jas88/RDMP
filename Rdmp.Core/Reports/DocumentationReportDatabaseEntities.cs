// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

//specially for MSword file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.Icons;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Comments;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;

namespace Rdmp.Core.Reports;

/// <summary>
/// Generates class descriptions for all <see cref="DatabaseEntity"/> classes (from xmldocs) into a user readable Microsoft Word Docx file.
/// Also shows corresponding icon within RDMP.  This allows the user to see what a Project is and the icon what an ExtractionConfiguration
/// is etc and for those descriptions/icons to always match the live/installed version of RDMP.
/// </summary>
public class DocumentationReportDatabaseEntities : DocXHelper
{
    private CommentStore _commentStore;
    private Dictionary<Type, string> Summaries = new();

    /// <summary>
    /// Writes the report to a new .docx file in the temp directory
    /// </summary>
    /// <param name="commentStore">Documentation for the classes</param>
    /// <param name="notifier">Where to report progress/failures</param>
    /// <param name="iconProvider">Provides the icon shown next to each class</param>
    /// <param name="showFile">True to open the generated file once complete</param>
    /// <param name="iconRenderer">Optional function that renders an <see cref="RdmpIcon"/> (including overlays etc) to PNG data.
    /// If not provided then the base image of each icon is used (<see cref="RdmpIcon.Operations"/> are ignored).</param>
    /// <returns>The file written, or null if generation failed</returns>
    public FileInfo GenerateReport(CommentStore commentStore, ICheckNotifier notifier, IIconProvider iconProvider,
        bool showFile, Func<RdmpIcon, byte[]> iconRenderer = null)
    {
        iconRenderer ??= EmbeddedIconHelper.GetPngBytes;

        _commentStore = commentStore;
        try
        {
            Check(notifier);

            using var document = GetNewDocFile("RDMPDocumentation");
            var t = InsertTable(document, Summaries.Count * 2 + 1, 1);

            //Listing Cell header
            SetTableCell(t, 0, 0, "Tables");

            var keys = Summaries.Keys.ToArray();

            for (var i = 0; i < Summaries.Count; i++)
            {
                //creates the run
                SetTableCell(t, i * 2 + 1, 0, "");

                var icon = iconProvider.GetImage(keys[i]);

                if (icon != null)
                {
                    var para = GetTableCell(t, i * 2 + 1, 0).Elements<Paragraph>().First();
                    var run = para.Elements<Run>().FirstOrDefault() ?? para.AppendChild(new Run());
                    GetPicture(document, run, iconRenderer(icon));
                }

                SetTableCell(t, i * 2 + 1, 0, $" {keys[i].Name}");

                SetTableCell(t, i * 2 + 2, 0, Summaries[keys[i]]);
            }

            //save and close the file before showing it to the user
            document.Dispose();

            if (showFile)
                ShowFile(document);

            return document.FileInfo;
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("Report generation failed", CheckResult.Fail, e));
        }

        return null;
    }

    private void Check(ICheckNotifier notifier)
    {
        // Skip plugin types being integrated - they have separate documentation
        var skipTypes = new HashSet<string>
        {
            "SuccessfullyExtractedResults",
            "AutomateExtractionSchedule",
            "QueuedExtraction",
            "AutomateExtraction"
        };

        foreach (var t in MEF.GetAllTypes().Where(t => typeof(DatabaseEntity).IsAssignableFrom(t)))
            if (typeof(IMapsDirectlyToDatabaseTable).IsAssignableFrom(t))
            {
                if (t.IsInterface || t.IsAbstract || t.Name.StartsWith("Spontaneous") ||
                    t.Assembly.FullName?.StartsWith("DynamicProxyGenAssembly2") == true ||
                    skipTypes.Contains(t.Name))
                    continue;
                try
                {
                    //spontaneous objects don't exist in the database.
                    if (typeof(SpontaneousObject).IsAssignableFrom(t))
                        continue;
                }
                catch (Exception)
                {
                    continue;
                }

                notifier.OnCheckPerformed(new CheckEventArgs($"Found type {t}", CheckResult.Success));

                var docs = _commentStore.GetTypeDocumentationIfExists(t, true, true);

                if (docs == null)
                    notifier.OnCheckPerformed(
                        new CheckEventArgs($"Failed to get definition for class {t.FullName} defined in {t.Assembly}",
                            CheckResult.Fail));
                else
                    Summaries.Add(t, docs);
            }
    }
}