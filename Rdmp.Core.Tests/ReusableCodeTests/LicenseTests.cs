// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Licenses;
using Tests.Common;

namespace Rdmp.Core.Tests.ReusableCodeTests;

/// <summary>
/// Tests that the licenses embedded in Rdmp.Core are the files in the repository.  The Windows client and the rdmp command line
/// both read them from Rdmp.Core, so accepting them in either records the same hash.
/// </summary>
public class LicenseTests
{
    [TestCase(License.RdmpLicense)]
    [TestCase(License.ThirdPartyLicenses)]
    public void EmbeddedLicense_IsTheFileInTheRepository(string fileName)
    {
        var path = Path.Join(RepositoryLayout.FindRoot().FullName, fileName);
        var license = new License(fileName);

        Assert.Multiple(() =>
        {
            Assert.That(license.GetLicenseText(), Is.EqualTo(File.ReadAllText(path)));
            Assert.That(license.GetHashOfLicense(),
                Is.EqualTo(BitConverter.ToString(SHA512.HashData(File.ReadAllBytes(path)))));
        });
    }

    [Test]
    public void GetHashOfLicense_IsDashSeparatedHexadecimal()
    {
        // The format which the Windows client has always recorded, so that existing acceptances still match
        Assert.That(new License(License.ThirdPartyLicenses).GetHashOfLicense(),
            Does.Match("^([0-9A-F]{2}-){63}[0-9A-F]{2}$"));
    }

    [Test]
    public void UnknownLicense_Throws()
    {
        Assert.Throws<Exception>(() => new License("NOSUCHLICENSE").GetHashOfLicense());
    }
}
