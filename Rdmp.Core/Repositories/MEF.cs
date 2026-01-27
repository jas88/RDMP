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
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
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
///
/// <para>This class is AssemblyLoadContext-aware: each context gets its own type cache containing only types
/// accessible from that context (the context itself + the default context). This ensures correct type identity
/// in test scenarios where assemblies may be loaded in multiple contexts.</para>
/// </summary>
public static class MEF
{
    // Primary type source: CompiledTypeRegistry (FrozenDictionary) if available, otherwise reflection-based
    // This is shared across all contexts since it's built from the default context's assemblies
    private static Lazy<IReadOnlyDictionary<string, Type>> _primaryTypes;

    // Per-context caches - automatically cleaned up when context is unloaded
    private static readonly ConditionalWeakTable<AssemblyLoadContext, ContextCache> _contextCaches = new();

    // Cache for the default context (used when CurrentContextualReflectionContext is null)
    private static ContextCache _defaultContextCache;

    private static readonly Dictionary<string, Exception> badAssemblies = new();

    /// <summary>
    /// Per-AssemblyLoadContext cache for types and type hierarchies
    /// </summary>
    private sealed class ContextCache
    {
        public ConcurrentDictionary<string, Type> LookasideTypes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<Type, Type[]> TypeHierarchyCache { get; } = new();
        public HashSet<string> ProcessedAssemblies { get; } = new();
    }

    static MEF()
    {
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        Initialize();
    }

    private static void Initialize()
    {
        _primaryTypes ??= new Lazy<IReadOnlyDictionary<string, Type>>(PopulatePrimary,
            LazyThreadSafetyMode.ExecutionAndPublication);
        _defaultContextCache ??= new ContextCache();
#if DEBUG
        Console.WriteLine("MEF: Initialized with context-aware caching");
#endif
    }

    /// <summary>
    /// Gets the current AssemblyLoadContext, or Default if not in a custom context
    /// </summary>
    private static AssemblyLoadContext CurrentContext =>
        AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default;

    /// <summary>
    /// Gets the cache for the current context
    /// </summary>
    private static ContextCache GetCurrentCache()
    {
        var context = AssemblyLoadContext.CurrentContextualReflectionContext;
        if (context == null)
            return _defaultContextCache;

        return _contextCaches.GetOrCreateValue(context);
    }

