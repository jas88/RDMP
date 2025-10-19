// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rdmp.Core.CommandLine.DatabaseCreation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.MapsDirectlyToDatabaseTable;

namespace Tests.Common.Performance;

/// <summary>
/// Utility class for measuring and optimizing test performance.
/// Provides benchmarking capabilities and performance monitoring for the test optimization components.
/// </summary>
public static class TestPerformanceOptimizer
{
    private static readonly Dictionary<string, Stopwatch> _timers = new();
    private static readonly Dictionary<string, List<TimeSpan>> _measurements = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Starts timing an operation with the specified name.
    /// </summary>
    /// <param name="operationName">Name of the operation to time</param>
    public static void StartTiming(string operationName)
    {
        lock (_lock)
        {
            if (_timers.ContainsKey(operationName))
            {
                _timers[operationName].Restart();
            }
            else
            {
                _timers[operationName] = Stopwatch.StartNew();
            }
        }
    }

    /// <summary>
    /// Stops timing an operation and records the measurement.
    /// </summary>
    /// <param name="operationName">Name of the operation to stop timing</param>
    /// <returns>The elapsed time for the operation</returns>
    public static TimeSpan StopTiming(string operationName)
    {
        lock (_lock)
        {
            if (!_timers.TryGetValue(operationName, out var timer))
            {
                throw new ArgumentException($"No timer found for operation: {operationName}");
            }

            timer.Stop();
            var elapsed = timer.Elapsed;

            if (!_measurements.ContainsKey(operationName))
            {
                _measurements[operationName] = new List<TimeSpan>();
            }

            _measurements[operationName].Add(elapsed);
            return elapsed;
        }
    }

    /// <summary>
    /// Measures the execution time of an operation.
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="operation">The operation to measure</param>
    /// <returns>The result of the operation</returns>
    public static T MeasureOperation<T>(string operationName, Func<T> operation)
    {
        StartTiming(operationName);
        try
        {
            var result = operation();
            StopTiming(operationName);
            return result;
        }
        catch
        {
            // Don't record failed operations
            _timers.Remove(operationName);
            throw;
        }
    }

    /// <summary>
    /// Measures the execution time of an operation that doesn't return a value.
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="operation">The operation to measure</param>
    public static void MeasureOperation(string operationName, Action operation)
    {
        StartTiming(operationName);
        try
        {
            operation();
            StopTiming(operationName);
        }
        catch
        {
            // Don't record failed operations
            _timers.Remove(operationName);
            throw;
        }
    }

    /// <summary>
    /// Gets performance statistics for all measured operations.
    /// </summary>
    /// <returns>Dictionary containing performance statistics</returns>
    public static Dictionary<string, PerformanceStats> GetPerformanceStats()
    {
        lock (_lock)
        {
            var stats = new Dictionary<string, PerformanceStats>();

            foreach (var kvp in _measurements)
            {
                var operationName = kvp.Key;
                var measurements = kvp.Value;

                if (measurements.Count == 0) continue;

                var statsForOperation = new PerformanceStats
                {
                    OperationName = operationName,
                    Count = measurements.Count,
                    TotalTime = TimeSpan.FromTicks(measurements.Sum(m => m.Ticks)),
                    AverageTime = TimeSpan.FromTicks((long)measurements.Average(m => m.Ticks)),
                    MinTime = measurements.Min(),
                    MaxTime = measurements.Max()
                };

                stats[operationName] = statsForOperation;
            }

            return stats;
        }
    }

    /// <summary>
    /// Clears all timing measurements.
    /// </summary>
    public static void ClearMeasurements()
    {
        lock (_lock)
        {
            _timers.Clear();
            _measurements.Clear();
        }
    }

