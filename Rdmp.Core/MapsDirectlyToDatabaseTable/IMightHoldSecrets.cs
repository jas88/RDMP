// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Reflection;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;

namespace Rdmp.Core.MapsDirectlyToDatabaseTable;

/// <summary>
/// An object which can hold a secret, such as an encrypted value, in a property whose name does not say so.
/// <see cref="TableRepository"/> does not log the values of such a property when it saves a change to it.
/// </summary>
internal interface IMightHoldSecrets
{
    /// <summary>
    /// True if <paramref name="property"/> holds a secret before or after <paramref name="changes"/> are saved
    /// </summary>
    bool IsSecret(PropertyInfo property, RevertableObjectReport changes);
}
