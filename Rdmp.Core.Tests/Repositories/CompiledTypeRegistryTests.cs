// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using NUnit.Framework;
using Rdmp.Core.Caching.Layouts;
using Rdmp.Core.Caching.Pipeline.Destinations;
using Rdmp.Core.Caching.Pipeline.Sources;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.DataLoad.Engine.Attachers;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.Validation.Constraints.Secondary;
using Tests.Common;

namespace Rdmp.Core.Tests.Repositories;

/// <summary>
/// Tests that <see cref="MEF"/> takes its Types from the <see cref="CompiledTypeRegistry"/> and finds the same Types, by
/// the same names, as reflection did.  The comparisons build both indexes from the assemblies loaded in the test process
/// with the code MEF itself uses (<see cref="MEF.BuildFromCompiledTypeRegistry"/> and <see cref="MEF.BuildFromReflection"/>)
/// and look every name up in each with <see cref="MEF.Resolve"/>.  Types which were added to an assembly after it was
/// compiled (by code coverage instrumentation) are not part of the comparisons, see
/// <see cref="CompiledTypeRegistryChecks.IsAddedAfterCompilation"/>.
/// </summary>
public class CompiledTypeRegistryTests
{
    private MEF.TypeIndex _registry;
    private MEF.TypeIndex _reflection;

    /// <summary>
    /// The RDMP assemblies (those which registered Types), in load order
    /// </summary>
    private Assembly[] _rdmpAssemblies;

    [OneTimeSetUp]
    public void BuildIndexes()
    {
        // Using MEF registers every RDMP assembly loaded so far
        Assert.That(MEF.UsesCompiledTypeRegistry, "MEF fell back on reflection, see MEF.ListBadAssemblies()");

        var loadOrder = AppDomain.CurrentDomain.GetAssemblies();
        var problems = new List<KeyValuePair<string, Exception>>();

        _registry = MEF.BuildFromCompiledTypeRegistry(CompiledTypeRegistry.GetRegistrations(), loadOrder, problems);
        Assert.That(problems.Select(static p => p.Value.Message), Is.Empty);

        _reflection = MEF.BuildFromReflection(loadOrder, new Dictionary<string, Exception>());
        _rdmpAssemblies = loadOrder.Where(_registry.Assemblies.Contains).ToArray();

        // So that the comparisons cannot pass by comparing nothing
        Assert.That(_rdmpAssemblies, Is.SupersetOf(new[]
        {
            typeof(MEF).Assembly, typeof(CompiledTypeRegistryTests).Assembly, typeof(UnitTests).Assembly
        }));
        Assert.That(CompiledTypeRegistry.GetRegisteredTypes(typeof(MEF).Assembly), Has.Count.GreaterThan(1000));
    }

