// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Rdmp.Core.Repositories;

/// <summary>
/// The Types which one assembly registered with the <see cref="CompiledTypeRegistry"/>.  The list is only built (which
/// loads every Type in it) when <see cref="GetTypes"/> is first called.
/// </summary>
internal sealed class CompiledTypeRegistration
{
    private readonly Func<Type[]> _getTypes;
    private Type[] _types;

    /// <summary>
    /// The assembly whose Types are registered
    /// </summary>
    public Assembly Assembly { get; }

    public CompiledTypeRegistration(Assembly assembly, Func<Type[]> getTypes)
    {
        Assembly = assembly;
        _getTypes = getTypes;
    }

    /// <summary>
    /// Returns the Types declared in <see cref="Assembly"/>, in the order they appear in its metadata (which is the order
    /// reflection lists them in).  Throws <see cref="CompiledTypeRegistryException"/> if the list cannot be built (e.g. a
    /// Type fails to load because a dependency is missing) or is not a list of distinct Types from <see cref="Assembly"/>.
    /// </summary>
    public IReadOnlyList<Type> GetTypes()
    {
        var types = Volatile.Read(ref _types);
        if (types != null)
            return types;

        try
        {
            types = _getTypes();
        }
        catch (Exception e)
        {
            throw new CompiledTypeRegistryException(
                $"The Types registered by assembly '{Assembly.FullName}' could not be listed: {e.Message}", e);
        }

        if (types == null)
            throw new CompiledTypeRegistryException($"Assembly '{Assembly.FullName}' registered a null list of Types");

        var seen = new HashSet<Type>();

        foreach (var type in types)
        {
            if (type == null)
                throw new CompiledTypeRegistryException(
                    $"Assembly '{Assembly.FullName}' registered a list of Types containing null");

            if (type.Assembly != Assembly)
                throw new CompiledTypeRegistryException(
                    $"Assembly '{Assembly.FullName}' registered Type '{type.FullName}' which is declared in another assembly ('{type.Assembly.FullName}')");

            if (!seen.Add(type))
                throw new CompiledTypeRegistryException(
                    $"Assembly '{Assembly.FullName}' registered Type '{type.FullName}' more than once");
        }

        Volatile.Write(ref _types, types);
        return types;
    }

    public override string ToString() => Assembly.GetName().Name;
}
