// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.IO;
using System.Linq;
using NPOI.XWPF.UserModel;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Icons;
using Rdmp.Core.Icons.IconProvision;
using Rdmp.Core.Reports;
using Rdmp.Core.ReusableLibraryCode.Progress;
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

        //setup delegate for returning images (any non-uniform PNG will do, so use one of the embedded icons)
        var png = EmbeddedIconHelper.GetPngBytes(CatalogueIcons.Catalogue);

        reporter.RequestCatalogueImages += s =>
        {
            return new BitmapWithDescription[] { new(png, "MyPicture", "Something interesting about it") };
        };

        var file = reporter.GenerateWordFile(ThrowImmediatelyDataLoadEventListener.Quiet, false);

        Assert.Multiple(() =>
        {
            Assert.That(file, Is.Not.Null);
            Assert.That(File.Exists(file.FullName));

            //refreshes the file stream status
            Assert.That(new FileInfo(file.FullName).Length, Is.GreaterThan(0));
        });

        // the PNG should have been embedded unchanged
        using var stream = File.OpenRead(file.FullName);
        using var doc = new XWPFDocument(stream);
        Assert.That(doc.AllPictures.Select(static p => p.Data), Has.One.EqualTo(png));
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
    }
}