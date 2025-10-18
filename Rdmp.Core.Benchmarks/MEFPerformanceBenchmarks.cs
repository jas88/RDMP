// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Rdmp.Core.Repositories;
using Rdmp.Core.Repositories.Construction;

namespace Rdmp.Core.Benchmarks;

/// <summary>
/// Comprehensive performance benchmarks for MEF optimization system
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MEFPerformanceBenchmarks
{
    private readonly string[] _commonTypeNames = {
        "Rdmp.Core.Repositories.MEF",
        "Rdmp.Core.Repositories.Construction.ObjectConstructor",
        "Rdmp.Core.Curation.Data.Catalogue",
        "Rdmp.Core.Curation.Data.TableInfo",
        "Rdmp.Core.DataLoad.Engine.DataFlowPipeline",
        "System.String",
        "System.Collections.Generic.List`1"
    };

    private readonly Type[] _commonTypes = {
        typeof(MEF),
        typeof(ObjectConstructor),
        typeof(string),
        typeof(List<string>),
        typeof(int)
    };

    private readonly object[] _constructorParameters = {
        "test parameter",
        42,
        true,
        DateTime.Now
    };

    [Benchmark(Baseline = true)]
    public Type GetType_LookupSingle()
    {
        return MEF.GetType("Rdmp.Core.Repositories.MEF");
    }

    [Benchmark]
    public Type GetType_LookupMultiple()
    {
        Type result = null;
        foreach (var typeName in _commonTypeNames)
        {
            result = MEF.GetType(typeName);
        }
        return result;
    }

    [Benchmark]
    public Type GetType_CaseInsensitiveLookup()
    {
        return MEF.GetType("rdmp.core.repositories.mef");
    }

    [Benchmark]
    public Type GetType_TailLookup()
    {
        return MEF.GetType("MEF");
    }

    [Benchmark]
    public IEnumerable<Type> GetTypes_GenericLookup()
    {
        return MEF.GetTypes<UseWithObjectConstructorAttribute>();
    }

    [Benchmark]
    public IEnumerable<Type> GetGenericTypes_GenericLookup()
    {
        return MEF.GetGenericTypes(typeof(IEnumerable<>), typeof(string));
    }

    [Benchmark]
    public Type[] GetAllTypes()
    {
        return MEF.GetAllTypes().ToArray();
    }

    [Benchmark]
    public object CreateA_SingleConstruction()
    {
        return MEF.CreateA<UseWithObjectConstructorAttribute>(
            "Rdmp.Core.Repositories.Construction.UseWithObjectConstructorAttribute");
    }

    [Benchmark]
    public object CreateA_MultipleConstructions()
    {
        object result = null;
        for (int i = 0; i < 10; i++)
        {
            result = MEF.CreateA<UseWithObjectConstructorAttribute>(
                "Rdmp.Core.Repositories.Construction.UseWithObjectConstructorAttribute");
        }
        return result;
    }

    [Benchmark]
    public string GetCSharpNameForType_SimpleType()
    {
        return MEF.GetCSharpNameForType(typeof(string));
    }

    [Benchmark]
    public string GetCSharpNameForType_GenericType()
    {
        return MEF.GetCSharpNameForType(typeof(List<string>));
    }

    [Benchmark]
    public object ObjectConstructor_BlankConstructor()
    {
        return ObjectConstructor.Construct(typeof(BenchmarkTestClass));
    }

    [Benchmark]
    public object ObjectConstructor_ParameterConstructor()
    {
        return ObjectConstructor.Construct(typeof(BenchmarkTestClass), "benchmark parameter");
    }

    [Benchmark]
    public object ObjectConstructor_ComplexConstructor()
    {
        return ObjectConstructor.ConstructIfPossible(typeof(ComplexBenchmarkTestClass), _constructorParameters);
    }

    [Benchmark]
    public Dictionary<System.Reflection.ConstructorInfo, List<object>> ObjectConstructor_GetConstructors()
    {
        return ObjectConstructor.GetConstructors(typeof(ComplexBenchmarkTestClass), true, false, _constructorParameters);
    }

    [Benchmark]
    public System.Reflection.ConstructorInfo ObjectConstructor_GetRepositoryConstructor()
    {
        return ObjectConstructor.GetRepositoryConstructor(typeof(BenchmarkRepositoryTestClass));
    }

    [Benchmark]
    public void WarmupOperations()
    {
        // Simulate typical warmup operations
        var allTypes = MEF.GetAllTypes();
        var someTypes = MEF.GetTypes<UseWithObjectConstructorAttribute>();
        var singleType = MEF.GetType("Rdmp.Core.Repositories.MEF");
    }

    [Benchmark]
    public void StartupSimulation()
    {
        // Simulate application startup operations
        var allTypes = MEF.GetAllTypes();
        var mefTypes = allTypes.Where(t => t.FullName?.StartsWith("Rdmp.Core") == true).Take(10);

        foreach (var type in mefTypes)
        {
            var csharpName = MEF.GetCSharpNameForType(type);
        }
    }

    [Benchmark]
    public void ConcurrentTypeLookups()
    {
        // Simulate concurrent type lookup scenarios
        var tasks = new List<System.Threading.Tasks.Task<Type>>();
        for (int i = 0; i < 5; i++)
        {
            var typeName = _commonTypeNames[i % _commonTypeNames.Length];
            tasks.Add(System.Threading.Tasks.Task.Run(() => MEF.GetType(typeName)));
        }

        System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
    }

    [Benchmark]
    public void MemoryAllocationPattern()
    {
        // Simulate memory allocation patterns during object construction
        var instances = new List<object>();
        for (int i = 0; i < 100; i++)
        {
            var instance = ObjectConstructor.ConstructIfPossible(
                typeof(BenchmarkTestClass), "parameter", i);
            if (instance != null)
            {
                instances.Add(instance);
            }
        }
    }

    [Benchmark]
    public void TypeResolutionChain()
    {
        // Simulate complex type resolution scenarios
        var baseType = typeof(UseWithObjectConstructorAttribute);
        var derivedTypes = MEF.GetTypes<UseWithObjectConstructorAttribute>();
        var genericTypes = MEF.GetGenericTypes(typeof(IEnumerable<>), typeof(object));

        var typeCount = derivedTypes.Count() + genericTypes.Count();
    }

    [Benchmark]
    public void ErrorHandlingOverhead()
    {
        // Simulate error handling overhead with invalid lookups
        var validResult = MEF.GetType("Rdmp.Core.Repositories.MEF");
        var invalidResult = MEF.GetType("NonExistent.Type.Name");

        try
        {
            MEF.CreateA<UseWithObjectConstructorAttribute>("NonExistent.Type");
        }
        catch
        {
            // Expected exception
        }
    }

    #region Benchmark Helper Classes

    public class BenchmarkTestClass
    {
        public string Parameter { get; }

        public BenchmarkTestClass()
        {
            Parameter = "default";
        }

        public BenchmarkTestClass(string parameter)
        {
            Parameter = parameter;
        }

        public BenchmarkTestClass(string parameter, int number)
        {
            Parameter = $"{parameter}_{number}";
        }
    }

    public class ComplexBenchmarkTestClass
    {
        public string StringValue { get; }
        public int IntValue { get; }
        public bool BoolValue { get; }
        public DateTime DateTimeValue { get; }

        public ComplexBenchmarkTestClass(string stringValue, int intValue, bool boolValue, DateTime dateTimeValue)
        {
            StringValue = stringValue;
            IntValue = intValue;
            BoolValue = boolValue;
            DateTimeValue = dateTimeValue;
        }
    }

    public class BenchmarkRepositoryTestClass
    {
        public BenchmarkRepositoryTestClass(ICatalogueRepository repository)
        {
            Repository = repository;
        }

        public ICatalogueRepository Repository { get; }
    }

    #endregion
}

