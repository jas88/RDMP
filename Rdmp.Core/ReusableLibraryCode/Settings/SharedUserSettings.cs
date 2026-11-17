// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;

namespace Rdmp.Core.ReusableLibraryCode.Settings;

/// <summary>
/// Settings stored as files in a folder of the current user which every RDMP program shares.  The isolated storage used by
/// <see cref="RDMPApplicationSettings"/> is keyed on where the program is, so the Windows client, the rdmp command line and each
/// unzipped release keep separate copies of those settings.
/// </summary>
internal sealed class SharedUserSettings
{
    /// <summary>
    /// The folder the settings are stored in, or null if the current user has none (and so nothing can be stored)
    /// </summary>
    public string Folder { get; }

    /// <summary>
    /// Settings stored in <paramref name="folder"/>, which is created when the first setting is stored
    /// </summary>
    /// <param name="folder">The folder to store settings in, or null if there is none</param>
    public SharedUserSettings(string folder)
    {
        Folder = folder;
    }

    /// <summary>
    /// Settings stored in the RDMP folder of the current user's local application data, where isolated storage also keeps its files
    /// </summary>
    /// <returns>The settings of the current user</returns>
    public static SharedUserSettings ForCurrentUser()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        return new SharedUserSettings(string.IsNullOrEmpty(localApplicationData)
            ? null
            : Path.Combine(localApplicationData, "RDMP"));
    }

    /// <summary>
    /// Returns the value stored for <paramref name="key"/>
    /// </summary>
    /// <param name="key">The name of the setting, which is also its file name</param>
    /// <returns>The value, or null if there is none or it cannot be read</returns>
    public string Get(string key)
    {
        if (Folder == null)
            return null;

        try
        {
            var path = Path.Combine(Folder, key);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Stores <paramref name="value"/> for <paramref name="key"/>, or removes the setting if <paramref name="value"/> is null
    /// </summary>
    /// <param name="key">The name of the setting, which is also its file name</param>
    /// <param name="value">The value to store, or null to remove the setting</param>
    /// <exception cref="IOException">If there is no folder, or the setting cannot be written</exception>
    /// <exception cref="UnauthorizedAccessException">If the user may not write to the folder</exception>
    public void Set(string key, string value)
    {
        if (Folder == null)
            throw new IOException("The current user has no local application data folder to store settings in");

        var path = Path.Combine(Folder, key);

        if (value == null)
        {
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        Directory.CreateDirectory(Folder);
        File.WriteAllText(path, value);
    }
}
