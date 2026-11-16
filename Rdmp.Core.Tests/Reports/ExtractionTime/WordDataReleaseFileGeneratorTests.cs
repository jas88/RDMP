// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.IO;
using NUnit.Framework;
using Rdmp.Core.Reports.ExtractionTime;
using Tests.Common.Scenarios;

namespace Rdmp.Core.Tests.Reports.ExtractionTime;

internal class WordDataReleaseFileGeneratorTests : TestsRequiringAnExtractionConfiguration
{
    [Test]
    public void Test_WordDataReleaseFileGenerator_Normal()
    {
        var report = new WordDataReleaseFileGenerator(_configuration, _configuration.DataExportRepository);

        var filename = Path.Combine(TestContext.CurrentContext.WorkDirectory, "release.doc");
        report.GenerateWordFile(filename);

        Assert.That(File.Exists(filename));

        var contents = DocXContents.Read(filename);

        Assert.Multiple(() =>
        {
            contents.AssertValid();
            Assert.That(contents.Paragraphs, Does.Contain($"Project:{_configuration.Project.Name}"));
            Assert.That(contents.Paragraphs, Does.Contain(_configuration.Name));
            Assert.That(contents.PageSize.Orient?.Value,
                Is.EqualTo(DocumentFormat.OpenXml.Wordprocessing.PageOrientationValues.Landscape));
            Assert.That(contents.Tables, Has.Count.EqualTo(3));
            Assert.That(contents.Tables[1][0],
                Is.EqualTo(new[] { "Version", "Description", "Date Extracted", "Unique Individuals" }));
            Assert.That(contents.Tables[2][0],
                Is.EqualTo(new[] { "Data Requirement", "Notes", "Filename", "Records", "Unique Individuals", "Datasets" }));
        });
    }
}