// Copyright (c) The University of Dundee 2018-2026
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories.Construction;

namespace Rdmp.Core.Repositories;

/// <summary>
/// Finds Types by name and lists the Types which implement a given interface or base class (e.g. every IAttacher), so
/// that they can be created on demand, and creates instances of them.
///
/// <para>The class name MEF is a misnomer because historically we used the Managed Extensibility Framework.  The Types
/// come from the <see cref="CompiledTypeRegistry"/>, in which every RDMP assembly lists its own Types at compile time, so
/// no assembly is scanned with reflection.  If the registry cannot be used (e.g. a registered Type fails to load or an
/// RDMP assembly was built without the generator) MEF logs a warning and scans the loaded assemblies with reflection
/// instead, as it used to, unless <see cref="RequireCompiledTypeRegistrySwitch"/> is set (as it is for the tests), in
/// which case it throws.</para>
///
/// <para>Types are found by full or short name, ignoring case.  When several Types have the same name one whose full name
/// starts with Rdmp.Core wins (the last listed if several do), otherwise the first listed wins.  Types are listed in
/// assembly load order and then metadata order, the order reflection lists them in, so the registry resolves names
/// exactly as reflection did, and <see cref="GetTypes{T}"/>, <see cref="GetGenericTypes"/> and <see cref="GetAllTypes"/>
/// return Types in that order.  Types of other assemblies (the .NET runtime, NuGet packages) are found by their exact full
/// name, as Type.GetType would find them, but not by short name, and are not listed by <see cref="GetTypes{T}"/>,
/// <see cref="GetGenericTypes"/> or <see cref="GetAllTypes"/> (unless MEF has fallen back on reflection).</para>
///
/// <para>This class is AssemblyLoadContext-aware: each context gets its own type cache containing only types
/// accessible from that context (the context itself + the default context). This ensures correct type identity
/// in test scenarios where assemblies may be loaded in multiple contexts.  The index built when MEF is first used is
/// shared by all contexts and holds the Types of the assemblies loaded then (in any context, as the index reflection
/// built did); the Types of assemblies loaded later go in the cache of their own context.  The cache of a collectible
/// context is dropped when it starts to unload, so that MEF does not stop it unloading.</para>
/// </summary>
public static class MEF
{
    /// <summary>
    /// Name of the AppContext switch which, when set to true, makes MEF throw a <see cref="CompiledTypeRegistryException"/>
    /// rather than log a warning and fall back on reflection when the <see cref="CompiledTypeRegistry"/> cannot be used.  The
    /// test projects set it (in Directory.Build.props) so that a problem with the registry fails the tests.
    /// </summary>
    public const string RequireCompiledTypeRegistrySwitch = "Rdmp.Core.Repositories.MEF.RequireCompiledTypeRegistry";

    /// <summary>
    /// The Types found when MEF was first used (from the <see cref="CompiledTypeRegistry"/>, or reflection if that could not
    /// be used).  Shared by all contexts since it is built from the assemblies loaded at that time.
    /// </summary>
    private static readonly Lazy<TypeIndex> Primary = new(BuildPrimary, LazyThreadSafetyMode.ExecutionAndPublication);

    // Per-context caches.  The cache of a collectible context is dropped when the context starts to unload: it holds the
    // context's Types, which would otherwise keep it loaded (the runtime keeps the context alive until its Types are gone)
    private static readonly ConditionalWeakTable<AssemblyLoadContext, ContextCache> _contextCaches = new();

    /// <summary>
    /// The collectible contexts MEF has seen, each with whether it has started to unload
    /// </summary>
    private static readonly ConditionalWeakTable<AssemblyLoadContext, StrongBox<bool>> CollectibleContexts = new();

    // Cache for the default context (used when CurrentContextualReflectionContext is null)
    private static readonly ContextCache _defaultContextCache = new();

    private static readonly Dictionary<string, Exception> badAssemblies = new();

    /// <summary>
    /// Assemblies loaded since MEF was first used whose Types have yet to be added to the caches.  Assembly load events only
    /// queue the assembly (so that they never wait for, or re-enter, the building of <see cref="Primary"/>); the queue is
    /// emptied the next time a Type is looked up.
    /// </summary>
    private static readonly ConcurrentQueue<Assembly> PendingAssemblies = new();

