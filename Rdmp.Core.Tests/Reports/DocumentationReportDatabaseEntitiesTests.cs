// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.Icons;
using Rdmp.Core.Icons.IconProvision;
using Rdmp.Core.Reports;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Comments;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using Tests.Common;

namespace Rdmp.Core.Tests.Reports;

internal class DocumentationReportDatabaseEntitiesTests : UnitTests
{
        [Test]
        public void Test_DocumentationReportDatabaseEntities_Normal()
        {
                var store = new CommentStore();
                store.ReadComments(TestContext.CurrentContext.TestDirectory);

                var reporter = new DocumentationReportDatabaseEntities();

                var img = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link);

                var iconProvider = Substitute.For<IIconProvider>();
                iconProvider.GetImage(Arg.Any<object>(), Arg.Any<OverlayKind>()).Returns(img);

                // without a renderer the base PNG of the icon is used
                var file = reporter.GenerateReport(store, ThrowImmediatelyCheckNotifier.Quiet, iconProvider, false);

                Assert.That(file, Is.Not.Null);
                var contents = DocXContents.Read(file.FullName);
                var table = contents.Tables.Single();

                Assert.Multiple(() =>
                {
                        contents.AssertValid();
                        Assert.That(table[0], Is.EqualTo(new[] { "Tables" }));
                        Assert.That(table, Has.Length.GreaterThan(1));
                        Assert.That(table[1][0], Does.StartWith(" "), "Type name row should follow the icon");
                        // one icon per documented type
                        Assert.That(contents.DrawingCount, Is.EqualTo((table.Length - 1) / 2));
                        Assert.That(contents.ImageCount, Is.EqualTo(contents.DrawingCount));
                });
                file.Delete();
        }

        [Test]
        public void Test_DocumentationReportDatabaseEntities_UsesIconRenderer()
        {
                var store = new CommentStore();
                store.ReadComments(TestContext.CurrentContext.TestDirectory);

                var reporter = new DocumentationReportDatabaseEntities();

                var img = CatalogueIcons.Catalogue.WithOverlay(OverlayKind.Link);

                var iconProvider = Substitute.For<IIconProvider>();
                iconProvider.GetImage(Arg.Any<object>(), Arg.Any<OverlayKind>()).Returns(img);

                var png = EmbeddedIconHelper.GetPngBytes(CatalogueIcons.Project);

                var rendered = 0;
                var file = reporter.GenerateReport(store, ThrowImmediatelyCheckNotifier.Quiet, iconProvider, false, icon =>
                {
                        Assert.That(icon, Is.EqualTo(img));
                        rendered++;
                        return png;
                });

                Assert.That(rendered, Is.GreaterThan(0));

                // what the renderer returned is what was put in the document, once per icon
                Assert.That(file, Is.Not.Null);
                var images = DocXContents.Read(file.FullName).Images;
                Assert.Multiple(() =>
                {
                        Assert.That(images, Has.Count.EqualTo(rendered));
                        Assert.That(images, Is.All.EqualTo(png));
                });
                file.Delete();
        }
}