    [Test]
    public void MEF_UsesTheCompiledTypeRegistry()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MEF.UsesCompiledTypeRegistry, "MEF fell back on reflection, see MEF.ListBadAssemblies()");
            Assert.That(MEF.IndexedAssemblies, Is.SupersetOf(new[]
            {
                typeof(MEF).Assembly, typeof(CompiledTypeRegistryTests).Assembly, typeof(UnitTests).Assembly
            }));
            Assert.That(MEF.ListBadAssemblies().Values.OfType<CompiledTypeRegistryException>(), Is.Empty);

            // Reflection lists the Types of every loaded assembly, e.g. System.String, the registry only RDMP's
            Assert.That(MEF.GetAllTypes().Where(static t => CompiledTypeRegistry.GetRegistration(t.Assembly) == null),
                Is.Empty);
            Assert.That(MEF.GetAllTypes(), Does.Not.Contain(typeof(string)));

            // Nor Types added to RDMP's assemblies after they were compiled (e.g. by code coverage)
            Assert.That(MEF.GetAllTypes().Where(CompiledTypeRegistryChecks.IsAddedAfterCompilation), Is.Empty);
        });
    }

    /// <summary>
    /// The registry lists the Types of each RDMP assembly in the order reflection lists them, so that the index MEF builds
    /// from either (with the same code) resolves names which several Types share in the same way
    /// </summary>
    [Test]
    public void Registrations_ListTheSameTypesAsReflection_InTheSameOrder()
    {
        TestContext.Out.WriteLine(
            $"Comparing the Types of {string.Join(", ", _rdmpAssemblies.Select(static a => $"{a.GetName().Name} ({CompiledTypeRegistry.GetRegisteredTypes(a).Count}{(CompiledTypeRegistryChecks.IsInstrumentedByCoverlet(a) ? ", instrumented by Coverlet" : "")})"))}");

        Assert.Multiple(() =>
        {
            foreach (var assembly in _rdmpAssemblies)
                CompiledTypeRegistryChecks.AssertRegistersTheTypesReflectionFinds(assembly);
        });
    }

    /// <summary>
    /// MEF lists RDMP's Types (in <see cref="MEF.GetTypes{T}"/>, <see cref="MEF.GetGenericTypes"/> and
    /// <see cref="MEF.GetAllTypes"/>) in the same order whether they come from the registry or reflection
    /// </summary>
    [Test]
    public void Types_AreListedInTheSameOrderAsWithReflection()
    {
        var registered = _registry.Types.ToHashSet();
        var withReflection = _reflection.Types
            .Where(t => IsRdmp(t) && !CompiledTypeRegistryChecks.IsAddedAfterCompilation(t) &&
                        (!IsSynthesized(t) || registered.Contains(t)))
            .GroupBy(static t => t.Assembly)
            .SelectMany(static g => CompiledTypeRegistryChecks.InCompilerOrder(g, g.Key));

        Assert.That(_registry.Types, Has.Length.GreaterThan(1500));
        Assert.That(_registry.Types, Is.EqualTo(withReflection));
    }

    [Test]
    public void Registrations_IncludeFileLocalTypes() =>
        Assert.That(CompiledTypeRegistry.GetRegisteredTypes(typeof(CompiledTypeRegistryTests).Assembly),
            Does.Contain(typeof(FileLocalStringList)));

    /// <summary>
    /// Every name of every Type reflection finds in the RDMP assemblies (full and short names, in their own, upper and lower
    /// case) resolves to the same Type through the registry as through reflection.  The only exceptions are Types which the
    /// compiler generates (closures, iterators etc.), which are not registered: they are found by their exact full name,
    /// like Types of other assemblies, but not by other names; and Types added to an assembly after it was compiled.
    /// </summary>
    [Test]
    public void GetType_FindsTheSameTypeAsReflection_ForEveryNameOfEveryRdmpType()
    {
        var compared = 0;
        var synthesizedNotFound = 0;
        var addedAfterCompilationNotFound = 0;
        var divergences = new List<string>();

        foreach (var type in _rdmpAssemblies.SelectMany(static a => a.GetTypes()))
        {
            foreach (var name in GetNames(type))
            {
                compared++;
                var viaRegistry = MEF.Resolve(_registry, new MEF.ContextCache(), name);
                var viaReflection = MEF.Resolve(_reflection, new MEF.ContextCache(), name);

                if (viaRegistry == viaReflection)
                    continue;

                if (IsSynthesized(type) && viaRegistry == null && name != type.FullName)
                {
                    synthesizedNotFound++;
                    continue;
                }

                // Coverlet's Types have a '.' in their name, so not even Assembly.GetType finds them by their full name
                if (CompiledTypeRegistryChecks.IsAddedAfterCompilation(type) && viaRegistry == null)
                {
                    addedAfterCompilationNotFound++;
                    continue;
                }

                divergences.Add($"'{name}' found {Describe(viaRegistry)} with the registry but {Describe(viaReflection)} with reflection");
            }
        }

        TestContext.Out.WriteLine(
            $"Compared {compared} lookups of the names of {_rdmpAssemblies.Sum(static a => a.GetTypes().Length)} Types: " +
            $"{divergences.Count} differ, {synthesizedNotFound} names of compiler generated Types are only found with reflection, " +
            $"{addedAfterCompilationNotFound} names of Types added after compilation are only found with reflection");
        Assert.That(compared, Is.GreaterThan(20000));
        Assert.That(divergences, Is.Empty);
    }

    /// <summary>
    /// For every Type declared in the RDMP assemblies, the Types MEF lists as implementing or inheriting it are the same
    /// whether they come from the registry or reflection.  Reflection also lists Types of other assemblies, but for an RDMP
    /// Type those can only be dynamically generated ones (e.g. NSubstitute proxies).
    /// </summary>
    [Test]
    public void GetTypes_FindsTheSameTypesAsReflection_ForEveryRdmpType()
    {
        var viaRegistry = GetImplementations(_registry.Types);
        var viaReflection = GetImplementations(_reflection.Types);
        var compared = 0;
        var divergences = new List<string>();

        foreach (var type in _rdmpAssemblies.SelectMany(static a => a.GetTypes()).Where(static t => !IsNotFromCompilation(t)))
        {
            compared++;
            var registry = viaRegistry.GetValueOrDefault(type, new HashSet<Type>());
            var reflection = viaReflection.GetValueOrDefault(type, new HashSet<Type>());
            var reflectionRdmp = reflection.Where(t => IsRdmp(t) && !IsNotFromCompilation(t)).ToHashSet();

            if (!registry.SetEquals(reflectionRdmp))
                divergences.Add(
                    $"{type.FullName}: only with the registry {string.Join(",", registry.Except(reflectionRdmp))}, only with reflection {string.Join(",", reflectionRdmp.Except(registry))}");

            var otherAssemblies = reflection.Where(t => !IsRdmp(t) && !t.Assembly.IsDynamic).ToArray();
            if (otherAssemblies.Length > 0)
                divergences.Add($"{type.FullName}: reflection also finds {string.Join(",", otherAssemblies.Select(static t => t.FullName))}");
        }

        TestContext.Out.WriteLine($"Compared the implementations of {compared} Types: {divergences.Count} differ");
        Assert.That(compared, Is.GreaterThan(2000));
        Assert.That(divergences, Is.Empty);
    }

    [TestCase(typeof(IAtomicCommand))]
    [TestCase(typeof(IAttacher))]
    [TestCase(typeof(DatabaseEntity))]
    [TestCase(typeof(IMapsDirectlyToDatabaseTable))]
    [TestCase(typeof(ICheckable))]
    [TestCase(typeof(ICacheLayout))]
    [TestCase(typeof(IDataFlowComponent<DataTable>))]
    [TestCase(typeof(IDataFlowSource<DataTable>))]
    [TestCase(typeof(IDataFlowDestination<DataTable>))]
    [TestCase(typeof(IPipelineRequirement<DataTable>))]
    public void GetTypes_FindsTheRdmpTypesReflectionFinds(Type type)
    {
        var expected = _reflection.Types.Where(IsRdmp).Where(t => !t.IsInterface && !t.IsAbstract && type.IsAssignableFrom(t))
            .ToArray();

        Assert.That(expected, Is.Not.Empty);
        Assert.That(MEF.GetTypes(type), Is.EquivalentTo(expected));
    }

    /// <summary>
    /// The registry lists non public Types, which it used not to (8 internal commands were missing)
    /// </summary>
    [Test]
    public void GetTypes_FindsNonPublicCommands()
    {
        var nonPublic = typeof(IAtomicCommand).Assembly.GetTypes()
            .Where(static t => !t.IsInterface && !t.IsAbstract && !t.IsVisible && typeof(IAtomicCommand).IsAssignableFrom(t))
            .ToArray();

        Assert.That(nonPublic, Is.Not.Empty);
        Assert.That(MEF.GetTypes<IAtomicCommand>(), Is.SupersetOf(nonPublic));
    }

    [TestCase(typeof(IList<>), typeof(string))]
    [TestCase(typeof(IEnumerable<>), typeof(string))]
    [TestCase(typeof(IDataFlowComponent<>), typeof(DataTable))]
    [TestCase(typeof(IDataFlowSource<>), typeof(DataTable))]
    [TestCase(typeof(IDataFlowDestination<>), typeof(DataTable))]
    [TestCase(typeof(IPipelineRequirement<>), typeof(DataTable))]
    public void GetGenericTypes_FindsTheRdmpTypesReflectionFinds(Type genericType, Type typeOfT)
    {
        var target = genericType.MakeGenericType(typeOfT);
        var expected = _reflection.Types.Where(IsRdmp)
            .Where(t => !t.IsAbstract && !t.IsGenericType && target.IsAssignableFrom(t)).ToArray();

        // Iterators are compiler generated classes implementing IEnumerable<T>, which the registry does not list
        Assert.That(expected, Is.Not.Empty);
        Assert.That(MEF.GetGenericTypes(genericType, typeOfT),
            Is.EquivalentTo(expected.Where(t => !IsNotFromCompilation(t) || _registry.Types.Contains(t))));
    }

    /// <summary>
    /// A private nested Type (the generator lists these with GetNestedType) and a file local Type (listed by metadata name)
    /// are found by <see cref="MEF.GetGenericTypes"/>
    /// </summary>
    [Test]
    public void GetGenericTypes_FindsPrivateNestedTypes() =>
        Assert.That(MEF.GetGenericTypes(typeof(IList<>), typeof(string)),
            Does.Contain(typeof(PrivateStringList)).And.Contain(typeof(FileLocalStringList)));

    /// <summary>
    /// Types of other assemblies are not in the registry but are still found by their (exact) full name, before any Type with
    /// the same short name, as they were when reflection indexed every loaded Type
    /// </summary>
    [Test]
    public void GetType_FindsTheSameTypeAsReflection_ForTheFullNameOfEveryTypeInOtherAssemblies()
    {
        var compared = 0;
        var foundByShortName = 0;
        var divergences = new List<string>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a =>
                     !a.IsDynamic && !_registry.Assemblies.Contains(a) && _reflection.Assemblies.Contains(a)))
        {
            foreach (var type in GetTypesSafely(assembly).Where(static t => t.FullName != null && !IsNotFromCompilation(t)))
            {
                compared++;
                var viaRegistry = MEF.Resolve(_registry, new MEF.ContextCache(), type.FullName);
                var viaReflection = MEF.Resolve(_reflection, new MEF.ContextCache(), type.FullName);

                if (viaRegistry == viaReflection)
                    continue;

                // Reflection's index held short names and full names (ignoring case) together, so a Type of another
                // assembly could be found through its short name (or its name in another case) before one whose full name
                // this is.  The registry finds the Type called exactly that, as short names of other assemblies' Types
                // are not used.
                if (viaRegistry?.FullName == type.FullName && viaReflection != null && !IsRdmp(viaReflection) &&
                    viaReflection.FullName != type.FullName)
                {
                    foundByShortName++;
                    continue;
                }

                divergences.Add($"'{type.FullName}' [{assembly.GetName().Name}] found {Describe(viaRegistry)} with the registry but {Describe(viaReflection)} with reflection");
            }
        }

        TestContext.Out.WriteLine(
            $"Compared {compared} full names: {divergences.Count} differ, {foundByShortName} resolved by reflection to another assembly's Type with that short name, or that name in another case");
        Assert.That(compared, Is.GreaterThan(1000));
        Assert.That(divergences, Is.Empty);
    }

    [Test]
    public void GetType_FindsOtherAssemblies_TypesByFullNameOnly()
    {
        // MEF only finds the Types of other assemblies which are loaded, so load each one first
        var yamlValue = typeof(YamlDotNet.Core.Tokens.Value);
        var dataSet = typeof(DataSet);
        var databaseType = typeof(FAnsi.DatabaseType);
        var nlogLogManager = typeof(NLog.LogManager);
        Assert.That(new[] { yamlValue, dataSet, databaseType, nlogLogManager }.Select(static t => t.Assembly),
            Is.All.Not.Null);

        Assert.Multiple(() =>
        {
            // YamlDotNet.Core.Tokens.Value used to be found for 'Value' (e.g. a word on the command line)
            Assert.That(MEF.GetType("Value"), Is.Null);
            Assert.That(MEF.GetType("YamlDotNet.Core.Tokens.Value"), Is.EqualTo(yamlValue));

            // A full name is looked for before a short name (Rdmp.Core's Dataset has the same short name, ignoring case)
            Assert.That(MEF.GetType("System.Data.DataSet"), Is.EqualTo(dataSet));
            Assert.That(MEF.GetType("FAnsi.DatabaseType"), Is.EqualTo(databaseType));
            Assert.That(MEF.GetType("System.String"), Is.EqualTo(typeof(string)));

            // Finding a Type by its full name does not make its short name find it
            Assert.That(MEF.GetType("NLog.LogManager"), Is.EqualTo(nlogLogManager));
            Assert.That(MEF.GetType("LogManager"), Is.EqualTo(typeof(Rdmp.Core.Logging.LogManager)));
            Assert.That(MEF.GetType("Rdmp.Core.Logging.LogManager"), Is.EqualTo(typeof(Rdmp.Core.Logging.LogManager)));

            // Old namespaces still find RDMP Types by their short name
            Assert.That(MEF.GetType("CatalogueLibrary.Data.Catalogue"), Is.EqualTo(typeof(Catalogue)));
        });
    }

    /// <summary>
    /// Kinds of Type the old registry left out (interfaces, enums, delegates, generic and non public Types) and names it
    /// resolved differently from reflection
    /// </summary>
    [Test]
    public void GetType_FindsEveryKindOfType()
    {
        // An internal Type whose short name is also that of a public .NET Type (loaded here), which the old registry did not
        // list, so a lookup of its full name fell through to the other Type's short name
        var mailAttachment = typeof(System.Net.Mail.Attachment);
        var attachment = typeof(MEF).Assembly.GetType("Rdmp.Core.CommandExecution.AtomicCommands.Attachment", true);
        Assert.That(attachment.IsVisible, Is.False);

        Assert.Multiple(() =>
        {
            Assert.That(MEF.GetType("ICacheLayout"), Is.EqualTo(typeof(ICacheLayout)));
            Assert.That(MEF.GetType("CacheFileGranularity"), Is.EqualTo(typeof(CacheFileGranularity)));
            Assert.That(MEF.GetType("CacheSource`1"), Is.EqualTo(typeof(CacheSource<>)));
            Assert.That(MEF.GetType("EmphasiseItemHandler"), Is.EqualTo(typeof(EmphasiseItemHandler)));
            Assert.That(MEF.GetType(typeof(PrivateStringList).FullName), Is.EqualTo(typeof(PrivateStringList)));
            Assert.That(MEF.GetType("CompiledTypeRegistryTests+PrivateStringList"), Is.EqualTo(typeof(PrivateStringList)));
            Assert.That(MEF.GetType(typeof(ProtectedNested).FullName), Is.EqualTo(typeof(ProtectedNested)));
            Assert.That(MEF.GetType(typeof(PrivateNestedGeneric<>).FullName), Is.EqualTo(typeof(PrivateNestedGeneric<>)));

            Assert.That(MEF.GetType("Rdmp.Core.CommandExecution.AtomicCommands.Attachment"), Is.EqualTo(attachment));
            Assert.That(MEF.GetType("Attachment"), Is.EqualTo(attachment));
            Assert.That(MEF.GetType("System.Net.Mail.Attachment"), Is.EqualTo(mailAttachment));

            // Short names shared with Types of other assemblies: RDMP's wins
            Assert.That(MEF.GetType("DataTableExtensions"),
                Is.EqualTo(typeof(Rdmp.Core.ReusableLibraryCode.Extensions.DataTableExtensions)));
            Assert.That(MEF.GetType("RegularExpression"), Is.EqualTo(typeof(RegularExpression)));
        });
    }

    [Test]
    public void BuildFromCompiledTypeRegistry_RegistrationFails_IsAProblem()
    {
        var assembly = typeof(CompiledTypeRegistryTests).Assembly;
        var registration = new CompiledTypeRegistration(assembly,
            static () => throw new TypeLoadException("Could not load file or assembly 'NotDeployed'"));
        var problems = new List<KeyValuePair<string, Exception>>();

        Assert.Multiple(() =>
        {
            Assert.That(MEF.BuildFromCompiledTypeRegistry(new[] { registration }, new[] { assembly }, problems), Is.Null);
            Assert.That(problems.Select(static p => p.Key), Is.EqualTo(new[] { assembly.FullName }));
            Assert.That(problems.Single().Value, Is.InstanceOf<CompiledTypeRegistryException>());
            Assert.That(problems.Single().Value.Message, Does.Contain("NotDeployed"));
        });
    }

    [Test]
    public void BuildFromCompiledTypeRegistry_RegistrationListsAnotherAssemblysType_IsAProblem()
    {
        var assembly = typeof(CompiledTypeRegistryTests).Assembly;
        var registration = new CompiledTypeRegistration(assembly,
            static () => new[] { typeof(CompiledTypeRegistryTests), typeof(Catalogue) });
        var problems = new List<KeyValuePair<string, Exception>>();

        Assert.Multiple(() =>
        {
            Assert.That(MEF.BuildFromCompiledTypeRegistry(new[] { registration }, new[] { assembly }, problems), Is.Null);
            Assert.That(problems.Single().Value.Message, Does.Contain(typeof(Catalogue).FullName));
        });
    }

    [Test]
    public void BuildFromCompiledTypeRegistry_RdmpAssemblyNotRegistered_IsAProblem()
    {
        var problems = new List<KeyValuePair<string, Exception>>();

        Assert.Multiple(() =>
        {
            Assert.That(MEF.BuildFromCompiledTypeRegistry(Array.Empty<CompiledTypeRegistration>(),
                new[] { typeof(MEF).Assembly, typeof(string).Assembly }, problems), Is.Null);
            Assert.That(problems.Select(static p => p.Key), Is.EqualTo(new[] { typeof(MEF).Assembly.FullName }));
            Assert.That(problems.Single().Value.Message, Does.Contain("has not registered"));
        });
    }

    /// <summary>
    /// Only the registrations of assemblies which are loaded (in the list given) are indexed
    /// </summary>
    [Test]
    public void BuildFromCompiledTypeRegistry_AssemblyNotLoaded_IsNotIndexed()
    {
        var registration = new CompiledTypeRegistration(typeof(CompiledTypeRegistryTests).Assembly,
            static () => new[] { typeof(CompiledTypeRegistryTests) });
        var problems = new List<KeyValuePair<string, Exception>>();

        var index = MEF.BuildFromCompiledTypeRegistry(new[] { registration }, new[] { typeof(string).Assembly }, problems);

        Assert.Multiple(() =>
        {
            Assert.That(problems, Is.Empty);
            Assert.That(index.Assemblies, Is.Empty);
            Assert.That(index.Types, Is.Empty);
        });
    }

    /// <summary>
    /// An RDMP assembly whose module initializer (which registers its Types) fails is a problem, which MEF reports (here, as
    /// the tests require the registry, by throwing) and in production logs before falling back on reflection, rather than an
    /// exception escaping from the building of its index
    /// </summary>
    [Test]
    public void ModuleInitializerFails_IsAProblem()
    {
        var context = new AssemblyLoadContext(nameof(ModuleInitializerFails_IsAProblem), true);

        try
        {
            var assembly = context.LoadFromStream(EmitRdmpAssembly("RegistryTestModuleInitializerFails", true));

            // MEF notices the assembly the next time it is used
            var ex = Assert.Throws<CompiledTypeRegistryException>(() => MEF.GetType("Catalogue"));
            Assert.That(ex.Message, Does.Contain("module initializer").And.Contain("RegistryTestModuleInitializerFails"));

            // How MEF prepares to build its index
            var problems = new List<KeyValuePair<string, Exception>>();
            Assert.DoesNotThrow(() => MEF.EnsureRegistered(new[] { assembly }, problems));

            Assert.Multiple(() =>
            {
                Assert.That(problems.Select(static p => p.Key), Is.EqualTo(new[] { assembly.FullName }));
                Assert.That(problems.Single().Value, Is.InstanceOf<CompiledTypeRegistryException>());
                Assert.That(problems.Single().Value.Message, Does.Contain("Module initializer failed for the test"));
            });
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    /// A problem found before the index is built (e.g. a module initializer which failed) stops the registry being used,
    /// and the assembly it is about is not reported again for not having registered
    /// </summary>
    [Test]
    public void BuildFromCompiledTypeRegistry_ProblemAlreadyFound_IsNotReportedTwice()
    {
        var problems = new List<KeyValuePair<string, Exception>>
        {
            new(typeof(MEF).Assembly.FullName, new CompiledTypeRegistryException("Its module initializer failed"))
        };

        Assert.Multiple(() =>
        {
            Assert.That(MEF.BuildFromCompiledTypeRegistry(Array.Empty<CompiledTypeRegistration>(),
                new[] { typeof(MEF).Assembly }, problems), Is.Null);
            Assert.That(problems.Select(static p => p.Value.Message), Is.EqualTo(new[] { "Its module initializer failed" }));
        });
    }

    /// <summary>
    /// The registry forgets the registration of an assembly in a collectible AssemblyLoadContext when the context unloads,
    /// so that it can be collected
    /// </summary>
    [Test]
    public void Register_CollectibleContext_DoesNotStopItUnloading()
    {
        var context = LoadRegisteringAssemblyAndUnload();

        for (var i = 0; i < 20 && context.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.That(context.IsAlive, Is.False, "Something still refers to the unloaded AssemblyLoadContext");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadRegisteringAssemblyAndUnload()
    {
        var context = new AssemblyLoadContext(nameof(Register_CollectibleContext_DoesNotStopItUnloading), true);
        var assembly = context.LoadFromStream(EmitRdmpAssembly("RegistryTestCollectible", false));

        // MEF finds the assembly's (one) Type for its context
        MEF.GetType("Catalogue");
        Assert.That(CompiledTypeRegistry.GetRegisteredTypes(assembly).Select(static t => t.FullName),
            Is.EqualTo(new[] { "RegistryTest.Marker" }));

        context.Unload();
        Assert.That(CompiledTypeRegistry.GetRegistration(assembly), Is.Null);

        return new WeakReference(context);
    }

    /// <summary>
    /// Emits an assembly like one built with the type registry generator: marked with
    /// <see cref="CompiledTypeRegistryAttribute"/>, with one Type (RegistryTest.Marker) which its module initializer registers,
    /// or with a module initializer which throws
    /// </summary>
    private static MemoryStream EmitRdmpAssembly(string name, bool moduleInitializerFails)
    {
        var assembly = new PersistedAssemblyBuilder(new AssemblyName(name), typeof(object).Assembly);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(CompiledTypeRegistryAttribute).GetConstructor(Type.EmptyTypes)!, Array.Empty<object>()));

        var module = assembly.DefineDynamicModule(name);
        var marker = module.DefineType("RegistryTest.Marker",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class);
        marker.CreateType();

        var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        var getTypes = module.DefineGlobalMethod("GetTypes", MethodAttributes.Public | MethodAttributes.Static,
            typeof(Type[]), Type.EmptyTypes);
        var il = getTypes.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Newarr, typeof(Type));
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldtoken, marker);
        il.Emit(OpCodes.Call, getTypeFromHandle);
        il.Emit(OpCodes.Stelem_Ref);
        il.Emit(OpCodes.Ret);

        var moduleInitializer = module.DefineGlobalMethod(".cctor",
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName, typeof(void), Type.EmptyTypes);
        il = moduleInitializer.GetILGenerator();

        if (moduleInitializerFails)
        {
            il.Emit(OpCodes.Ldstr, "Module initializer failed for the test");
            il.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!);
            il.Emit(OpCodes.Throw);
        }
        else
        {
            // CompiledTypeRegistry.Register(typeof(Marker).Assembly, GetTypes)
            il.Emit(OpCodes.Ldtoken, marker);
            il.Emit(OpCodes.Call, getTypeFromHandle);
            il.Emit(OpCodes.Callvirt, typeof(Type).GetProperty(nameof(Type.Assembly))!.GetMethod!);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, getTypes);
            il.Emit(OpCodes.Newobj, typeof(Func<Type[]>).GetConstructor(new[] { typeof(object), typeof(IntPtr) })!);
            il.Emit(OpCodes.Call, typeof(CompiledTypeRegistry).GetMethod(nameof(CompiledTypeRegistry.Register))!);
            il.Emit(OpCodes.Ret);
        }

        module.CreateGlobalFunctions();

        var stream = new MemoryStream();
        assembly.Save(stream);
        stream.Position = 0;
        return stream;
    }

    [Test]
    public void ReportProblems_RegistryRequired_Throws()
    {
        var failures = new Dictionary<string, Exception>();
        var warnings = new List<string>();
        var problem = new KeyValuePair<string, Exception>("SomeAssembly", new CompiledTypeRegistryException("It went wrong"));

        var ex = Assert.Throws<CompiledTypeRegistryException>(() =>
            MEF.ReportProblems(new[] { problem }, "MEF cannot use the registry", true, failures, warnings.Add));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain("It went wrong").And.Contain(MEF.RequireCompiledTypeRegistrySwitch));
            Assert.That(failures, Is.Empty);
            Assert.That(warnings, Is.Empty);
        });
    }

    [Test]
    public void ReportProblems_RegistryNotRequired_WarnsAndRecords()
    {
        var failures = new Dictionary<string, Exception>();
        var warnings = new List<string>();
        var problem = new KeyValuePair<string, Exception>("SomeAssembly", new CompiledTypeRegistryException("It went wrong"));

        MEF.ReportProblems(new[] { problem }, "MEF cannot use the registry", false, failures, warnings.Add);

        Assert.Multiple(() =>
        {
            Assert.That(failures.Keys, Is.EqualTo(new[] { "SomeAssembly" }));
            Assert.That(warnings.Single(), Does.StartWith("MEF cannot use the registry").And.Contain("It went wrong"));
        });
    }

    [Test]
    public void TestsRequireTheCompiledTypeRegistry() =>
        Assert.That(AppContext.TryGetSwitch(MEF.RequireCompiledTypeRegistrySwitch, out var required) && required,
            "The test projects should set the switch (see Directory.Build.props) so that registry problems fail tests");

    private static bool IsSynthesized(Type type) => CompiledTypeRegistryChecks.IsSynthesized(type);

    private static bool IsNotFromCompilation(Type type) =>
        CompiledTypeRegistryChecks.IsSynthesized(type) || CompiledTypeRegistryChecks.IsAddedAfterCompilation(type);

    private bool IsRdmp(Type type) => _registry.Assemblies.Contains(type.Assembly);

    private static IEnumerable<string> GetNames(Type type)
    {
        var fullName = type.FullName;
        if (fullName == null)
            yield break;

        var off = fullName.LastIndexOf('.') + 1;
        var shortName = off == 0 ? fullName : fullName[off..];

        foreach (var name in new[]
                 {
                     fullName, shortName, fullName.ToUpperInvariant(), shortName.ToUpperInvariant(),
                     fullName.ToLowerInvariant(), shortName.ToLowerInvariant()
                 }.Distinct())
            yield return name;
    }

    /// <summary>
    /// Returns, for every Type, the Types listed by <paramref name="types"/> which <see cref="MEF.GetTypes{T}"/> would return
    /// for it (concrete Types assignable to it)
    /// </summary>
    private static Dictionary<Type, HashSet<Type>> GetImplementations(IEnumerable<Type> types)
    {
        var implementations = new Dictionary<Type, HashSet<Type>>();

        foreach (var type in types.Where(static t => !t.IsInterface && !t.IsAbstract))
        {
            for (var t = type; t != null; t = t.BaseType)
                Add(t);

            foreach (var i in type.GetInterfaces())
                Add(i);

            void Add(Type implemented)
            {
                if (!implementations.TryGetValue(implemented, out var set))
                    implementations.Add(implemented, set = new HashSet<Type>());

                set.Add(type);
            }
        }

        return implementations;
    }

    private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(static t => t != null);
        }
    }

    private static string Describe(Type type) =>
        type == null ? "nothing" : $"'{type.FullName}' [{type.Assembly.GetName().Name}]";

    private sealed class PrivateStringList : List<string>
    {
    }

    protected sealed class ProtectedNested
    {
    }

    private sealed class PrivateNestedGeneric<T>
    {
        public T Value { get; set; }
    }
}

/// <summary>
/// A file local Type, which the type registry generator can only list by its metadata name
/// </summary>
file sealed class FileLocalStringList : List<string>
{
}
