// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.DataRelease.Potential;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace Rdmp.Core.Tests.DataExport.DataRelease;

internal class GlobalReleasePotentialTests
{
    private sealed class TestGlobalReleasePotential(ISupplementalExtractionResults globalResult,
        IMapsDirectlyToDatabaseTable globalToCheck) : GlobalReleasePotential(null, globalResult, globalToCheck)
    {
        protected override void CheckDestination(ICheckNotifier notifier, ISupplementalExtractionResults globalResult)
        {
        }
    }

    [Test]
    public void Check_GlobalWithSqlThatIsNotASupportingSQLTable_FailsTheCheck()
    {
        var result = Substitute.For<ISupplementalExtractionResults>();
        result.SQLExecuted.Returns("SELECT 1");
        result.ExtractedName.Returns("MyGlobal");
        var potential = new TestGlobalReleasePotential(result, Substitute.For<IMapsDirectlyToDatabaseTable>());
        var notifier = new ToMemoryCheckNotifier();

        potential.Check(notifier);

        Assert.Multiple(() =>
        {
            Assert.That(potential.Releasability, Is.EqualTo(Releaseability.ExtractionSQLDesynchronisation));
            Assert.That(notifier.Messages, Has.Some.Matches<CheckEventArgs>(static m =>
                m.Result == CheckResult.Fail && m.Message.Contains("is not a SupportingSQLTable")));
        });
    }

    [TestCase("SELECT 1", Releaseability.Releaseable)]
    [TestCase("SELECT 2", Releaseability.ExtractionSQLDesynchronisation)]
    public void Check_SupportingSQLTable_ComparesItsSqlWithTheSqlThatWasRun(string currentSql,
        Releaseability expected)
    {
        var result = Substitute.For<ISupplementalExtractionResults>();
        result.SQLExecuted.Returns("SELECT 1");
        result.ExtractedName.Returns("MyGlobal");
        var potential = new TestGlobalReleasePotential(result, new SupportingSQLTable { SQL = currentSql });
        var notifier = new ToMemoryCheckNotifier();

        potential.Check(notifier);

        Assert.Multiple(() =>
        {
            Assert.That(potential.Releasability, Is.EqualTo(expected));
            Assert.That(notifier.Messages.Any(static m => m.Message.Contains("is different from the current one")),
                Is.EqualTo(expected == Releaseability.ExtractionSQLDesynchronisation));
        });
    }
}
