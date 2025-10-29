// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories.Construction;

namespace Rdmp.Core.Repositories;

/// <summary>
/// Provides support for downloading Plugins out of the Catalogue Database, identifying Exports and building the
/// <see cref="SafeDirectoryCatalog"/>.  It also includes methods for creating instances of the exported Types.
/// 
/// <para>The class name MEF is a misnomer because historically we used the Managed Extensibility Framework (but now we
/// just grab everything with reflection)</para>
/// </summary>
public static class MEF
{
    // Primary type source: CompiledTypeRegistry (FrozenDictionary) if available, otherwise reflection-based
    private static Lazy<IReadOnlyDictionary<string, Type>> _primaryTypes = null;

    // Lookaside cache for runtime-loaded assemblies not in CompiledTypeRegistry
    private static readonly ConcurrentDictionary<string, Type> _lookasideTypes = new(StringComparer.OrdinalIgnoreCase);

    // Cache for type hierarchy queries (GetTypes<T>)
    private static readonly ConcurrentDictionary<Type, Type[]> TypeCache = new();

    private static readonly Dictionary<string, Exception> badAssemblies = new();

    // Track assemblies already processed to avoid duplicate work
    private static readonly HashSet<string> _processedAssemblies = new();

    static MEF()
    {
        AppDomain.CurrentDomain.AssemblyLoad += Flush;
        Flush(null, null);
    }

    private static void Flush(object _1, AssemblyLoadEventArgs ale)
    {
        // On initialization, create primary type source
        if (ale is null)
        {
            if (_primaryTypes is null)
            {
#if DEBUG
                Console.WriteLine("MEF: Initializing primary type source");
#endif
                _primaryTypes = new Lazy<IReadOnlyDictionary<string, Type>>(PopulatePrimary,
                    LazyThreadSafetyMode.ExecutionAndPublication);
            }
            return;
        }

        // On assembly load, add to lookaside only
        var loadedAssembly = ale.LoadedAssembly;
        if (loadedAssembly != null)
        {
            AddAssemblyToLookaside(loadedAssembly);
        }

        // Clear type hierarchy cache as new types may affect inheritance queries
        TypeCache.Clear();
    }

    /// <summary>
    /// Forces a refresh of the MEF type cache. Use this after dynamically loading assemblies
    /// that need to be discovered. This is primarily for testing scenarios where assemblies
    /// are loaded via typeof() after MEF has already been initialized.
    /// </summary>
    public static void RefreshTypes()
    {
        // Clear lookaside and re-scan all assemblies
        _lookasideTypes.Clear();
        lock (_processedAssemblies)
        {
            _processedAssemblies.Clear();
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            AddAssemblyToLookaside(assembly);
        }
        TypeCache.Clear();
    }

    private static IReadOnlyDictionary<string, Type> PopulatePrimary()
    {
        var sw = Stopwatch.StartNew();

        // Try to use compile-time generated registry (FrozenDictionary) if available
        // Search all loaded assemblies since Type.GetType() doesn't work across assembly boundaries
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var compiledRegistryType = assembly.GetType("Rdmp.Core.Repositories.CompiledTypeRegistry");
                if (compiledRegistryType != null)
                {
                    var getTypeMethod = compiledRegistryType.GetMethod("GetAllTypes", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (getTypeMethod != null)
                    {
                        var compiledTypes = getTypeMethod.Invoke(null, null) as IEnumerable<KeyValuePair<string, Type>>;
                        if (compiledTypes != null)
                        {
                            // Use FrozenDictionary from CompiledTypeRegistry for optimal lookup performance
                            var frozen = compiledTypes.ToFrozenDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value,
                                StringComparer.OrdinalIgnoreCase);

#if DEBUG
                            Console.WriteLine($"MEF: Using CompiledTypeRegistry with {frozen.Count} types (loaded in {sw.ElapsedMilliseconds}ms)");
#endif
                            return frozen; // Early return - use compile-time registry
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent failure - fall back to reflection
#if DEBUG
                if (ex.Message.Contains("CompiledTypeRegistry"))
                    Console.WriteLine($"MEF: Error loading CompiledTypeRegistry: {ex.Message}");
#endif
            }
        }

        // Fallback: Use reflection to scan all assemblies (slower)
#if DEBUG
        Console.WriteLine("MEF: CompiledTypeRegistry not found, falling back to reflection");
#endif
        var typeByName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var assembliesProcessed = 0;
        var assembliesSkipped = 0;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip CommandLine assembly
            if (assembly.FullName?.StartsWith("CommandLine", StringComparison.Ordinal) == true)
            {
                assembliesSkipped++;
                continue;
            }

            assembliesProcessed++;
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var alias in new[]
                             {
                             Tail(type.FullName), type.FullName, Tail(type.FullName).ToUpperInvariant(),
                             type.FullName?.ToUpperInvariant()
                         }.Where(static x => x is not null).Distinct())
                        if (!typeByName.TryAdd(alias, type) &&
                            type.FullName?.StartsWith("Rdmp.Core", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // Simple hack so Rdmp.Core types like ColumnInfo take precedence over others like System.Data.Select+ColumnInfo
                            typeByName.Remove(alias);
                            typeByName.Add(alias, type);
                        }
                }
            }
            catch (Exception e)
            {
                lock (badAssemblies)
                {
                    badAssemblies.TryAdd(assembly.FullName, e);
                }
            }
        }

