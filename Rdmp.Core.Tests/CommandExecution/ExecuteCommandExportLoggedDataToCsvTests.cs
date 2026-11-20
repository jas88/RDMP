// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Databases;
using Rdmp.Core.Logging;
using Rdmp.Core.MapsDirectlyToDatabaseTable;

namespace Rdmp.Core.Tests.CommandExecution;

internal class ExecuteCommandExportLoggedDataToCsvTests : CommandCliTests
{
    [Test]
    public void Execute_WhenNoLoggingServerIsChosen_DoesNothing()
    {
        _ = new ExternalDatabaseServer(Repository.CatalogueRepository, "Logging", new LoggingDatabasePatcher());

        // SelectOne returns null when the user cancels choosing a server
        var activator = GetMockActivator();
        activator.SelectOne(Arg.Any<DialogArgs>(), Arg.Any<IMapsDirectlyToDatabaseTable[]>())
            .Returns((IMapsDirectlyToDatabaseTable)null);
        var cmd = new ExecuteCommandExportLoggedDataToCsv(activator, new LogViewerFilter(LoggingTables.DataLoadTask));

        Assert.DoesNotThrow(cmd.Execute);
        activator.DidNotReceive().SelectFile(Arg.Any<string>());
    }
}
