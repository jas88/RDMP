// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.Curation.Data.Remoting;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;
using Rdmp.Core.DataLoad.Modules.DataProvider;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;

namespace Rdmp.Core.Tests.MapsDirectlyToDatabaseTable;

/// <summary>
/// Tests the messages that <see cref="TableRepository"/> logs for the changes it saves
/// </summary>
public class SaveAuditMessageTests
{
    private const string Hidden = SaveAuditMessage.HiddenValue;

    [Test]
    public void For_IncludesTheOldAndNewValues()
    {
        var saved = new DataAccessCredentials { ID = 3 };

        Assert.That(SaveAuditMessage.For(saved, Changes(saved, (nameof(DataAccessCredentials.Username), "new", "old"))),
            Is.EqualTo(new[] { "Save,DataAccessCredentials,3,System.String Username,old,new" }));
    }

    [TestCase(typeof(DataAccessCredentials))]
    [TestCase(typeof(ExternalDatabaseServer))]
    [TestCase(typeof(ExternalCohortTable))]
    [TestCase(typeof(RemoteRDMP))]
    public void For_HidesTheValuesOfPasswords(Type type)
    {
        var saved = (DatabaseEntity)Activator.CreateInstance(type);
        saved.ID = 5;

        Assert.That(SaveAuditMessage.For(saved, Changes(saved, ("Password", "new secret", "old secret"))),
            Is.EqualTo(new[] { $"Save,{type.Name},5,System.String Password,{Hidden},{Hidden}" }));
    }

    [Test]
    public void For_HidesTheValuesOfEncryptedArguments(
        [Values(typeof(ProcessTaskArgument), typeof(PipelineComponentArgument), typeof(ExtendedProperty))]
        Type argumentClass,
        [Values(typeof(EncryptedString), typeof(IEncryptedString), typeof(WebServiceConfiguration),
            typeof(Dictionary<string, EncryptedString>))]
        Type settingType)
    {
        var argument = NewArgument(argumentClass, settingType);
        var changes = Changes(argument, (nameof(Argument.Value), "new ciphertext", "old ciphertext"));

        Assert.That(SaveAuditMessage.For(argument, changes),
            Is.EqualTo(new[] { $"Save,{argumentClass.Name},7,System.String Value,{Hidden},{Hidden}" }));
    }

    [TestCase(typeof(string))]
    [TestCase(typeof(int))]
    [TestCase(typeof(DataAccessCredentials))] // the value is the ID of the credentials
    [TestCase(typeof(ExplicitTypingCollection))] // a class with a custom UI that has no password
    public void For_IncludesTheValuesOfOtherArguments(Type settingType)
    {
        var argument = NewArgument(typeof(ProcessTaskArgument), settingType);

        Assert.That(SaveAuditMessage.For(argument, Changes(argument, (nameof(Argument.Value), "new", "old"))),
            Is.EqualTo(new[] { "Save,ProcessTaskArgument,7,System.String Value,old,new" }));
    }

    [Test]
    public void For_HidesTheValuesOfAnArgumentThatWasEncrypted()
    {
        var argument = NewArgument(typeof(ProcessTaskArgument), typeof(string));
        var changes = Changes(argument,
            (nameof(Argument.Type), typeof(string).ToString(), typeof(EncryptedString).ToString()),
            (nameof(Argument.Value), "new", "old ciphertext"));

        Assert.That(SaveAuditMessage.For(argument, changes), Is.EqualTo(new[]
        {
            "Save,ProcessTaskArgument,7,System.String Type,Rdmp.Core.Curation.Data.EncryptedString,System.String",
            $"Save,ProcessTaskArgument,7,System.String Value,{Hidden},{Hidden}"
        }));
    }

    [TestCase(nameof(DataAccessCredentials.Password), true)]
    [TestCase(nameof(DataAccessCredentials.Username), false)]
    [TestCase(nameof(DataAccessCredentials.Name), false)]
    public void IsPassword(string property, bool expected)
    {
        Assert.That(SaveAuditMessage.IsPassword(typeof(DataAccessCredentials).GetProperty(property)),
            Is.EqualTo(expected));
    }

    /// <summary>
    /// Returns a new argument of <paramref name="argumentClass"/>, with ID 7, for a setting of <paramref name="settingType"/>
    /// </summary>
    private static Argument NewArgument(Type argumentClass, Type settingType)
    {
        var argument = (Argument)Activator.CreateInstance(argumentClass);
        argument.ID = 7;
        argument.SetType(settingType);
        return argument;
    }

    /// <summary>
    /// Returns a report of <paramref name="changes"/> to <paramref name="saved"/>: the name of each property that changed,
    /// with its new (local) and old (database) values
    /// </summary>
    private static RevertableObjectReport Changes(object saved,
        params (string Property, object LocalValue, object DatabaseValue)[] changes) =>
        new(changes.Select(c => new RevertablePropertyDifference(
            saved.GetType().GetProperty(c.Property), c.LocalValue, c.DatabaseValue)).ToList())
        {
            Evaluation = ChangeDescription.DatabaseCopyDifferent
        };
}
