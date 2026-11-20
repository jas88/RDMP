// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Remoting;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;

namespace Rdmp.Core.Tests.MapsDirectlyToDatabaseTable;

/// <summary>
/// Tests the message that <see cref="TableRepository"/> logs for each property it saves a change to
/// </summary>
public class SaveAuditMessageTests
{
    [Test]
    public void For_IncludesTheOldAndNewValues()
    {
        var saved = new DataAccessCredentials { ID = 3 };
        var change = new RevertablePropertyDifference(
            typeof(DataAccessCredentials).GetProperty(nameof(DataAccessCredentials.Username)), "new", "old");

        Assert.That(SaveAuditMessage.For(saved, change),
            Is.EqualTo("Save,DataAccessCredentials,3,System.String Username,old,new"));
    }

    [TestCase(typeof(DataAccessCredentials))]
    [TestCase(typeof(ExternalDatabaseServer))]
    [TestCase(typeof(ExternalCohortTable))]
    [TestCase(typeof(RemoteRDMP))]
    public void For_HidesTheValuesOfPasswords(Type type)
    {
        var saved = (DatabaseEntity)Activator.CreateInstance(type);
        saved.ID = 5;
        var change = new RevertablePropertyDifference(type.GetProperty("Password"), "new secret", "old secret");

        Assert.That(SaveAuditMessage.For(saved, change),
            Is.EqualTo($"Save,{type.Name},5,System.String Password,{SaveAuditMessage.HiddenValue},{SaveAuditMessage.HiddenValue}"));
    }

    [TestCase(nameof(DataAccessCredentials.Password), true)]
    [TestCase(nameof(DataAccessCredentials.Username), false)]
    [TestCase(nameof(DataAccessCredentials.Name), false)]
    public void IsPassword(string property, bool expected)
    {
        Assert.That(SaveAuditMessage.IsPassword(typeof(DataAccessCredentials).GetProperty(property)),
            Is.EqualTo(expected));
    }
}
