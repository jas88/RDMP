// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using DicomTypeTranslation.TableCreation;
using FAnsi;
using NUnit.Framework;
using Rdmp.Dicom.CommandExecution;
using System.Linq;
using Tests.Common;

namespace Rdmp.Dicom.Tests.Integration;

public class ImagingTableCreationTests : DatabaseTests
{

    [TestCase(DatabaseType.MySql)]
    [TestCase(DatabaseType.MicrosoftSQLServer)]
    public void TestImageTemplates(DatabaseType type)
    {
        var db = GetCleanedServer(type);

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
                }
            }
        };
        var tbl = db.ExpectTable(template.TableName);
        var cmd = new ExecuteCommandCreateNewImagingDataset(RepositoryLocator, tbl, template);
        Assert.That(cmd.IsImpossible, Is.False);
        cmd.Execute();

        Assert.That(tbl.Exists());

        var cols = tbl.DiscoverColumns();
        Assert.That(cols, Has.Length.EqualTo(2));

        var rfa = cols.Single(c => c.GetRuntimeName().Equals("RelativeFileArchiveURI"));

        Assert.Multiple(() =>
        {
            Assert.That(rfa.IsPrimaryKey);
            Assert.That(rfa.AllowNulls, Is.False); //because PK!
        });


        var sid = cols.Single(c => c.GetRuntimeName().Equals("SeriesInstanceUID"));

        Assert.Multiple(() =>
        {
            Assert.That(sid.IsPrimaryKey, Is.False);
            Assert.That(sid.AllowNulls);
        });



    }
}