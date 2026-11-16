// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.Reports;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Comments;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tests.Common;
using Color = SixLabors.ImageSharp.Color;

namespace Rdmp.Core.Tests.Reports;

internal class DocumentationReportDatabaseEntitiesTests : UnitTests
{
        [Test]
        public void Test_DocumentationReportDatabaseEntities_Normal()
        {
                var store = new CommentStore();
                store.ReadComments(TestContext.CurrentContext.TestDirectory);

                var reporter = new DocumentationReportDatabaseEntities();

                Image img = new Image<Rgba32>(19, 19, Color.DarkMagenta.ToPixel<Rgba32>());

                var iconProvider = Substitute.For<IIconProvider>();
                iconProvider.GetImage(Arg.Any<object>(), Arg.Any<OverlayKind>()).Returns(img);

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
}