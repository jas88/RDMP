// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Rdmp.Core.Repositories.Construction;

/// <summary>
/// Performance benchmarking and validation tools for AOT object factories
/// </summary>
public static class AotObjectFactoryBenchmark
{
    /// <summary>
    /// Benchmark configuration options
    /// </summary>
    public class BenchmarkConfig
    {
        /// <summary>
        /// Number of warmup iterations (not counted in results)
        /// </summary>
        public int WarmupIterations { get; set; } = 1000;

        /// <summary>
        /// Number of benchmark iterations
        /// </summary>
        public int BenchmarkIterations { get; set; } = 10000;

        /// <summary>
        /// Whether to run parallel benchmarks
        /// </summary>
        public bool RunParallel { get; set; } = true;

        /// <summary>
        /// Whether to validate outputs are identical
        /// </summary>
        public bool ValidateOutputs { get; set; } = true;

        /// <summary>
        /// Whether to collect memory statistics
        /// </summary>
        public bool CollectMemoryStats { get; set; } = true;

        /// <summary>
        /// Types to include in benchmark
        /// </summary>
        public List<Type> IncludeTypes { get; set; } = new();

        /// <summary>
        /// Types to exclude from benchmark
        /// </summary>
        public List<Type> ExcludeTypes { get; set; } = new();

        /// <summary>
        /// Namespaces to include (null for all)
        /// </summary>
        public List<string> IncludeNamespaces { get; set; } = new();
    }

    /// <summary>
    /// Benchmark results for a single type
    /// </summary>
    public class TypeBenchmarkResult
    {
        public Type Type { get; set; }
        public ConstructorInfo Constructor { get; set; }
        public object[] TestParameters { get; set; }

        // Timing results
        public TimeSpan ReflectionWarmupTime { get; set; }
        public TimeSpan AotWarmupTime { get; set; }
        public TimeSpan ReflectionBenchmarkTime { get; set; }
        public TimeSpan AotBenchmarkTime { get; set; }
        public int IterationCount { get; set; }

        // Memory results
        public long ReflectionMemoryBefore { get; set; }
        public long ReflectionMemoryAfter { get; set; }
        public long AotMemoryBefore { get; set; }
        public long AotMemoryAfter { get; set; }

        // Calculated metrics
        public double SpeedupRatio { get; set; }
        public double ReflectionAvgTimePerCall { get; set; }
        public double AotAvgTimePerCall { get; set; }
        public double ReflectionMemoryDelta => ReflectionMemoryAfter - ReflectionMemoryBefore;
        public double AotMemoryDelta => AotMemoryAfter - AotMemoryBefore;

        // Validation
        public bool ValidationPassed { get; set; }
        public string ErrorMessage { get; set; }

        public double CallsPerSecondReflection => ReflectionBenchmarkTime.TotalSeconds > 0
            ? IterationCount / ReflectionBenchmarkTime.TotalSeconds : 0;

        public double CallsPerSecondAot => AotBenchmarkTime.TotalSeconds > 0
            ? IterationCount / AotBenchmarkTime.TotalSeconds : 0;
    }

    /// <summary>
    /// Overall benchmark results
    /// </summary>
    public class BenchmarkReport
    {
        public DateTime RunDate { get; set; } = DateTime.UtcNow;
        public BenchmarkConfig Config { get; set; }
        public List<TypeBenchmarkResult> TypeResults { get; set; } = new();
        public TimeSpan TotalBenchmarkTime { get; set; }

        // Summary statistics
        public int TotalTypesBenchmarked => TypeResults.Count;
        public int SuccessfulBenchmarks => TypeResults.Count(r => r.ErrorMessage == null);
        public int FailedBenchmarks => TypeResults.Count(r => r.ErrorMessage != null);
        public double AverageSpeedupRatio => TypeResults.Where(r => r.ErrorMessage == null).Average(r => r.SpeedupRatio);
        public double MaxSpeedupRatio => TypeResults.Where(r => r.ErrorMessage == null).Max(r => r.SpeedupRatio);
        public double MinSpeedupRatio => TypeResults.Where(r => r.ErrorMessage == null).Min(r => r.SpeedupRatio);