    /// <summary>
    /// Benchmarks repository creation with and without pooling.
    /// </summary>
    /// <param name="options">Database creation options</param>
    /// <param name="iterations">Number of iterations to test</param>
    /// <returns>Comparison results</returns>
    public static RepositoryBenchmark BenchmarkRepositoryCreation(PlatformDatabaseCreationOptions options, int iterations = 10)
    {
        var pooledTimes = new List<TimeSpan>();
        var nonPooledTimes = new List<TimeSpan>();

        // Benchmark with pooling
        for (int i = 0; i < iterations; i++)
        {
            StartTiming($"Repository_Pooled_{i}");
            var repository = RepositoryPool.Instance.GetOrCreateRepository(options, false);
            RepositoryPool.Instance.ReleaseRepository(repository);
            var pooledTime = StopTiming($"Repository_Pooled_{i}");
            pooledTimes.Add(pooledTime);
        }

        // Benchmark without pooling
        for (int i = 0; i < iterations; i++)
        {
            StartTiming($"Repository_NonPooled_{i}");
            var repository = new PlatformDatabaseCreationRepositoryFinder(options);
            if (repository is IDisposable disposable)
                disposable.Dispose();
            var nonPooledTime = StopTiming($"Repository_NonPooled_{i}");
            nonPooledTimes.Add(nonPooledTime);
        }

        return new RepositoryBenchmark
        {
            PooledAverage = TimeSpan.FromTicks((long)pooledTimes.Average(t => t.Ticks)),
            NonPooledAverage = TimeSpan.FromTicks((long)nonPooledTimes.Average(t => t.Ticks)),
            ImprovementPercent = (1.0 - (pooledTimes.Average(t => t.Ticks) / nonPooledTimes.Average(t => t.Ticks))) * 100,
            Iterations = iterations
        };
    }

    /// <summary>
    /// Benchmarks object creation with and without caching.
    /// </summary>
    /// <typeparam name="T">Type of object to create</typeparam>
    /// <param name="repository">Repository to use for object creation</param>
    /// <param name="iterations">Number of iterations to test</param>
    /// <returns>Comparison results</returns>
    public static ObjectCreationBenchmark BenchmarkObjectCreation<T>(MemoryDataExportRepository repository, int iterations = 100) where T : DatabaseEntity
    {
        var cachedTimes = new List<TimeSpan>();
        var nonCachedTimes = new List<TimeSpan>();

        // Clear cache before benchmarking
        TestObjectCache.Instance.ClearCache();

        // Benchmark with caching
        for (int i = 0; i < iterations; i++)
        {
            StartTiming($"Object_Cached_{typeof(T).Name}_{i}");
            var obj = TestObjectCache.Instance.GetOrCreate<T>(repository);
            TestObjectCache.Instance.ReturnToCache(obj);
            var cachedTime = StopTiming($"Object_Cached_{typeof(T).Name}_{i}");
            cachedTimes.Add(cachedTime);
        }

        // Benchmark without caching
        for (int i = 0; i < iterations; i++)
        {
            StartTiming($"Object_NonCached_{typeof(T).Name}_{i}");
            var obj = UnitTests.WhenIHaveA<T>(repository);
            var nonCachedTime = StopTiming($"Object_NonCached_{typeof(T).Name}_{i}");
            nonCachedTimes.Add(nonCachedTime);
        }

        return new ObjectCreationBenchmark
        {
            ObjectType = typeof(T).Name,
            CachedAverage = TimeSpan.FromTicks((long)cachedTimes.Average(t => t.Ticks)),
            NonCachedAverage = TimeSpan.FromTicks((long)nonCachedTimes.Average(t => t.Ticks)),
            ImprovementPercent = (1.0 - (cachedTimes.Average(t => t.Ticks) / nonCachedTimes.Average(t => t.Ticks))) * 100,
            Iterations = iterations
        };
    }

