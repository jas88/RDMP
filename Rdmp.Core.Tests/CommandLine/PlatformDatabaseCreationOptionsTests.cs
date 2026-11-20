// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.CommandLine.DatabaseCreation;

namespace Rdmp.Core.Tests.CommandLine;

/// <summary>
/// Tests whether the connection strings that <see cref="PlatformDatabaseCreationOptions.GetBuilder"/> builds, for the rdmp
/// install verb and for creating platform databases in the Windows client, trust the certificate of the server
/// </summary>
public class PlatformDatabaseCreationOptionsTests
{
    private const string Server = "localhost";

    [Test]
    public void GetBuilder_TrustsServerCertificate_WhenNothingSaysOtherwise()
    {
        var builder = new PlatformDatabaseCreationOptions { ServerName = Server }.GetBuilder("Catalogue");

        Assert.That(builder.TrustServerCertificate, Is.True);
    }

    [TestCase("TrustServerCertificate=False", false)]
    [TestCase("trust server certificate=false", false)]
    [TestCase("Encrypt=Mandatory;TrustServerCertificate=False", false)]
    [TestCase("TrustServerCertificate=True", true)]
    [TestCase("Encrypt=Mandatory", true)]
    public void GetBuilder_UsesTrustServerCertificateOfOtherKeywords(string otherKeywords, bool expected)
    {
        var builder = new PlatformDatabaseCreationOptions { ServerName = Server, OtherKeywords = otherKeywords }
            .GetBuilder("Catalogue");

        Assert.That(builder.TrustServerCertificate, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("TrustServerCertificate=True")]
    public void GetBuilder_ValidateCertificate_NeverTrustsServerCertificate(string otherKeywords)
    {
        var builder = new PlatformDatabaseCreationOptions
        {
            ServerName = Server,
            OtherKeywords = otherKeywords,
            ValidateCertificate = true
        }.GetBuilder("Catalogue");

        Assert.That(builder.TrustServerCertificate, Is.False);
    }
}
