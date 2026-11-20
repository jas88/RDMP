// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;

namespace Rdmp.Core.MapsDirectlyToDatabaseTable;

/// <summary>
/// Describes the changes that a <see cref="TableRepository"/> saves, for the audit trail that it writes to its log, without
/// the values of passwords and other secrets
/// </summary>
internal static class SaveAuditMessage
{
    /// <summary>
    /// Logged instead of the old and new values of a secret
    /// </summary>
    internal const string HiddenValue = "****";

    /// <summary>
    /// Returns the messages to log for saving <paramref name="changes"/> to <paramref name="saved"/>, one for each property
    /// that changed: its Type, ID, the property and its old (database) and new (local) values e.g.
    /// "Save,Catalogue,3,System.String Name,Biochem,Biochemistry".  The values of secrets (see <see cref="IsSecret"/>) are
    /// <see cref="HiddenValue"/>.
    /// </summary>
    internal static IEnumerable<string> For(IMapsDirectlyToDatabaseTable saved, RevertableObjectReport changes) =>
        changes.Differences.Select(change => For(saved, change, IsSecret(saved, change.Property, changes)));

    private static string For(IMapsDirectlyToDatabaseTable saved, RevertablePropertyDifference change, bool hide)
    {
        var databaseValue = hide ? HiddenValue : change.DatabaseValue;
        var localValue = hide ? HiddenValue : change.LocalValue;

        return $"Save,{saved.GetType().Name},{saved.ID},{change.Property},{databaseValue},{localValue}";
    }

    /// <summary>
    /// True if <paramref name="property"/> of <paramref name="saved"/> holds a secret before or after
    /// <paramref name="changes"/> are saved: a password (see <see cref="IsPassword"/>), or anything else that
    /// <paramref name="saved"/> says is secret (see <see cref="IMightHoldSecrets"/>), such as an encrypted setting
    /// </summary>
    private static bool IsSecret(IMapsDirectlyToDatabaseTable saved, PropertyInfo property,
        RevertableObjectReport changes) =>
        IsPassword(property) ||
        (saved is IMightHoldSecrets mightHoldSecrets && mightHoldSecrets.IsSecret(property, changes));

    /// <summary>
    /// True if <paramref name="property"/> holds a password, such as the Password of credentials and servers.  These are
    /// encrypted, but unless an encryption key has been set up the key is the default one in RDMP's source code, so anyone
    /// who can read the log could decrypt them.
    /// </summary>
    internal static bool IsPassword(PropertyInfo property) =>
        property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase);
}