        // Performance totals
        public double TotalReflectionCallsPerSecond => TypeResults.Where(r => r.ErrorMessage == null).Sum(r => r.CallsPerSecondReflection);
        public double TotalAotCallsPerSecond => TypeResults.Where(r => r.ErrorMessage == null).Sum(r => r.CallsPerSecondAot);

        // Memory statistics
        public double AverageReflectionMemoryDelta => TypeResults.Where(r => r.ErrorMessage == null).Average(r => r.ReflectionMemoryDelta);
        public double AverageAotMemoryDelta => TypeResults.Where(r => r.ErrorMessage == null).Average(r => r.AotMemoryDelta);

        /// <summary>
        /// Gets the types that benefit most from AOT
        /// </summary>
        public List<TypeBenchmarkResult> TopPerformers => TypeResults
            .Where(r => r.ErrorMessage == null)
            .OrderByDescending(r => r.SpeedupRatio)
            .Take(10)
            .ToList();

        /// <summary>
        /// Gets types that don't benefit from AOT or have issues
        /// </summary>
        public List<TypeBenchmarkResult> ProblematicTypes => TypeResults
            .Where(r => r.ErrorMessage != null || r.SpeedupRatio < 1.0)
            .OrderBy(r => r.SpeedupRatio)
            .ToList();
    }

    /// <summary>
    /// Runs comprehensive benchmarking of AOT vs reflection object construction
    /// </summary>
    /// <param name="assembly">Assembly to benchmark</param>
    /// <param name="config">Benchmark configuration</param>
    /// <returns>Benchmark report</returns>
    public static async Task<BenchmarkReport> RunBenchmark(Assembly assembly, BenchmarkConfig config = null)
    {
        config ??= new BenchmarkConfig();
        var report = new BenchmarkReport { Config = config };

        // Initialize AOT factories
        AotObjectFactoryRegistry.Initialize();

        var stopwatch = Stopwatch.StartNew();

        // Get types to benchmark
        var types = GetTypesForBenchmark(assembly, config);

        if (config.RunParallel)
        {
            // Run benchmarks in parallel
            var tasks = types.Select(type => Task.Run(() => BenchmarkType(type, config)));
            var results = await Task.WhenAll(tasks);
            report.TypeResults.AddRange(results.Where(r => r != null));
        }
        else
        {
            // Run benchmarks sequentially
            foreach (var type in types)
            {
                var result = BenchmarkType(type, config);
                if (result != null)
                    report.TypeResults.Add(result);
            }
        }

        stopwatch.Stop();
        report.TotalBenchmarkTime = stopwatch.Elapsed;

        return report;
    }

