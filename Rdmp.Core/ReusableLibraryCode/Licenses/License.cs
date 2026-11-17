// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Security.Cryptography;

namespace Rdmp.Core.ReusableLibraryCode.Licenses;

/// <summary>
/// Facilitates reading from the embedded license files for RDMP (LICENSE) and third party libraries (LIBRARYLICENSES).  Also generates a SHA512 hash for
/// tracking when a user has agreed to a license that has been subsequently changed in a software update (e.g. if we use a new library).  The Windows client
/// and the rdmp command line both ask the user to accept the licenses and record the hash in
/// <see cref="Rdmp.Core.ReusableLibraryCode.Settings.UserSettings.LicenseAccepted"/>.
/// </summary>
public class License
{
    /// <summary>
    /// The license of RDMP itself
    /// </summary>
    public const string RdmpLicense = "LICENSE";

    /// <summary>
    /// The licenses of the third party libraries, native code and data in RDMP, whose hash records that the user accepted the licenses
    /// </summary>
    public const string ThirdPartyLicenses = "LIBRARYLICENSES";

    private readonly string _resourceFilename;
    private const string LicenseResourcePath = "Rdmp.Core.Licenses.";

    /// <summary>
    /// Reads one of the license files embedded in this assembly
    /// </summary>
    /// <param name="licenseFilename">The license file, <see cref="RdmpLicense"/> or <see cref="ThirdPartyLicenses"/></param>
    public License(string licenseFilename = RdmpLicense)
    {
        _resourceFilename = LicenseResourcePath + licenseFilename;
    }

    /// <summary>
    /// Computes a SHA512 hash of the current License text
    /// </summary>
    /// <returns>The hash as pairs of hexadecimal digits separated by dashes</returns>
    public string GetHashOfLicense()
    {
        using var stream = GetStream();
        return BitConverter.ToString(SHA512.HashData(stream));
    }

    /// <summary>
    /// Returns the current License text
    /// </summary>
    /// <returns>The text of the license file</returns>
    public string GetLicenseText()
    {
        using var stream = GetStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private Stream GetStream()
    {
        var stream = typeof(License).Assembly.GetManifestResourceStream(_resourceFilename);

        return stream ?? throw new Exception($"Could not find EmbeddedResource '{_resourceFilename}'");
    }
}