    private static readonly object PendingLock = new();
    private static int _pendingCount;

    /// <summary>
    /// Name prefixes of assemblies whose Types are left out of the Type lookup caches.  DocumentFormat.OpenXml is
    /// excluded because it contains thousands of classes with generic names (e.g. Name, Body, Table) which would
    /// otherwise be returned when an unrelated Type is looked up by its short name.  Types in these assemblies are
    /// never cached, since caching also records the short name.  One can still be found by its full name, provided
    /// no cached Type has the same short name (cached names are tried first).  Only matters when MEF falls back on
    /// reflection: the <see cref="CompiledTypeRegistry"/> only lists RDMP's own Types.
    /// </summary>
    private static readonly string[] AssembliesExcludedFromTypeLookup = { "CommandLine", "DocumentFormat.OpenXml" };

    private static readonly Assembly RdmpCoreAssembly = typeof(MEF).Assembly;
    private static readonly string RdmpCoreAssemblyName = RdmpCoreAssembly.GetName().Name;

    /// <summary>
    /// Per-AssemblyLoadContext cache for types and type hierarchies
    /// </summary>
    internal sealed class ContextCache
    {
        /// <summary>
        /// Types of assemblies loaded after <see cref="Primary"/> was built, by short and full name (as in <see cref="TypeIndex"/>)
        /// </summary>
        public ConcurrentDictionary<string, Type> LookasideTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Types outside the index which were found by their exact full name (only used with the <see cref="CompiledTypeRegistry"/>)
        /// </summary>
        public ConcurrentDictionary<string, Type> TypesFoundByFullName { get; } = new(StringComparer.Ordinal);

        public ConcurrentDictionary<Type, Type[]> TypeHierarchyCache { get; } = new();
        public HashSet<string> ProcessedAssemblies { get; } = new();
    }

    /// <summary>
    /// Types indexed by short and full name (see <see cref="IndexByName"/>), and where they came from
    /// </summary>
    internal sealed class TypeIndex
    {
        /// <summary>
        /// Indexes <paramref name="types"/> by name (see <see cref="IndexByName"/>)
        /// </summary>
        /// <param name="types">The Types to index, in assembly load order and then metadata order</param>
        /// <param name="fromCompiledTypeRegistry">True if the Types came from the registry, false if from reflection</param>
        /// <param name="assemblies">The assemblies whose Types are listed</param>
        public TypeIndex(IReadOnlyList<Type> types, bool fromCompiledTypeRegistry, IReadOnlySet<Assembly> assemblies)
        {
            TypesByName = IndexByName(types);

            // Each Type found by at least one name, once, in the order listed
            var found = new HashSet<Type>(TypesByName.Values);
            Types = types.Where(t => found.Remove(t)).ToArray();

            FromCompiledTypeRegistry = fromCompiledTypeRegistry;
            Assemblies = assemblies;
        }

        public FrozenDictionary<string, Type> TypesByName { get; }

        /// <summary>
        /// Every Type which can be found by at least one name, in the order listed (assembly load order and then metadata
        /// order), which is the order <see cref="GetTypes{T}"/>, <see cref="GetGenericTypes"/> and <see cref="GetAllTypes"/>
        /// list them in.  So the order does not depend on which other Types are loaded, and is the same for RDMP's Types
        /// whether they come from the registry or reflection.
        /// </summary>
        public Type[] Types { get; }

        /// <summary>
        /// True if the Types came from the <see cref="CompiledTypeRegistry"/>, false if from scanning assemblies with reflection
        /// </summary>
        public bool FromCompiledTypeRegistry { get; }

        /// <summary>
        /// The assemblies whose Types were indexed
        /// </summary>
        public IReadOnlySet<Assembly> Assemblies { get; }

        public TimeSpan BuildTime { get; set; }
    }

    static MEF()
    {
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
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

        // A context which is unloading gets an empty cache which is not kept
        return IsUnloading(context) ? new ContextCache() : GetCache(context);
    }

    /// <summary>
    /// Gets the cache for the context <paramref name="assembly"/> is loaded in, or null if it is in none or its context is
    /// unloading (so its Types are not to be added)
    /// </summary>
    private static ContextCache GetCacheFor(Assembly assembly)
    {
        var context = AssemblyLoadContext.GetLoadContext(assembly);

        if (context == AssemblyLoadContext.Default)
            return _defaultContextCache;

        return context == null || IsUnloading(context) ? null : GetCache(context);
    }

