// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Tests.Common;

namespace Rdmp.Core.Tests.MapsDirectlyToDatabaseTable;

/// <summary>
/// Tests the audit log that <see cref="TableRepository"/> writes of the changes it saves
/// </summary>
public class TableRepositorySaveAuditTests : DatabaseTests
{
    [Test]
    public void SaveToDatabase_DoesNotLogPasswords()
    {
        if (CatalogueRepository is not TableRepository)
            Assert.Ignore("Only database-backed repositories (TableRepository) log the changes they save");

        var credentials = new DataAccessCredentials(CatalogueRepository, nameof(SaveToDatabase_DoesNotLogPasswords));
        var secrets = new List<string> { "FirstPassword", "SecondPassword" };
        string[] log;

        try
        {
            log = CaptureLog(() =>
            {
                credentials.Username = "bob";
                credentials.Password = secrets[0];
                secrets.Add(credentials.Password);
                credentials.SaveToDatabase();

                credentials.Password = secrets[1];
                secrets.Add(credentials.Password);
                credentials.SaveToDatabase();
            });
        }
        finally
        {
            credentials.DeleteInDatabase();
        }

        var saves = log.Where(l => l.StartsWith($"Save,{nameof(DataAccessCredentials)},{credentials.ID},")).ToArray();
        var hidden = $",System.String Password,{SaveAuditMessage.HiddenValue},{SaveAuditMessage.HiddenValue}";

        Assert.Multiple(() =>
        {
            Assert.That(saves.Count(s => s.EndsWith(",System.String Username,,bob")), Is.EqualTo(1));
            Assert.That(saves.Count(s => s.EndsWith(hidden)), Is.EqualTo(2));

            // the passwords, and the encrypted values that the credentials store
            foreach (var secret in secrets)
                Assert.That(log.Where(l => l.Contains(secret)), Is.Empty);
        });
    }

    /// <summary>
    /// Returns the messages logged at Debug level or above while <paramref name="action"/> runs
    /// </summary>
    private static string[] CaptureLog(Action action)
    {
        using var log = new MemoryTarget { Layout = "${message}" };
        var configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, log);

        var previous = LogManager.Configuration;
        LogManager.Configuration = configuration;
        try
        {
            action();
        }
        finally
        {
            LogManager.Configuration = previous;
        }

        return log.Logs.ToArray();
    }
}
