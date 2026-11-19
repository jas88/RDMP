// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Rdmp.Core.Repositories;

/// <summary>
/// The Types declared in each RDMP assembly, listed at compile time so that <see cref="MEF"/> can find them without
/// scanning assemblies with reflection.
///
/// <para>Every project in the RDMP solution runs the type registry generator in Rdmp.Core.Generators (it is referenced in
/// Directory.Build.props).  The generated code marks the assembly with <see cref="CompiledTypeRegistryAttribute"/> and calls
/// <see cref="Register"/> from a module initializer, which the runtime runs before any other code in the assembly.  Each
/// assembly lists only its own Types, so nothing is registered from an assembly which is not deployed, and the list is
/// only built when <see cref="MEF"/> first needs it.</para>
///
/// <para>Types of other assemblies (the .NET runtime, NuGet packages) are not registered.  <see cref="MEF"/> finds them by
/// their exact full name only.</para>
///
/// <para>The registration of an assembly in a collectible AssemblyLoadContext is forgotten when the context starts to
/// unload, so that the registry does not stop it unloading.</para>
/// </summary>
public static class CompiledTypeRegistry
{
    private static readonly object Lock = new();
    private static readonly List<CompiledTypeRegistration> Registrations = new();
    private static readonly Dictionary<Assembly, CompiledTypeRegistration> RegistrationsByAssembly = new();

    /// <summary>
    /// Registers the Types declared in <paramref name="assembly"/>.  Called by the code which the type registry generator
    /// adds to each RDMP assembly.  Registering an assembly which is already registered does nothing.
    /// </summary>
    /// <param name="assembly">The assembly whose Types are listed</param>
    /// <param name="getTypes">Returns every Type declared in <paramref name="assembly"/>, in metadata order</param>
    public static void Register(Assembly assembly, Func<Type[]> getTypes)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(getTypes);

        lock (Lock)
        {
            if (RegistrationsByAssembly.ContainsKey(assembly))
                return;

            var registration = new CompiledTypeRegistration(assembly, getTypes);
            Registrations.Add(registration);
            RegistrationsByAssembly.Add(assembly, registration);
        }

        // The registration refers to the assembly and its Types, which would otherwise never be collected
        if (AssemblyLoadContext.GetLoadContext(assembly) is { IsCollectible: true } context)
            context.Unloading += _ => Unregister(assembly);
    }

    private static void Unregister(Assembly assembly)
    {
        lock (Lock)
        {
            if (RegistrationsByAssembly.Remove(assembly, out var registration))
                Registrations.Remove(registration);
        }
    }

    /// <summary>
    /// True if <paramref name="assembly"/> was built with the type registry generator, i.e. it registers its Types when
    /// its module initializer runs
    /// </summary>
    public static bool IsRegisteringAssembly(Assembly assembly) =>
        !assembly.IsDynamic && assembly.IsDefined(typeof(CompiledTypeRegistryAttribute), false);

    /// <summary>
    /// Makes sure that <paramref name="assembly"/> (one built with the type registry generator) has registered its Types,
    /// by running its module initializer if that has not run yet.  Module initializers normally run when the assembly's
    /// code is first used, so an assembly can be loaded (e.g. by reflection) without having registered.  Throws
    /// <see cref="CompiledTypeRegistryException"/> if the module initializer fails.
    /// </summary>
    internal static void EnsureRegistered(Assembly assembly)
    {
        try
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        catch (Exception e)
        {
            throw new CompiledTypeRegistryException(
                $"The module initializer of assembly '{assembly.FullName}', which registers its Types, failed: {e.GetBaseException().Message}",
                e);
        }
    }

    /// <summary>
    /// Returns the Types registered by <paramref name="assembly"/> (every Type declared in it, in metadata order), or null if
    /// it has not registered.  Throws <see cref="CompiledTypeRegistryException"/> if the registered Types cannot be listed.
    /// </summary>
    public static IReadOnlyList<Type> GetRegisteredTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (IsRegisteringAssembly(assembly))
            EnsureRegistered(assembly);

        return GetRegistration(assembly)?.GetTypes();
    }

    /// <summary>
    /// Returns the registrations made so far, in the order they were made
    /// </summary>
    internal static IReadOnlyList<CompiledTypeRegistration> GetRegistrations()
    {
        lock (Lock)
        {
            return Registrations.ToArray();
        }
    }

    /// <summary>
    /// Returns the registration made by <paramref name="assembly"/>, or null if it has not registered
    /// </summary>
    internal static CompiledTypeRegistration GetRegistration(Assembly assembly)
    {
        lock (Lock)
        {
            return RegistrationsByAssembly.GetValueOrDefault(assembly);
        }
    }
}
