// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rdmp.Core.Repositories;

namespace Tests.Common;

/// <summary>
/// Checks of the Types which an assembly registers with the <see cref="CompiledTypeRegistry"/>.  <see cref="MEF"/> indexes
/// the registered Types with the same code it uses for the Types reflection finds, so an assembly which registers the same
/// Types as reflection finds, in the same order, has its Types found by the same names whichever is used.
/// </summary>
public static class CompiledTypeRegistryChecks
{
    /// <summary>
    /// Asserts that <paramref name="assembly"/> registers exactly the Types which reflection finds in it, in the order
    /// reflection lists them, apart from those the compiler generates (see <see cref="IsSynthesized"/>).  Types with names
    /// containing '&lt;' may be registered or not: the compiler generates most, but also gives file local Types such names.
    /// </summary>
    public static void AssertRegistersTheTypesReflectionFinds(Assembly assembly)
    {
        var registered = CompiledTypeRegistry.GetRegisteredTypes(assembly);
        Assert.That(registered, Is.Not.Null,
            $"Assembly {assembly.GetName().Name} has not registered its Types with the compiled type registry");

        var registeredSet = registered.ToHashSet();
        var declared = assembly.GetTypes().Where(t => !IsSynthesized(t) || registeredSet.Contains(t)).ToArray();

        if (registered.SequenceEqual(declared))
            return;

        var notRegistered = declared.Except(registered).Select(static t => t.FullName);
        var notDeclared = registered.Except(declared).Select(static t => t.FullName);
        var firstDifference = registered.Zip(declared).TakeWhile(static p => p.First == p.Second).Count();

        Assert.Fail($"Assembly {assembly.GetName().Name} registered {registered.Count} Types but reflection finds {declared.Length}.  " +
                    $"Not registered: {string.Join(", ", notRegistered)}.  Registered but not found by reflection: {string.Join(", ", notDeclared)}.  " +
                    $"The lists first differ at position {firstDifference}");
    }

    /// <summary>
    /// True for Types which the compiler (or a source generator) creates rather than ones declared in source, which the type
    /// registry generator cannot see so does not register: closures, iterators, anonymous and file local Types etc. (names
    /// containing '&lt;'), attributes the compiler embeds (e.g. for nullable annotations) and the generated registration itself
    /// </summary>
    public static bool IsSynthesized(Type type)
    {
        for (var t = type; t != null; t = t.DeclaringType)
            if (t.Name.Contains('<') ||
                t.CustomAttributes.Any(static a => a.AttributeType.FullName == "Microsoft.CodeAnalysis.EmbeddedAttribute") ||
                (t.Namespace == "Rdmp.Core.Repositories.Generated" &&
                 t.Name.StartsWith("GeneratedTypeRegistration_", StringComparison.Ordinal)))
                return true;

        return false;
    }
}