#if DEBUG
        Console.WriteLine($"MEF: Reflection fallback processed {assembliesProcessed} assemblies, found {typeByName.Count} types in {sw.ElapsedMilliseconds}ms");
#endif

        // Return as FrozenDictionary for optimal lookup performance
        return typeByName.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Add types from a runtime-loaded assembly to the lookaside cache if not already in primary
    /// </summary>
    private static void AddAssemblyToLookaside(System.Reflection.Assembly assembly)
    {
        // Skip if already processed
        var assemblyName = assembly.FullName;
        lock (_processedAssemblies)
        {
            if (!_processedAssemblies.Add(assemblyName))
                return; // Already processed
        }

        // Skip CommandLine and other noise assemblies
        if (assemblyName?.StartsWith("CommandLine", StringComparison.Ordinal) == true)
            return;

        try
        {
            foreach (var type in assembly.GetTypes())
            {
                // Only add if not in primary dictionary
                var primaryDict = _primaryTypes?.Value;
                if (primaryDict != null && primaryDict.ContainsKey(type.FullName))
                    continue; // Already in primary, skip

                foreach (var alias in new[]
                         {
                         Tail(type.FullName), type.FullName, Tail(type.FullName).ToUpperInvariant(),
                         type.FullName?.ToUpperInvariant()
                     }.Where(static x => x is not null).Distinct())
                {
                    // Use AddOrUpdate to handle Rdmp.Core precedence
                    if (type.FullName?.StartsWith("Rdmp.Core", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _lookasideTypes[alias] = type; // Rdmp.Core takes precedence
                    }
                    else
                    {
                        _lookasideTypes.TryAdd(alias, type);
                    }
                }
            }
        }
        catch (Exception e)
        {
            lock (badAssemblies)
            {
                badAssemblies.TryAdd(assemblyName, e);
            }
        }
    }

    private static string Tail(string full)
    {
        var off = full.LastIndexOf(".", StringComparison.Ordinal) + 1;
        return off == 0 ? full : full[off..];
    }


    /// <summary>
    /// Looks up the given Type in all loaded assemblies (during <see cref="Startup.Startup"/>).  Returns null
    /// if the Type is not found.
    ///
    /// <para>This method supports both fully qualified Type names and Name only (although this is slower).  Answers
    /// are cached.</para>
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static Type GetType(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        // Fast path: Check primary dictionary (FrozenDictionary from CompiledTypeRegistry)
        var primaryDict = _primaryTypes.Value;
        if (primaryDict.TryGetValue(typeName, out var type))
            return type;

        // Try short name in primary
        if (primaryDict.TryGetValue(Tail(typeName), out type))
            return type;

        // Slower path: Check lookaside for runtime-loaded assemblies
        if (_lookasideTypes.TryGetValue(typeName, out type))
            return type;

        // Try short name in lookaside
        if (_lookasideTypes.TryGetValue(Tail(typeName), out type))
            return type;

        // Fallback: Use Type.GetType() for types in currently loaded assemblies not in our cache
        // This handles edge cases like test classes that weren't in CompiledTypeRegistry
        type = Type.GetType(typeName);
        if (type != null)
        {
            // Add to lookaside for future lookups
            _lookasideTypes.TryAdd(typeName, type);
            _lookasideTypes.TryAdd(Tail(typeName), type);
            return type;
        }

        // Still not found - scan all loaded assemblies as last resort
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                // Add to lookaside for future lookups
                _lookasideTypes.TryAdd(typeName, type);
                _lookasideTypes.TryAdd(Tail(typeName), type);
                return type;
            }
        }

        // Not found
        return null;
    }

    public static Type GetType(string type, Type expectedBaseClass)
    {
        var t = GetType(type);

        return !expectedBaseClass.IsAssignableFrom(t)
            ? throw new Exception(
                $"Found Type {t?.FullName} for '{type}' did not implement expected base class/interface '{expectedBaseClass}'")
            : t;
    }

    public static IReadOnlyDictionary<string, Exception> ListBadAssemblies()
    {
        lock (badAssemblies)
        {
            return new ReadOnlyDictionary<string, Exception>(badAssemblies);
        }
    }

    /// <summary>
    /// 
    /// <para>Turns the legit C# name:
    /// DataLoadEngine.DataFlowPipeline.IDataFlowSource`1[[System.Data.DataTable, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]</para>
    /// 
    /// <para>Into a proper C# code:
    /// IDataFlowSource&lt;DataTable&gt;</para>
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static string GetCSharpNameForType(Type t)
    {
        if (!t.IsGenericType) return t.Name;

        if (t.GenericTypeArguments.Length != 1)
            throw new NotSupportedException(
                "Generic type has more than 1 token (e.g. T1,T2) so no idea what MEF would call it");

        var genericTypeName = t.GetGenericTypeDefinition().Name;

        Debug.Assert(genericTypeName.EndsWith("`1", StringComparison.Ordinal));
        genericTypeName = genericTypeName[..^"`1".Length];

        var underlyingType = t.GenericTypeArguments.Single().Name;
        return $"{genericTypeName}<{underlyingType}>";
    }

    public static IEnumerable<Type> GetTypes<T>() => GetTypes(typeof(T));

    /// <summary>
    /// Returns MEF exported Types which inherit or implement <paramref name="type"/>.  E.g. pass IAttacher to see
    /// all exported implementers
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static IEnumerable<Type> GetTypes(Type type)
    {
        return TypeCache.GetOrAdd(type, target =>
        {
            // Combine primary and lookaside types
            var allTypes = _primaryTypes.Value.Values
                .Concat(_lookasideTypes.Values)
                .Distinct();

            return allTypes
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .Where(target.IsAssignableFrom)
                .Distinct()
                .ToArray();
        });
    }

    /// <summary>
    /// Returns all MEF exported classes decorated with the specified generic export e.g.
    /// </summary>
    /// <param name="genericType"></param>
    /// <param name="typeOfT"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetGenericTypes(Type genericType, Type typeOfT)
    {
        var target = genericType.MakeGenericType(typeOfT);

        // Combine primary and lookaside types
        var allTypes = _primaryTypes.Value.Values.Concat(_lookasideTypes.Values).Distinct();

        return allTypes
            .Where(t => !t.IsAbstract && !t.IsGenericType && target.IsAssignableFrom(t))
            .Distinct();
    }

    public static IEnumerable<Type> GetAllTypes()
    {
        // Combine primary and lookaside types
        return _primaryTypes.Value.Values
            .Concat(_lookasideTypes.Values)
            .Distinct();
    }

    /// <summary>
    /// Creates an instance of the named class with the provided constructor arguments
    /// </summary>
    /// <typeparam name="T">The base/interface of the Type you want to create e.g. IAttacher</typeparam>
    /// <returns></returns>
    public static T CreateA<T>(string typeToCreate, params object[] args)
    {
        var typeToCreateAsType = GetType(typeToCreate) ?? throw new Exception($"Could not find Type '{typeToCreate}'");

        //can we cast to T?
        if (!typeof(T).IsAssignableFrom(typeToCreateAsType))
            throw new Exception(
                $"Requested typeToCreate '{typeToCreate}' was not assignable to the required Type '{typeof(T).Name}'");

        var instance = (T)ObjectConstructor.ConstructIfPossible(typeToCreateAsType, args) ??
                       throw new ObjectLacksCompatibleConstructorException(
                           $"Could not construct a {typeof(T)} using the {args.Length} constructor arguments");
        return instance;
    }

    public static void AddTypeToCatalogForTesting(Type p0)
    {
        ArgumentNullException.ThrowIfNull(p0);

        // Check if type exists in either dictionary (by value, not just key)
        var inPrimary = _primaryTypes.Value.ContainsKey(p0.FullName) ||
                       _primaryTypes.Value.Values.Contains(p0);
        var inLookaside = _lookasideTypes.ContainsKey(p0.FullName) ||
                         _lookasideTypes.Values.Contains(p0);

        if (!inPrimary && !inLookaside)
            throw new Exception($"Type {p0.FullName} was not preloaded");
    }
}