/// <summary>
/// Memory usage analysis benchmarks
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MemoryUsageBenchmarks
{
    private readonly string[] _typeNames = {
        "Rdmp.Core.Repositories.MEF",
        "Rdmp.Core.Repositories.Construction.ObjectConstructor",
        "Rdmp.Core.Curation.Data.Catalogue",
        "System.String",
        "System.Collections.Generic.List`1",
        "System.Linq.Enumerable"
    };

    [Benchmark]
    public void TypeLookupMemoryUsage()
    {
        for (int i = 0; i < 1000; i++)
        {
            var typeName = _typeNames[i % _typeNames.Length];
            var type = MEF.GetType(typeName);
        }
    }

    [Benchmark]
    public void ObjectConstructionMemoryUsage()
    {
        for (int i = 0; i < 100; i++)
        {
            var instance = ObjectConstructor.Construct(typeof(BenchmarkTestClass), $"parameter_{i}");
        }
    }

    [Benchmark]
    public void TypeEnumerationMemoryUsage()
    {
        for (int i = 0; i < 10; i++)
        {
            var allTypes = MEF.GetAllTypes();
            var filteredTypes = allTypes.Where(t => t.FullName?.Contains("Rdmp") == true).Take(100);
        }
    }
}

/// <summary>
/// Startup time performance benchmarks
/// </summary>
[SimpleJob]
public class StartupTimeBenchmarks
{
    [Benchmark]
    public void ColdStartTypeResolution()
    {
        // Simulate cold start scenario
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Force a fresh type resolution
        var assemblyLoadHandler = typeof(MEF).GetMethod("Flush",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        assemblyLoadHandler?.Invoke(null, new object[] { null, null });

        var allTypes = MEF.GetAllTypes();
        var commonType = MEF.GetType("Rdmp.Core.Repositories.MEF");

        sw.Stop();
    }

    [Benchmark]
    public void WarmStartTypeResolution()
    {
        // Simulate warm start scenario (types already cached)
        var allTypes = MEF.GetAllTypes();
        var commonType = MEF.GetType("Rdmp.Core.Repositories.MEF");
        var genericTypes = MEF.GetGenericTypes(typeof(IEnumerable<>), typeof(string));
    }

    [Benchmark]
    public void ApplicationStartupSimulation()
    {
        // Simulate full application startup sequence
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Stage 1: Initialize type system
        var allTypes = MEF.GetAllTypes();

        // Stage 2: Resolve common types
        var coreTypes = new[]
        {
            "Rdmp.Core.Repositories.MEF",
            "Rdmp.Core.Repositories.Construction.ObjectConstructor",
            "Rdmp.Core.Curation.Data.Catalogue"
        };

        foreach (var typeName in coreTypes)
        {
            var type = MEF.GetType(typeName);
        }

        // Stage 3: Initialize object construction
        var constructorTest = ObjectConstructor.Construct(typeof(BenchmarkTestClass));

        // Stage 4: Warm up generic type resolution
        var genericTypes = MEF.GetGenericTypes(typeof(IEnumerable<>), typeof(string));

        sw.Stop();
    }
}

/// <summary>
/// Main program entry point for running benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("RDMP Core Performance Benchmarks");
        Console.WriteLine("===============================");
        Console.WriteLine();

        Console.WriteLine("Running MEF Performance Benchmarks...");
        BenchmarkRunner.Run<MEFPerformanceBenchmarks>();

        Console.WriteLine("\nRunning Memory Usage Benchmarks...");
        BenchmarkRunner.Run<MemoryUsageBenchmarks>();

        Console.WriteLine("\nRunning Startup Time Benchmarks...");
        BenchmarkRunner.Run<StartupTimeBenchmarks>();

        Console.WriteLine("\nAll benchmarks completed!");
        Console.WriteLine("Check the results in the generated reports.");
    }
}