    /// <summary>
    /// Gets assemblies accessible from the current context (current + default)
    /// </summary>
    private static IEnumerable<System.Reflection.Assembly> GetAccessibleAssemblies()
    {
        var current = CurrentContext;
        var defaultCtx = AssemblyLoadContext.Default;

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
            {
                var ctx = AssemblyLoadContext.GetLoadContext(a);
                return ctx == current || ctx == defaultCtx;
            });
    }

    private static void OnAssemblyLoad(object _1, AssemblyLoadEventArgs ale)
    {
        var loadedAssembly = ale?.LoadedAssembly;
        if (loadedAssembly == null)
            return;

        // Determine which context the assembly was loaded into
        var assemblyContext = AssemblyLoadContext.GetLoadContext(loadedAssembly);

        // Add to the appropriate context's cache
        if (assemblyContext == AssemblyLoadContext.Default)
        {
            AddAssemblyToCache(loadedAssembly, _defaultContextCache);
        }
        else if (assemblyContext != null)
        {
            var cache = _contextCaches.GetOrCreateValue(assemblyContext);
            AddAssemblyToCache(loadedAssembly, cache);
        }
    }

    /// <summary>
    /// Forces a refresh of the MEF type cache for the current context.
    /// Use this after dynamically loading assemblies that need to be discovered.
    /// </summary>
    public static void RefreshTypes()
    {
        var cache = GetCurrentCache();

        cache.LookasideTypes.Clear();
        cache.TypeHierarchyCache.Clear();
        lock (cache.ProcessedAssemblies)
        {
            cache.ProcessedAssemblies.Clear();
        }

        foreach (var assembly in GetAccessibleAssemblies())
        {
            AddAssemblyToCache(assembly, cache);
        }
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
#if DEBUG
            catch (Exception ex)
            {
                // Silent failure - fall back to reflection
                if (ex.Message.Contains("CompiledTypeRegistry"))
                    Console.WriteLine($"MEF: Error loading CompiledTypeRegistry: {ex.Message}");
            }
#else
            catch (Exception)
            {
                // Silent failure - fall back to reflection
            }
#endif
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
    /// Add types from a runtime-loaded assembly to the specified context cache
    /// </summary>
    private static void AddAssemblyToCache(System.Reflection.Assembly assembly, ContextCache cache)
    {
        // Skip if already processed in this cache
        var assemblyName = assembly.FullName;
        lock (cache.ProcessedAssemblies)
        {
            if (!cache.ProcessedAssemblies.Add(assemblyName))
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
                        cache.LookasideTypes[alias] = type; // Rdmp.Core takes precedence
                    }
                    else
                    {
                        cache.LookasideTypes.TryAdd(alias, type);
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

        // Clear type hierarchy cache as new types may affect inheritance queries
        cache.TypeHierarchyCache.Clear();
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
    /// are cached per AssemblyLoadContext.</para>
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static Type GetType(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        var cache = GetCurrentCache();

        // Check context-specific lookaside first - these are runtime-loaded types
        if (cache.LookasideTypes.TryGetValue(typeName, out var type))
            return type;
        if (cache.LookasideTypes.TryGetValue(Tail(typeName), out type))
            return type;

        // Fast path: Check primary dictionary (FrozenDictionary from CompiledTypeRegistry)
        var primaryDict = _primaryTypes.Value;
        if (primaryDict.TryGetValue(typeName, out type))
            return type;

        // Try short name in primary
        if (primaryDict.TryGetValue(Tail(typeName), out type))
            return type;

        // Fallback: Use Type.GetType() for types in currently loaded assemblies not in our cache
        // This handles edge cases like test classes that weren't in CompiledTypeRegistry
        type = Type.GetType(typeName);
        if (type != null)
        {
            // Add to lookaside for future lookups
            cache.LookasideTypes.TryAdd(typeName, type);
            cache.LookasideTypes.TryAdd(Tail(typeName), type);
            return type;
        }

        // Still not found - scan accessible assemblies only (current context + default)
        foreach (var assembly in GetAccessibleAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                // Add to lookaside for future lookups
                cache.LookasideTypes.TryAdd(typeName, type);
                cache.LookasideTypes.TryAdd(Tail(typeName), type);
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
    /// all exported implementers. Results are cached per AssemblyLoadContext.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static IEnumerable<Type> GetTypes(Type type)
    {
        var cache = GetCurrentCache();

        return cache.TypeHierarchyCache.GetOrAdd(type, target =>
        {
            // Combine lookaside and primary types (lookaside first for context-specific types)
            var allTypes = cache.LookasideTypes.Values
                .Concat(_primaryTypes.Value.Values)
                .Distinct();

            return allTypes
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .Where(target.IsAssignableFrom)
                .Distinct()
                .ToArray();
        });
    }

    /// <summary>
    /// Static plugin registry for AOT/Trim compatibility.
    /// This replaces runtime plugin loading with compile-time registration.
    /// All "plugins" (RdmpDicom, Plugins, Plugins.UI) are now integrated into the main application.
    /// </summary>
    public static class PluginRegistry
    {
        // Note: We use GetType() with string names rather than typeof() because some plugin types
        // may be in conditional compilation blocks or may not be available in all build configurations.
        // This approach is more flexible for gradual migration to compile-time registration.
        private static readonly Lazy<Dictionary<Type, Type[]>> _registeredPlugins = new(BuildPluginRegistry);

        private static Dictionary<Type, Type[]> BuildPluginRegistry()
        {
            var registry = new Dictionary<Type, Type[]>();

            // IPluginUserInterface implementations
            // These provide custom UI integration and right-click menu items
            var pluginUITypes = new List<Type>();
            AddTypeIfExists(pluginUITypes, "Rdmp.Dicom.UI.RdmpDicomUserInterface"); // WinForms UI
            AddTypeIfExists(pluginUITypes, "Rdmp.Dicom.RdmpDicomConsoleUserInterface"); // Console UI
            AddTypeIfExists(pluginUITypes, "Rdmp.Core.Providers.ExamplePluginCohortCompilerUI"); // Example UI
            if (pluginUITypes.Count > 0)
                registry[MEF.GetType("Rdmp.Core.IPluginUserInterface")] = pluginUITypes.ToArray();

            // IPluginCohortCompiler implementations
            // These provide custom cohort building tasks (e.g., SemEHR API integration)
            var pluginCohortCompilers = new List<Type>();
            AddTypeIfExists(pluginCohortCompilers, "Rdmp.Dicom.ExternalApis.SemEHRApiCaller");
            AddTypeIfExists(pluginCohortCompilers, "Rdmp.Core.CohortCreation.Execution.ExamplePluginCohortCompiler");
            if (pluginCohortCompilers.Count > 0)
                registry[MEF.GetType("Rdmp.Core.CohortCreation.Execution.IPluginCohortCompiler")] = pluginCohortCompilers.ToArray();

            // PluginPatcher implementations
            // These provide database schema patching for plugin-specific tables
            var pluginPatchers = new List<Type>();
            AddTypeIfExists(pluginPatchers, "Rdmp.Dicom.SMIDatabasePatcher");
            if (pluginPatchers.Count > 0)
                registry[MEF.GetType("Rdmp.Core.MapsDirectlyToDatabaseTable.Versioning.PluginPatcher")] = pluginPatchers.ToArray();

            // IPluginRepositoryFinder implementations
            // Currently no implementations - this interface may be deprecated
            // registry[GetType("Rdmp.Core.Startup.IPluginRepositoryFinder")] = Array.Empty<Type>();

            return registry;
        }

        private static void AddTypeIfExists(List<Type> list, string typeName)
        {
            var type = MEF.GetType(typeName);
            if (type != null)
                list.Add(type);
        }

        /// <summary>
        /// Gets registered plugin types for the specified interface/base class.
        /// Returns an empty array if no plugins are registered for the type.
        /// </summary>
        public static IEnumerable<Type> GetPluginTypes<T>() where T : class
        {
            return GetPluginTypes(typeof(T));
        }

        /// <summary>
        /// Gets registered plugin types for the specified interface/base class.
        /// Returns an empty array if no plugins are registered for the type.
        /// </summary>
        public static IEnumerable<Type> GetPluginTypes(Type type)
        {
            return _registeredPlugins.Value.TryGetValue(type, out var types)
                ? types
                : Array.Empty<Type>();
        }

        /// <summary>
        /// Returns true if any plugins are registered for the specified type.
        /// </summary>
        public static bool HasPlugins<T>() where T : class
        {
            return _registeredPlugins.Value.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Gets all registered plugin types across all interfaces.
        /// </summary>
        public static IEnumerable<Type> GetAllPluginTypes()
        {
            return _registeredPlugins.Value.Values.SelectMany(types => types).Distinct();
        }
    }

    /// <summary>
    /// Returns all MEF exported classes decorated with the specified generic export e.g.
    /// </summary>
    /// <param name="genericType"></param>
    /// <param name="typeOfT"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetGenericTypes(Type genericType, Type typeOfT)
    {
        var cache = GetCurrentCache();
        var target = genericType.MakeGenericType(typeOfT);

        // Combine primary and context-specific lookaside types
        var allTypes = _primaryTypes.Value.Values.Concat(cache.LookasideTypes.Values).Distinct();

        return allTypes
            .Where(t => !t.IsAbstract && !t.IsGenericType && target.IsAssignableFrom(t))
            .Distinct();
    }

    public static IEnumerable<Type> GetAllTypes()
    {
        var cache = GetCurrentCache();

        // Combine primary and context-specific lookaside types
        return _primaryTypes.Value.Values
            .Concat(cache.LookasideTypes.Values)
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

        var instance = (T)AotObjectConstructor.ConstructIfPossible(typeToCreateAsType, args) ??
                       throw new ObjectLacksCompatibleConstructorException(
                           $"Could not construct a {typeof(T)} using the {args.Length} constructor arguments");
        return instance;
    }

    /// <summary>
    /// Registers a type for testing. This method is deprecated - the context-aware caching
    /// should handle type identity automatically. Kept for backward compatibility.
    /// </summary>
    [Obsolete("Context-aware caching should handle type identity automatically. This method may be removed in a future version.")]
    public static void AddTypeToCatalogForTesting(Type p0)
    {
        ArgumentNullException.ThrowIfNull(p0);

        var cache = GetCurrentCache();

        // Add to current context's lookaside
        cache.LookasideTypes[p0.FullName!] = p0;
        cache.LookasideTypes[Tail(p0.FullName!)] = p0;

        // Clear type hierarchy cache so GetTypes<T>() includes the new type
        cache.TypeHierarchyCache.Clear();
    }
}