    /// <summary>
    /// Benchmarks a single type
    /// </summary>
    /// <param name="type">Type to benchmark</param>
    /// <param name="config">Benchmark configuration</param>
    /// <returns>Benchmark result for the type</returns>
    public static TypeBenchmarkResult BenchmarkType(Type type, BenchmarkConfig config = null)
    {
        config ??= new BenchmarkConfig();
        var result = new TypeBenchmarkResult { Type = type };

        try
        {
            // Find a suitable constructor for testing
            result.Constructor = FindBenchmarkableConstructor(type);
            if (result.Constructor == null)
            {
                result.ErrorMessage = "No benchmarkable constructor found";
                return result;
            }

            result.TestParameters = CreateTestParameters(result.Constructor);

            // Warmup
            result.ReflectionWarmupTime = MeasureTime(() =>
            {
                for (int i = 0; i < config.WarmupIterations; i++)
                {
                    ObjectConstructor.ConstructIfPossible(type, result.TestParameters);
                }
            });

            result.AotWarmupTime = MeasureTime(() =>
            {
                for (int i = 0; i < config.WarmupIterations; i++)
                {
                    AotObjectConstructor.ConstructIfPossible(type, result.TestParameters);
                }
            });

            // Memory collection before benchmark
            if (config.CollectMemoryStats)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            // Reflection benchmark
            if (config.CollectMemoryStats)
                result.ReflectionMemoryBefore = GC.GetTotalMemory(false);

            result.ReflectionBenchmarkTime = MeasureTime(() =>
            {
                for (int i = 0; i < config.BenchmarkIterations; i++)
                {
                    ObjectConstructor.ConstructIfPossible(type, result.TestParameters);
                }
            });

            if (config.CollectMemoryStats)
                result.ReflectionMemoryAfter = GC.GetTotalMemory(false);

            // AOT benchmark
            if (config.CollectMemoryStats)
                result.AotMemoryBefore = GC.GetTotalMemory(false);

            result.AotBenchmarkTime = MeasureTime(() =>
            {
                for (int i = 0; i < config.BenchmarkIterations; i++)
                {
                    AotObjectConstructor.ConstructIfPossible(type, result.TestParameters);
                }
            });

            if (config.CollectMemoryStats)
                result.AotMemoryAfter = GC.GetTotalMemory(false);

            // Calculate metrics
            result.ReflectionAvgTimePerCall = result.ReflectionBenchmarkTime.TotalMilliseconds / config.BenchmarkIterations;
            result.AotAvgTimePerCall = result.AotBenchmarkTime.TotalMilliseconds / config.BenchmarkIterations;
            result.SpeedupRatio = result.ReflectionAvgTimePerCall / result.AotAvgTimePerCall;

            // Validate outputs
            if (config.ValidateOutputs)
            {
                result.ValidationPassed = ValidateTypeOutputs(type, result.TestParameters);
                if (!result.ValidationPassed)
                {
                    result.ErrorMessage = "Output validation failed";
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Generates a formatted benchmark report
    /// </summary>
    /// <param name="report">Benchmark report</param>
    /// <returns>Formatted report string</returns>
    public static string GenerateReport(BenchmarkReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("AOT Object Factory Benchmark Report");
        sb.AppendLine("===================================");
        sb.AppendLine($"Run Date: {report.RunDate:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Total Benchmark Time: {report.TotalBenchmarkTime.TotalSeconds:F2} seconds");
        sb.AppendLine();

        // Summary
        sb.AppendLine("Summary:");
        sb.AppendLine($"  Total Types: {report.TotalTypesBenchmarked}");
        sb.AppendLine($"  Successful: {report.SuccessfulBenchmarks}");
        sb.AppendLine($"  Failed: {report.FailedBenchmarks}");
        sb.AppendLine($"  Average Speedup: {report.AverageSpeedupRatio:F2}x");
        sb.AppendLine($"  Max Speedup: {report.MaxSpeedupRatio:F2}x");
        sb.AppendLine($"  Min Speedup: {report.MinSpeedupRatio:F2}x");
        sb.AppendLine();

        // Performance totals
        sb.AppendLine("Performance Totals:");
        sb.AppendLine($"  Reflection: {report.TotalReflectionCallsPerSecond:N0} calls/second");
        sb.AppendLine($"  AOT: {report.TotalAotCallsPerSecond:N0} calls/second");
        sb.AppendLine();

        // Top performers
        if (report.TopPerformers.Any())
        {
            sb.AppendLine("Top AOT Performers:");
            foreach (var result in report.TopPerformers.Take(5))
            {
                sb.AppendLine($"  {result.Type.Name}: {result.SpeedupRatio:F2}x speedup ({result.CallsPerSecondAot:N0} vs {result.CallsPerSecondReflection:N0} calls/sec)");
            }
            sb.AppendLine();
        }

        // Problematic types
        if (report.ProblematicTypes.Any())
        {
            sb.AppendLine("Types Needing Attention:");
            foreach (var result in report.ProblematicTypes)
            {
                var issue = result.ErrorMessage ?? $"{result.SpeedupRatio:F2}x speedup (no benefit)";
                sb.AppendLine($"  {result.Type.Name}: {issue}");
            }
            sb.AppendLine();
        }

        // Detailed results
        sb.AppendLine("Detailed Results:");
        sb.AppendLine("Type".PadRight(40) + "Reflection (ms)".PadRight(15) + "AOT (ms)".PadRight(12) + "Speedup".PadRight(10) + "Calls/sec (R/A)".PadRight(20) + "Status");
        sb.AppendLine(new string('-', 120));

        foreach (var result in report.TypeResults.OrderByDescending(r => r.SpeedupRatio))
        {
            var status = result.ErrorMessage == null ? "✓" : $"✗ {result.ErrorMessage}";
            var callsPerSec = $"{result.CallsPerSecondReflection:N0}/{result.CallsPerSecondAot:N0}";

            sb.AppendLine($"{result.Type.Name.PadRight(40)}" +
                         $"{result.ReflectionAvgTimePerCall:F3}".PadRight(15) +
                         $"{result.AotAvgTimePerCall:F3}".PadRight(12) +
                         $"{result.SpeedupRatio:F2}x".PadRight(10) +
                         $"{callsPerSec}".PadRight(20) +
                         status);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates performance recommendations based on benchmark results
    /// </summary>
    /// <param name="report">Benchmark report</param>
    /// <returns>List of recommendations</returns>
    public static List<string> GenerateRecommendations(BenchmarkReport report)
    {
        var recommendations = new List<string>();

        if (report.SuccessfulBenchmarks == 0)
        {
            recommendations.Add("No successful benchmarks - check that types have AOT factories registered");
            return recommendations;
        }

        // Overall performance
        if (report.AverageSpeedupRatio < 2.0)
        {
            recommendations.Add("Average speedup is low - consider adding AOT factories to more types");
        }

        if (report.AverageSpeedupRatio > 5.0)
        {
            recommendations.Add("Excellent AOT performance - consider migrating more types to AOT");
        }

        // High-performers
        var highPerformers = report.TypeResults.Where(r => r.SpeedupRatio > 10.0).ToList();
        if (highPerformers.Any())
        {
            recommendations.Add($"Found {highPerformers.Count} types with >10x speedup - prioritize these for AOT migration");
        }

        // Problematic types
        var problematicCount = report.ProblematicTypes.Count;
        if (problematicCount > 0)
        {
            recommendations.Add($"{problematicCount} types have issues or don't benefit from AOT - review these types");
        }

        // Memory usage
        if (report.AverageAotMemoryDelta > report.AverageReflectionMemoryDelta * 1.5)
        {
            recommendations.Add("AOT construction uses more memory - investigate memory usage patterns");
        }

        // Coverage
        var coverage = (double)report.SuccessfulBenchmarks / report.TotalTypesBenchmarked * 100;
        if (coverage < 50)
        {
            recommendations.Add($"Only {coverage:F1}% of types benefit from AOT - consider adding AOT factories to more types");
        }

        return recommendations;
    }

    private static IEnumerable<Type> GetTypesForBenchmark(Assembly assembly, BenchmarkConfig config)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType && t.GetConstructors().Any());

        if (config.IncludeNamespaces.Any())
        {
            types = types.Where(t => config.IncludeNamespaces.Any(ns =>
                t.Namespace?.StartsWith(ns) == true));
        }

        if (config.IncludeTypes.Any())
        {
            types = types.Where(t => config.IncludeTypes.Contains(t));
        }

        if (config.ExcludeTypes.Any())
        {
            types = types.Where(t => !config.ExcludeTypes.Contains(t));
        }

        return types;
    }

    private static ConstructorInfo FindBenchmarkableConstructor(Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Prefer parameterless constructor
        var parameterless = constructors.FirstOrDefault(c => !c.GetParameters().Any());
        if (parameterless != null)
            return parameterless;

        // Prefer constructors with simple parameters
        return constructors.FirstOrDefault(c => c.GetParameters().All(p => IsSimpleParameter(p.ParameterType)));
    }

    private static object[] CreateTestParameters(ConstructorInfo constructor)
    {
        return constructor.GetParameters().Select(p => CreateTestParameter(p.ParameterType)).ToArray();
    }

    private static object CreateTestParameter(Type type)
    {
        if (type == typeof(string)) return "benchmark_test";
        if (type == typeof(int)) return 42;
        if (type == typeof(bool)) return true;
        if (type == typeof(double)) return 3.14;
        if (type == typeof(decimal)) return 123.45m;
        if (type == typeof(DateTime)) return DateTime.UtcNow;
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
        if (type.IsValueType) return Activator.CreateInstance(type);

        return null;
    }

    private static bool IsSimpleParameter(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid) ||
               type.IsEnum ||
               type.IsValueType;
    }

    private static TimeSpan MeasureTime(Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private static bool ValidateTypeOutputs(Type type, object[] parameters)
    {
        try
        {
            var reflectionResult = ObjectConstructor.ConstructIfPossible(type, parameters);
            var aotResult = AotObjectConstructor.ConstructIfPossible(type, parameters);

            if (reflectionResult == null && aotResult == null)
                return true;

            if (reflectionResult == null || aotResult == null)
                return false;

            return reflectionResult.GetType() == aotResult.GetType();
        }
        catch
        {
            return false;
        }
    }
}