    private static ContextCache GetCache(AssemblyLoadContext context)
    {
        WatchForUnloading(context);
        return _contextCaches.GetOrCreateValue(context);
    }

    /// <summary>
    /// If <paramref name="context"/> is collectible, arranges for its cache to be dropped when it starts to unload
    /// </summary>
    private static void WatchForUnloading(AssemblyLoadContext context)
    {
        if (!context.IsCollectible)
            return;

        var unloading = new StrongBox<bool>();
        if (CollectibleContexts.TryAdd(context, unloading))
            context.Unloading += unloadingContext =>
            {
                unloading.Value = true;
                _contextCaches.Remove(unloadingContext);
            };
    }

    private static bool IsUnloading(AssemblyLoadContext context) =>
        context.IsCollectible && CollectibleContexts.TryGetValue(context, out var unloading) && unloading.Value;

    /// <summary>
    /// Gets assemblies accessible from the current context (current + default)
    /// </summary>
    private static IEnumerable<Assembly> GetAccessibleAssemblies()
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

        // So that an assembly whose collectible context starts to unload before MEF next runs is skipped
        if (AssemblyLoadContext.GetLoadContext(loadedAssembly) is { } context)
            WatchForUnloading(context);

        PendingAssemblies.Enqueue(loadedAssembly);
        Interlocked.Increment(ref _pendingCount);
    }

    /// <summary>
    /// Returns the index of Types (building it if MEF has not been used before), having added the Types of any assemblies
    /// loaded since it was last used to the caches
    /// </summary>
    private static TypeIndex GetPrimary()
    {
        var primary = Primary.Value;

        if (Volatile.Read(ref _pendingCount) > 0)
            ProcessPendingAssemblies(primary);

        return primary;
    }

    private static void ProcessPendingAssemblies(TypeIndex primary)
    {
        // Lookups wait here while another thread adds Types, rather than miss them
        lock (PendingLock)
        {
            while (PendingAssemblies.TryDequeue(out var assembly))
                try
                {
                    var cache = GetCacheFor(assembly);
                    if (cache != null)
                        AddAssembly(primary, assembly, cache);
                }
                finally
                {
                    Interlocked.Decrement(ref _pendingCount);
                }
        }
    }

    /// <summary>
    /// Forces a refresh of the MEF type cache for the current context.
    /// Use this after dynamically loading assemblies that need to be discovered.
    /// </summary>
    public static void RefreshTypes()
    {
        var primary = GetPrimary();
        var cache = GetCurrentCache();

        cache.LookasideTypes.Clear();
        cache.TypesFoundByFullName.Clear();
        cache.TypeHierarchyCache.Clear();
        lock (cache.ProcessedAssemblies)
        {
            cache.ProcessedAssemblies.Clear();
        }

        foreach (var assembly in GetAccessibleAssemblies())
            AddAssembly(primary, assembly, cache);
    }

    /// <summary>
    /// True if the <see cref="CompiledTypeRegistry"/> cannot be used without MEF throwing (see <see cref="RequireCompiledTypeRegistrySwitch"/>)
    /// </summary>
    private static bool CompiledTypeRegistryRequired =>
        AppContext.TryGetSwitch(RequireCompiledTypeRegistrySwitch, out var required) && required;

    /// <summary>
    /// True if MEF's Types come from the <see cref="CompiledTypeRegistry"/>.  False if MEF is finding them with reflection
    /// because the registry could not be used: the reasons are logged as warnings and listed by <see cref="ListBadAssemblies"/>.
    /// </summary>
    public static bool UsesCompiledTypeRegistry => GetPrimary().FromCompiledTypeRegistry;

    /// <summary>
    /// How long it took to find the Types, when MEF was first used
    /// </summary>
    public static TimeSpan TypeIndexBuildTime => GetPrimary().BuildTime;

    /// <summary>
    /// The assemblies whose Types were found when MEF was first used
    /// </summary>
    internal static IReadOnlySet<Assembly> IndexedAssemblies => GetPrimary().Assemblies;

    private static TypeIndex BuildPrimary()
    {
        var sw = Stopwatch.StartNew();
        var problems = new List<KeyValuePair<string, Exception>>();

        EnsureRegistered(AppDomain.CurrentDomain.GetAssemblies(), problems);

        var loadOrder = AppDomain.CurrentDomain.GetAssemblies();
        var index = BuildFromCompiledTypeRegistry(CompiledTypeRegistry.GetRegistrations(), loadOrder, problems);

        if (index == null)
        {
            ReportProblems(problems,
                "MEF could not use the compiled type registry so is finding Types with reflection instead, as RDMP used to.  This is slower, and Types from assemblies other than RDMP's can then be found by short name");
            index = BuildFromReflection(loadOrder, badAssemblies);
        }

        index.BuildTime = sw.Elapsed;
        return index;
    }

    /// <summary>
    /// Makes sure that each of the RDMP <paramref name="assemblies"/> built with the type registry generator has registered
    /// its Types (see <see cref="CompiledTypeRegistry.EnsureRegistered"/>).  An assembly whose module initializer fails is
    /// added to <paramref name="problems"/> rather than thrown, so that MEF can still fall back on reflection.
    /// </summary>
    internal static void EnsureRegistered(IEnumerable<Assembly> assemblies, List<KeyValuePair<string, Exception>> problems)
    {
        foreach (var assembly in assemblies.Where(static a => IsRdmpAssembly(a) && CompiledTypeRegistry.IsRegisteringAssembly(a)))
            try
            {
                CompiledTypeRegistry.EnsureRegistered(assembly);
            }
            catch (CompiledTypeRegistryException e)
            {
                problems.Add(new KeyValuePair<string, Exception>(assembly.FullName, e));
            }
    }

    /// <summary>
    /// Indexes the Types listed by the <paramref name="registrations"/> of the assemblies in <paramref name="loadOrder"/>,
    /// ordering the assemblies as in <paramref name="loadOrder"/> (as reflection does).  Returns null (having added the
    /// reasons to <paramref name="problems"/>) if there were already problems, any registration cannot list its Types, or an
    /// RDMP assembly in <paramref name="loadOrder"/> (one which is or references Rdmp.Core) has not registered, since its
    /// Types would then be missing.
    /// </summary>
    internal static TypeIndex BuildFromCompiledTypeRegistry(IReadOnlyList<CompiledTypeRegistration> registrations,
        IReadOnlyList<Assembly> loadOrder, List<KeyValuePair<string, Exception>> problems)
    {
        var position = new Dictionary<Assembly, int>();
        for (var i = 0; i < loadOrder.Count; i++)
            position.TryAdd(loadOrder[i], i);

        // Like reflection, only index assemblies which are loaded (not one whose context has since been unloaded)
        var present = registrations.Where(r => position.ContainsKey(r.Assembly))
            .OrderBy(r => position[r.Assembly]).ToArray();
        var registered = present.Select(static r => r.Assembly).ToHashSet();

        var rdmpCoreContext = AssemblyLoadContext.GetLoadContext(RdmpCoreAssembly);
        var alreadyReported = problems.Select(static p => p.Key).ToHashSet();

        foreach (var assembly in loadOrder.Where(a =>
                     !registered.Contains(a) && !alreadyReported.Contains(a.FullName) &&
                     AssemblyLoadContext.GetLoadContext(a) == rdmpCoreContext && IsRdmpAssembly(a)))
            problems.Add(new KeyValuePair<string, Exception>(assembly.FullName, GetNotRegisteredException(assembly)));

        var types = new List<Type>();

        foreach (var registration in present)
            try
            {
                types.AddRange(registration.GetTypes());
            }
            catch (CompiledTypeRegistryException e)
            {
                problems.Add(new KeyValuePair<string, Exception>(registration.Assembly.FullName, e));
            }

        return problems.Count > 0 ? null : new TypeIndex(types, true, registered);
    }

    /// <summary>
    /// Indexes the Types of all <paramref name="assemblies"/> (except those <see cref="IsExcludedFromTypeLookup">excluded</see>)
    /// by scanning them with reflection, as MEF did before the <see cref="CompiledTypeRegistry"/>
    /// </summary>
    /// <param name="assemblies">The assemblies to index, in load order</param>
    /// <param name="failures">Where to record assemblies whose Types cannot be listed</param>
    internal static TypeIndex BuildFromReflection(IEnumerable<Assembly> assemblies, Dictionary<string, Exception> failures)
    {
        var indexed = new HashSet<Assembly>();
        var types = new List<Type>();

        foreach (var assembly in assemblies)
        {
            if (IsExcludedFromTypeLookup(assembly))
                continue;

            indexed.Add(assembly);
            types.AddRange(AssemblyTypeLoader.GetTypesSafely(assembly, failures));
        }

        return new TypeIndex(types, false, indexed);
    }

    /// <summary>
    /// Indexes <paramref name="types"/> by short name and full name, ignoring case.  When several Types have the same name a
    /// Type whose full name starts with Rdmp.Core wins (the last such Type listed), so that e.g. Rdmp.Core's ColumnInfo is
    /// found rather than Npgsql's, otherwise the first Type listed wins.  Used whether the Types come from the registry or
    /// reflection, so given the same Types in the same order both resolve every name the same way.
    /// </summary>
    internal static FrozenDictionary<string, Type> IndexByName(IEnumerable<Type> types)
    {
        var typeByName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in types)
        {
            var fullName = type.FullName;
            if (fullName == null)
                continue;

            var takesPrecedence = fullName.StartsWith("Rdmp.Core", StringComparison.OrdinalIgnoreCase);
            var shortName = Tail(fullName);

            Add(shortName);
            if (shortName != fullName)
                Add(fullName);

            void Add(string name)
            {
                if (!typeByName.TryAdd(name, type) && takesPrecedence)
                    typeByName[name] = type;
            }
        }

        return typeByName.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds the Types of an assembly loaded after the index was built (or all accessible assemblies on <see cref="RefreshTypes"/>)
    /// to <paramref name="cache"/>.  With the registry only RDMP assemblies' Types are added: from their registration, or
    /// (loudly) with reflection if they have none.
    /// </summary>
    private static void AddAssembly(TypeIndex primary, Assembly assembly, ContextCache cache)
    {
        if (!primary.FromCompiledTypeRegistry)
        {
            // As before the registry: Types already in the index are skipped one by one
            if (!IsExcludedFromTypeLookup(assembly))
                AddTypesToCache(primary, assembly, AssemblyTypeLoader.GetTypesSafely(assembly, badAssemblies), cache);
            return;
        }

        if (primary.Assemblies.Contains(assembly) || !IsRdmpAssembly(assembly))
            return;

        Exception problem;

        try
        {
            if (CompiledTypeRegistry.IsRegisteringAssembly(assembly))
                CompiledTypeRegistry.EnsureRegistered(assembly);

            var registration = CompiledTypeRegistry.GetRegistration(assembly);

            if (registration != null)
            {
                AddTypesToCache(primary, assembly, registration.GetTypes(), cache);
                return;
            }

            problem = GetNotRegisteredException(assembly);
        }
        catch (CompiledTypeRegistryException e)
        {
            problem = e;
        }

        ReportProblems(new[] { new KeyValuePair<string, Exception>(assembly.FullName, problem) },
            $"MEF is finding the Types of assembly '{assembly.GetName().Name}' with reflection");
        AddTypesToCache(primary, assembly, AssemblyTypeLoader.GetTypesSafely(assembly, badAssemblies), cache);
    }

    /// <summary>
    /// Add types from a runtime-loaded assembly to the specified context cache
    /// </summary>
    private static void AddTypesToCache(TypeIndex primary, Assembly assembly, IEnumerable<Type> types, ContextCache cache)
    {
        // Skip if already processed in this cache
        var assemblyName = assembly.FullName;
        lock (cache.ProcessedAssemblies)
        {
            if (!cache.ProcessedAssemblies.Add(assemblyName))
                return; // Already processed
        }

        foreach (var type in types)
        {
            // Only add if not in primary dictionary
            if (type.FullName == null || primary.TypesByName.ContainsKey(type.FullName))
                continue; // Already in primary, skip

            var shortName = Tail(type.FullName);
            foreach (var alias in shortName == type.FullName ? new[] { shortName } : new[] { shortName, type.FullName })
                // Use AddOrUpdate to handle Rdmp.Core precedence
                if (type.FullName.StartsWith("Rdmp.Core", StringComparison.OrdinalIgnoreCase))
                    cache.LookasideTypes[alias] = type; // Rdmp.Core takes precedence
                else
                    cache.LookasideTypes.TryAdd(alias, type);
        }

        // Clear type hierarchy cache as new types may affect inheritance queries
        cache.TypeHierarchyCache.Clear();
    }

    /// <summary>
    /// True if <paramref name="assembly"/> is Rdmp.Core or references it, i.e. is one whose Types MEF is meant to find
    /// </summary>
    private static bool IsRdmpAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic)
            return false;

        if (assembly == RdmpCoreAssembly)
            return true;

        try
        {
            // Reads the assembly's metadata only (does not load the referenced assemblies)
            return Array.Exists(assembly.GetReferencedAssemblies(),
                static r => string.Equals(r.Name, RdmpCoreAssemblyName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static CompiledTypeRegistryException GetNotRegisteredException(Assembly assembly) =>
        new(CompiledTypeRegistry.IsRegisteringAssembly(assembly)
            ? $"Assembly '{assembly.FullName}' was built with the type registry generator but has not registered its Types with the compiled type registry"
            : $"Assembly '{assembly.FullName}' uses Rdmp.Core but was built without the type registry generator (Rdmp.Core.Generators), so its Types are not in the compiled type registry");

    /// <summary>
    /// Records <paramref name="problems"/> (see <see cref="ListBadAssemblies"/>) and logs them as a warning, or throws if the
    /// registry is required (see <see cref="RequireCompiledTypeRegistrySwitch"/>)
    /// </summary>
    private static void ReportProblems(IReadOnlyCollection<KeyValuePair<string, Exception>> problems, string consequence) =>
        ReportProblems(problems, consequence, CompiledTypeRegistryRequired, badAssemblies,
            static message => NLog.LogManager.GetCurrentClassLogger().Warn(message));

    /// <summary>
    /// Throws a <see cref="CompiledTypeRegistryException"/> describing <paramref name="problems"/> if <paramref name="required"/>,
    /// otherwise records them in <paramref name="failures"/> and passes the description, starting with what MEF does instead
    /// (<paramref name="consequence"/>), to <paramref name="warn"/>
    /// </summary>
    internal static void ReportProblems(IReadOnlyCollection<KeyValuePair<string, Exception>> problems, string consequence,
        bool required, Dictionary<string, Exception> failures, Action<string> warn)
    {
        var list = string.Join(Environment.NewLine, problems.Select(static p => $"- {p.Value.Message}"));

        if (required)
            throw new CompiledTypeRegistryException(
                $"MEF requires the compiled type registry (AppContext switch '{RequireCompiledTypeRegistrySwitch}' is set) but cannot use it:{Environment.NewLine}{list}",
                new AggregateException(problems.Select(static p => p.Value)));

        lock (failures)
        {
            foreach (var (assembly, problem) in problems)
                failures.TryAdd(assembly, problem);
        }

        warn($"{consequence}:{Environment.NewLine}{list}");
    }

    /// <summary>
    /// True if the Types in <paramref name="assembly"/> are left out of the Type lookup caches (see
    /// <see cref="AssembliesExcludedFromTypeLookup"/>)
    /// </summary>
    private static bool IsExcludedFromTypeLookup(Assembly assembly)
    {
        var name = assembly.FullName;

        return name != null && Array.Exists(AssembliesExcludedFromTypeLookup,
            prefix => name.StartsWith(prefix, StringComparison.Ordinal));
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

        return Resolve(GetPrimary(), GetCurrentCache(), typeName);
    }

    /// <summary>
    /// Finds the Type called <paramref name="typeName"/> in <paramref name="primary"/> and <paramref name="cache"/>, or
    /// failing that the loaded assemblies (see <see cref="GetType(string)"/>)
    /// </summary>
    internal static Type Resolve(TypeIndex primary, ContextCache cache, string typeName)
    {
        // Check context-specific lookaside first - these are runtime-loaded types
        if (cache.LookasideTypes.TryGetValue(typeName, out var type))
            return type;
        if (cache.LookasideTypes.TryGetValue(Tail(typeName), out type))
            return type;

        // Fast path: Check primary dictionary
        if (primary.TypesByName.TryGetValue(typeName, out type))
            return type;

        if (primary.FromCompiledTypeRegistry)
            return ResolveOutsideCompiledTypeRegistry(primary, cache, typeName);

        // Try short name in primary
        if (primary.TypesByName.TryGetValue(Tail(typeName), out type))
            return type;

        // Fallback: Use Type.GetType() for types in currently loaded assemblies not in our cache
        type = Type.GetType(typeName);
        if (type != null)
        {
            CacheForFutureLookups(cache, typeName, type);
            return type;
        }

        // Still not found - scan accessible assemblies only (current context + default)
        foreach (var assembly in GetAccessibleAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                CacheForFutureLookups(cache, typeName, type);
                return type;
            }
        }

        // Not found
        return null;
    }

    /// <summary>
    /// Finds a Type which is not in the <see cref="CompiledTypeRegistry"/> index as if the index held every loaded Type, as
    /// the one reflection built did: a Type called exactly <paramref name="typeName"/> in the first accessible assembly (in
    /// load order) which has one is found before any Type with the same short name, except one in an
    /// <see cref="IsExcludedFromTypeLookup">excluded</see> assembly.  Names which no assembly has a Type called (e.g.
    /// assembly qualified names) are then given to Type.GetType.
    /// </summary>
    private static Type ResolveOutsideCompiledTypeRegistry(TypeIndex primary, ContextCache cache, string typeName)
    {
        if (cache.TypesFoundByFullName.TryGetValue(typeName, out var type))
            return type;

        Type inExcludedAssembly = null;

        // Still not found - scan accessible assemblies only (current context + default)
        foreach (var assembly in GetAccessibleAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type == null)
                continue;

            if (!IsExcludedFromTypeLookup(assembly))
            {
                cache.TypesFoundByFullName.TryAdd(typeName, type);
                return type;
            }

            inExcludedAssembly ??= type;
        }

        // Try short name in primary
        if (primary.TypesByName.TryGetValue(Tail(typeName), out type))
            return type;

        // Types in excluded assemblies are only found by full name, and are never cached (as before)
        if (inExcludedAssembly != null)
            return inExcludedAssembly;

        // Fallback: Use Type.GetType() e.g. for assembly qualified names
        type = Type.GetType(typeName);
        if (type != null && !IsExcludedFromTypeLookup(type.Assembly))
            cache.TypesFoundByFullName.TryAdd(typeName, type);

        return type;
    }

    /// <summary>
    /// Records <paramref name="type"/> (found outside the caches) under the name it was asked for and its short name.
    /// Types from assemblies which are <see cref="IsExcludedFromTypeLookup">excluded</see> are not recorded, otherwise
    /// looking one up by its full name would make its short name resolvable from then on.
    /// </summary>
    private static void CacheForFutureLookups(ContextCache cache, string typeName, Type type)
    {
        if (IsExcludedFromTypeLookup(type.Assembly))
            return;

        cache.LookasideTypes.TryAdd(typeName, type);
        cache.LookasideTypes.TryAdd(Tail(typeName), type);
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
            return new ReadOnlyDictionary<string, Exception>(new Dictionary<string, Exception>(badAssemblies));
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
    internal static IEnumerable<Type> GetTypes(Type type)
    {
        var primary = GetPrimary();
        var cache = GetCurrentCache();

        return cache.TypeHierarchyCache.GetOrAdd(type, target =>
        {
            // Combine lookaside and primary types (lookaside first for context-specific types)
            var allTypes = cache.LookasideTypes.Values
                .Concat(primary.Types)
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
        var primary = GetPrimary();
        var cache = GetCurrentCache();
        var target = genericType.MakeGenericType(typeOfT);

        // Combine primary and context-specific lookaside types
        var allTypes = primary.Types.Concat(cache.LookasideTypes.Values).Distinct();

        return allTypes
            .Where(t => !t.IsAbstract && !t.IsGenericType && target.IsAssignableFrom(t))
            .Distinct();
    }

    public static IEnumerable<Type> GetAllTypes()
    {
        var primary = GetPrimary();
        var cache = GetCurrentCache();

        // Combine primary and context-specific lookaside types
        return primary.Types
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

}