    /// <summary>
    /// Prints a performance report to the test context.
    /// </summary>
    public static void PrintPerformanceReport()
    {
        var stats = GetPerformanceStats();
        var repoStats = RepositoryPool.Instance.GetStatistics();
        var cacheStats = TestObjectCache.Instance.GetStatistics();

        TestContext.Out.WriteLine("=== Test Performance Report ===");
        TestContext.Out.WriteLine();

        TestContext.Out.WriteLine("Repository Pool Statistics:");
        TestContext.Out.WriteLine($"  Total Repositories: {repoStats["TotalRepositories"]}");
        TestContext.Out.WriteLine($"  Active Repositories: {repoStats["ActiveRepositories"]}");
        TestContext.Out.WriteLine($"  Disposed Repositories: {repoStats["DisposedRepositories"]}");
        TestContext.Out.WriteLine($"  Total References: {repoStats["TotalReferences"]}");
        TestContext.Out.WriteLine();

        TestContext.Out.WriteLine("Object Cache Statistics:");
        TestContext.Out.WriteLine($"  Cache Hits: {cacheStats["CacheHits"]}");
        TestContext.Out.WriteLine($"  Cache Misses: {cacheStats["CacheMisses"]}");
        TestContext.Out.WriteLine($"  Hit Rate: {cacheStats["HitRate"]:P2}");
        TestContext.Out.WriteLine($"  Total Objects Cached: {cacheStats["TotalObjectsCached"]}");
        TestContext.Out.WriteLine();

        TestContext.Out.WriteLine("Operation Performance:");
        foreach (var kvp in stats.OrderByDescending(x => x.Value.TotalTime))
        {
            var stat = kvp.Value;
            TestContext.Out.WriteLine($"  {stat.OperationName}:");
            TestContext.Out.WriteLine($"    Count: {stat.Count}");
            TestContext.Out.WriteLine($"    Total: {stat.TotalTime.TotalMilliseconds:F2}ms");
            TestContext.Out.WriteLine($"    Average: {stat.AverageTime.TotalMilliseconds:F2}ms");
            TestContext.Out.WriteLine($"    Min: {stat.MinTime.TotalMilliseconds:F2}ms");
            TestContext.Out.WriteLine($"    Max: {stat.MaxTime.TotalMilliseconds:F2}ms");
            TestContext.Out.WriteLine();
        }

        TestContext.Out.WriteLine("=== End Performance Report ===");
    }

    /// <summary>
    /// Represents performance statistics for an operation.
    /// </summary>
    public class PerformanceStats
    {
        public string OperationName { get; set; }
        public int Count { get; set; }
        public TimeSpan TotalTime { get; set; }
        public TimeSpan AverageTime { get; set; }
        public TimeSpan MinTime { get; set; }
        public TimeSpan MaxTime { get; set; }
    }

    /// <summary>
    /// Represents repository creation benchmark results.
    /// </summary>
    public class RepositoryBenchmark
    {
        public TimeSpan PooledAverage { get; set; }
        public TimeSpan NonPooledAverage { get; set; }
        public double ImprovementPercent { get; set; }
        public int Iterations { get; set; }

        public override string ToString()
        {
            return $"Repository Creation Benchmark ({Iterations} iterations):\n" +
                   $"  Pooled Average: {PooledAverage.TotalMilliseconds:F2}ms\n" +
                   $"  Non-Pooled Average: {NonPooledAverage.TotalMilliseconds:F2}ms\n" +
                   $"  Improvement: {ImprovementPercent:F1}%";
        }
    }

    /// <summary>
    /// Represents object creation benchmark results.
    /// </summary>
    public class ObjectCreationBenchmark
    {
        public string ObjectType { get; set; }
        public TimeSpan CachedAverage { get; set; }
        public TimeSpan NonCachedAverage { get; set; }
        public double ImprovementPercent { get; set; }
        public int Iterations { get; set; }

        public override string ToString()
        {
            return $"Object Creation Benchmark ({ObjectType} - {Iterations} iterations):\n" +
                   $"  Cached Average: {CachedAverage.TotalMilliseconds:F2}ms\n" +
                   $"  Non-Cached Average: {NonCachedAverage.TotalMilliseconds:F2}ms\n" +
                   $"  Improvement: {ImprovementPercent:F1}%";
        }
    }
}

/// <summary>
/// Attribute to automatically measure test method execution time.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MeasurePerformanceAttribute : Attribute
{
    public string OperationName { get; }

    public MeasurePerformanceAttribute(string operationName = null)
    {
        OperationName = operationName;
    }
}

/// <summary>
/// Base class for performance-related tests with automatic measurement.
/// </summary>
public abstract class PerformanceTestBase
{
    [SetUp]
    public virtual void SetUp()
    {
        TestPerformanceOptimizer.ClearMeasurements();
    }

    [TearDown]
    public virtual void TearDown()
    {
        TestPerformanceOptimizer.PrintPerformanceReport();
    }

    protected T MeasureOperation<T>(string operationName, Func<T> operation)
    {
        return TestPerformanceOptimizer.MeasureOperation(operationName, operation);
    }

    protected void MeasureOperation(string operationName, Action operation)
    {
        TestPerformanceOptimizer.MeasureOperation(operationName, operation);
    }
}