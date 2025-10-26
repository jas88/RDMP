// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using DicomTypeTranslation.TableCreation;
using FAnsi;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Dicom.CommandExecution;
using Tests.Common;

namespace Rdmp.Dicom.Tests.Integration;

class LiveVsTemplateComparerTests : DatabaseTests
{

    [TestCase(DatabaseType.MySql)]
    [TestCase(DatabaseType.MicrosoftSQLServer)]
    public void TestImageTemplates(DatabaseType type)
    {
        var db = GetCleanedServer(type);

        // Create a nice template with lots of columns
        var template = new ImageTableTemplate
        {
            TableName = "Fish",
            Columns = new[]
            {
                new ImageColumnTemplate
                {
                    IsPrimaryKey = true, AllowNulls = true, ColumnName = "RelativeFileArchiveURI"
                },
                new ImageColumnTemplate
                {
                    IsPrimaryKey = false, AllowNulls = true, ColumnName = "SeriesInstanceUID"
                },
                new ImageColumnTemplate {IsPrimaryKey = false, AllowNulls = true, ColumnName = "StudyDate"},
                new ImageColumnTemplate
                {
                    IsPrimaryKey = false, AllowNulls = true, ColumnName = "StudyInstanceUID"
                },
                new ImageColumnTemplate
                {
                    IsPrimaryKey = false, AllowNulls = true, ColumnName = "StudyDescription"
                },
                new ImageColumnTemplate {IsPrimaryKey = false, AllowNulls = true, ColumnName = "EchoTime"},
                new ImageColumnTemplate
                {
                    IsPrimaryKey = false, AllowNulls = true, ColumnName = "RepetitionTime"
                },
                new ImageColumnTemplate
                {
                    IsPrimaryKey = false, AllowNulls = true, ColumnName = "PatientAge"
                }
            }
        };

        // use it to create a table
        var tbl = db.ExpectTable(template.TableName);
        var cmd = new ExecuteCommandCreateNewImagingDataset(RepositoryLocator, tbl, template);
        Assert.That(cmd.IsImpossible, Is.False);
        cmd.Execute();

        Assert.That(tbl.Exists());

        // import RDMP reference to the table
        var importer = new TableInfoImporter(CatalogueRepository, tbl);
        importer.DoImport(out var ti, out _);

        // compare the live with the template
        var comparer = new LiveVsTemplateComparer(ti, new() { DatabaseType = type, Tables = new() { template } });

        // should be no differences
        Assert.That(comparer.LiveSql, Is.EqualTo(comparer.TemplateSql));

        // make a difference
        tbl.DropColumn(tbl.DiscoverColumn("EchoTime"));

        //now comparer should see a difference
        comparer = new(ti, new() { DatabaseType = type, Tables = new() { template } });
        Assert.That(comparer.LiveSql, Is.Not.EqualTo(comparer.TemplateSql));

        tbl.Drop();
    }
}