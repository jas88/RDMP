// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Reflection;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.DataAccess;

namespace Rdmp.Core.Curation.Data.DataLoad;

// The part of Argument that tells TableRepository which arguments hold encrypted values, so that it does not log them
// when it saves them
public abstract partial class Argument : IMightHoldSecrets
{
    /// <summary>
    /// The characters around the names of the types in a <see cref="Type"/> such as
    /// "System.Collections.Generic.Dictionary`2[System.String,Rdmp.Core.Curation.Data.EncryptedString]"
    /// </summary>
    private static readonly char[] TypeNameSeparators = { '[', ']', ',' };

    /// <summary>
    /// True for <see cref="Value"/> if <see cref="Type"/>, before or after <paramref name="changes"/> are saved, is one whose
    /// values are encrypted (see <see cref="IsEncryptedType"/>).  These are secrets because, unless an encryption key has
    /// been set up, the key is the default one in RDMP's source code.
    /// </summary>
    bool IMightHoldSecrets.IsSecret(PropertyInfo property, RevertableObjectReport changes) =>
        property.Name == nameof(Value) &&
        (IsEncryptedType(Type) || changes.Differences.Any(static d =>
            d.Property.Name == nameof(Type) && IsEncryptedType(d.DatabaseValue as string)));

    /// <summary>
    /// True if an argument whose <see cref="Type"/> is <paramref name="type"/> keeps an encrypted value in
    /// <see cref="Value"/>: an <see cref="IEncryptedString"/>, a class with a custom UI that saves an encrypted password
    /// with its state (such as <see cref="Rdmp.Core.DataLoad.Modules.DataProvider.WebServiceConfiguration"/>), or a
    /// dictionary of either.  A type that cannot be found is not one, because RDMP could not have encrypted a value for it.
    /// </summary>
    internal static bool IsEncryptedType(string type) =>
        !string.IsNullOrWhiteSpace(type) &&
        type.Split(TypeNameSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static name => MEF.GetType(name))
            .Any(static t => t != null && IsEncrypted(t));

    private static bool IsEncrypted(Type type) =>
        typeof(IEncryptedString).IsAssignableFrom(type) ||
        (typeof(ICustomUIDrivenClass).IsAssignableFrom(type) && typeof(IEncryptedPasswordHost).IsAssignableFrom(type));
}
