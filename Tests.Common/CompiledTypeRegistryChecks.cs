// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// Namespace of the Type which Coverlet (which collects code coverage in CI) adds to each assembly it instruments
    /// </summary>
    private const string CoverletTrackerNamespace = "Coverlet.Core.Instrumentation.Tracker";

    private static readonly ConcurrentDictionary<Assembly, Type> EmbeddedAttributes = new();
    private static readonly ConcurrentDictionary<Assembly, bool> InstrumentedByCoverlet = new();

    /// <summary>
    /// Asserts that <paramref name="assembly"/> registers exactly the Types which reflection finds in it, in the order
    /// reflection lists them, apart from those the compiler generates (see <see cref="IsSynthesized"/>) and those added after
    /// it was compiled (see <see cref="IsAddedAfterCompilation"/>).  Types with names containing '&lt;' may be registered or
    /// not: the compiler generates most, but also gives file local Types such names.
    /// </summary>
    public static void AssertRegistersTheTypesReflectionFinds(Assembly assembly)
    {
        var registered = CompiledTypeRegistry.GetRegisteredTypes(assembly);
        Assert.That(registered, Is.Not.Null,
            $"Assembly {assembly.GetName().Name} has not registered its Types with the compiled type registry");

        var registeredSet = registered.ToHashSet();
        var declared = InCompilerOrder(
            assembly.GetTypes().Where(t =>
                !IsAddedAfterCompilation(t) && (!IsSynthesized(t) || registeredSet.Contains(t))),
            assembly).ToArray();

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
        // The compiler embeds its own copy of EmbeddedAttribute in each assembly which needs one, and marks with it the
        // attributes it embeds
        var embedded = EmbeddedAttributes.GetOrAdd(type.Assembly,
            static a => a.GetType("Microsoft.CodeAnalysis.EmbeddedAttribute", false));

        for (var t = type; t != null; t = t.DeclaringType)
            if (t.Name.Contains('<') || (embedded != null && t.IsDefined(embedded, false)) || IsGeneratedRegistration(t))
                return true;

        return false;
    }

    private static bool IsGeneratedRegistration(Type type) =>
        type.GetCustomAttribute<GeneratedCodeAttribute>()?.Tool == "Rdmp.Core.Generators.TypeRegistryGenerator";

    /// <summary>
    /// True for Types which a tool added to the assembly after it was compiled, so which the compiler (and the type registry
    /// generator) never saw: the Tracker Type which Coverlet adds to each assembly it instruments for code coverage.  Such
    /// Types carry no mark which tells them from Types the compiler wrote, so each tool's is recognised explicitly.
    /// </summary>
    public static bool IsAddedAfterCompilation(Type type) => type.Namespace == CoverletTrackerNamespace;

    /// <summary>
    /// True if Coverlet has instrumented (rewritten) <paramref name="assembly"/>
    /// </summary>
    public static bool IsInstrumentedByCoverlet(Assembly assembly) =>
        InstrumentedByCoverlet.GetOrAdd(assembly, static a => a.GetTypes().Any(IsAddedAfterCompilation));

    /// <summary>
    /// Returns <paramref name="types"/> (Types of <paramref name="assembly"/> in the order reflection lists them) in the order
    /// the compiler wrote them.  That is the same order unless Coverlet has rewritten the assembly: the compiler writes all
    /// the Types which are not nested, then the nested Types level by level, whereas Mono.Cecil (with which Coverlet rewrites
    /// assemblies) writes each Type's nested Types straight after it.  Both keep the order of the Types at each depth of
    /// nesting, which is all that MEF's resolving of names shared by several Types depends on (only Types at the same depth
    /// can have the same name), so sorting by depth, which keeps that order, restores the compiler's.
    /// </summary>
    public static IEnumerable<Type> InCompilerOrder(IEnumerable<Type> types, Assembly assembly) =>
        IsInstrumentedByCoverlet(assembly) ? types.OrderBy(GetDepth) : types;

    private static int GetDepth(Type type)
    {
        var depth = 0;
        for (var t = type.DeclaringType; t != null; t = t.DeclaringType)
            depth++;

        return depth;
    }
}
