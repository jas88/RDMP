// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.IO;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Reports;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tests.Common;

namespace Rdmp.Core.Tests.Reports;

internal class MetadataReportTests : UnitTests
{
    [Test]
    public void Test_MetadataReport_Basic()
    {
        var cata = WhenIHaveA<Catalogue>();
        var reporter = new MetadataReport(Repository, new MetadataReportArgs(new[] { cata }));
        cata.Description = "The Quick Brown Fox Was Quicker Than The slow tortoise";

        //setup delegate for returning images
        var bmp = new Image<Rgba32>(200, 200);

        // paint a 50x50 black square at (10,10) so the image is non-uniform
        var black = Color.Black.ToPixel<Rgba32>();
        bmp.ProcessPixelRows(accessor =>
        {
            for (var y = 10; y < 60; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 10; x < 60; x++)
                    row[x] = black;
            }
        });

        reporter.RequestCatalogueImages += s =>
        {
            return new BitmapWithDescription[] { new(bmp, "MyPicture", "Something interesting about it") };
        };

        var file = reporter.GenerateWordFile(ThrowImmediatelyDataLoadEventListener.Quiet, false);

        Assert.Multiple(() =>
        {
            Assert.That(file, Is.Not.Null);
            Assert.That(File.Exists(file.FullName));

            //refreshes the file stream status
            Assert.That(new FileInfo(file.FullName).Length, Is.GreaterThan(0));
        });

        var contents = DocXContents.Read(file.FullName);

        Assert.Multiple(() =>
        {
            contents.AssertValid();
            Assert.That(contents.Paragraphs, Does.Contain(cata.Name));
            Assert.That(contents.Paragraphs, Does.Contain(cata.Description));
            Assert.That(contents.Paragraphs, Does.Contain("Aggregates"));
            Assert.That(contents.Paragraphs, Does.Contain("MyPicture"));
            Assert.That(contents.Paragraphs, Does.Contain("Something interesting about it"));
            Assert.That(contents.FontSizes[cata.Name], Is.EqualTo(16));
            Assert.That(contents.FontSizes[cata.Description], Is.EqualTo(7));
            Assert.That(contents.ImageCount, Is.EqualTo(1));
            Assert.That(contents.DrawingCount, Is.EqualTo(1));
            Assert.That(contents.FooterTexts.Single(), Does.StartWith("Created on "));
            Assert.That(contents.PageMargin.Left?.Value, Is.EqualTo(292u));
        });
    }

    [Test]
    public void Test_OrphanExtractionInformation()
    {
        var ei = WhenIHaveA<ExtractionInformation>();

        //make it an orphan
        ei.CatalogueItem.ColumnInfo.DeleteInDatabase();
        ei.CatalogueItem.ColumnInfo_ID = null;
        ei.CatalogueItem.SaveToDatabase();
        ei.CatalogueItem.ClearAllInjections();
        ei.ClearAllInjections();

        var reporter = new MetadataReport(Repository,
            new MetadataReportArgs(new[] { ei.CatalogueItem.Catalogue })
        );
        var file = reporter.GenerateWordFile(ThrowImmediatelyDataLoadEventListener.Quiet, false);

        Assert.Multiple(() =>
        {
            Assert.That(file, Is.Not.Null);
            Assert.That(File.Exists(file.FullName));
        });

        var contents = DocXContents.Read(file.FullName);
        var columnsTable = contents.Tables.Single(t => t[0][0] == "Column");

        Assert.Multiple(() =>
        {
            contents.AssertValid();
            Assert.That(contents.Paragraphs, Does.Contain("Extractable Columns"));
            Assert.That(columnsTable[0], Is.EqualTo(new[] { "Column", "Datatype", "Description", "Category" }));
            Assert.That(columnsTable, Has.Length.EqualTo(2));
            Assert.That(columnsTable[1][0], Is.EqualTo(ei.GetRuntimeName()));
            Assert.That(columnsTable[1][1], Is.EqualTo("ORPHAN"));
        });
    }
}