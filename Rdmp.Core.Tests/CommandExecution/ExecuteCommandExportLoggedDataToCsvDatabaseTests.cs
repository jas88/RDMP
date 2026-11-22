// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.IO;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.Logging;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Tests.Common;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests for <see cref="ExecuteCommandExportLoggedDataToCsv"/> that read the test logging database
/// </summary>
public class ExecuteCommandExportLoggedDataToCsvDatabaseTests : DatabaseTests
{
    [Test]
    public void Execute_WhenNoOutputFileIsChosen_DoesNothing()
    {
        // SelectFile returns null when the user cancels choosing the output file
        var activator = GetActivatorChoosingTheLoggingServer();
        activator.SelectFile(Arg.Any<string>()).Returns((FileInfo)null);
        var cmd = new ExecuteCommandExportLoggedDataToCsv(activator, new LogViewerFilter(LoggingTables.DataLoadTask));

        Assert.DoesNotThrow(cmd.Execute);
        activator.Received(1).SelectFile(Arg.Any<string>());
    }

    [Test]
    public void Execute_WritesTheLoggedEventsToTheChosenFile()
    {
        var dir = Directory.CreateDirectory(Path.Combine(TestContext.CurrentContext.WorkDirectory,
            nameof(Execute_WritesTheLoggedEventsToTheChosenFile)));
        var output = new FileInfo(Path.Combine(dir.FullName, "logs.csv"));
        if (output.Exists)
            output.Delete();

        var activator = GetActivatorChoosingTheLoggingServer();
        activator.SelectFile(Arg.Any<string>()).Returns(output);

        new ExecuteCommandExportLoggedDataToCsv(activator, new LogViewerFilter(LoggingTables.DataLoadTask)).Execute();

        output.Refresh();
        Assert.That(output.Exists, $"Expected the logged events to be written to {output.FullName}");
        Assert.That(File.ReadLines(output.FullName).First(),
            Is.EqualTo("dataLoadRunID,eventType,description,source,time,ID"));
    }

    private IBasicActivateItems GetActivatorChoosingTheLoggingServer()
    {
        var loggingServer = (ExternalDatabaseServer)CatalogueRepository.GetDefaultFor(
            PermissableDefaults.LiveLoggingServer_ID);

        var activator = Substitute.For<IBasicActivateItems>();
        activator.RepositoryLocator.Returns(RepositoryLocator);
        activator.SelectOne(Arg.Any<DialogArgs>(), Arg.Any<IMapsDirectlyToDatabaseTable[]>()).Returns(loggingServer);
        return activator;
    }
}
