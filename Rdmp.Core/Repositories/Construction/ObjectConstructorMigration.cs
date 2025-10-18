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
/// Utilities for migrating from reflection-based ObjectConstructor to AOT-compatible factories
/// Provides performance comparison, validation, and gradual migration tools
/// </summary>
public static class ObjectConstructorMigration
{
    /// <summary>
    /// Configuration for migration analysis
    /// </summary>
    public class MigrationConfig
    {
        /// <summary>
        /// Number of iterations for performance testing
        /// </summary>
        public int PerformanceTestIterations { get; set; } = 10000;

        /// <summary>
        /// Whether to validate that AOT and reflection produce identical results
        /// </summary>
        public bool ValidateOutput { get; set; } = true;

        /// <summary>
        /// Whether to generate AOT factory suggestions for classes without them
        /// </summary>
        public bool GenerateSuggestions { get; set; } = true;

        /// <summary>
        /// Types to exclude from analysis
        /// </summary>
        public List<Type> ExcludedTypes { get; set; } = new();

        /// <summary>
        /// Namespaces to include in analysis (null for all)
        /// </summary>
        public List<string> IncludedNamespaces { get; set; } = new();
    }

    /// <summary>
    /// Results of migration analysis
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// Types analyzed
        /// </summary>
        public List<Type> AnalyzedTypes { get; set; } = new();

        /// <summary>
        /// Types that already have AOT factories
        /// </summary>
        public List<Type> TypesWithAotFactories { get; set; } = new();

        /// <summary>
        /// Types that can have AOT factories generated
        /// </summary>
        public List<Type> CandidatesForAot { get; set; } = new();

        /// <summary>
        /// Types that cannot use AOT factories (reasons)
        /// </summary>
        public Dictionary<Type, string> IncompatibleTypes { get; set; } = new();

        /// <summary>
        /// Performance comparison results
        /// </summary>
        public List<TypePerformanceResult> PerformanceResults { get; set; } = new();

        /// <summary>
        /// Suggested AOT factory attributes to add
        /// </summary>
        public List<SuggestedAttribute> SuggestedAttributes { get; set; } = new();

        /// <summary>
        /// Overall migration statistics
        /// </summary>
        public MigrationStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// Performance result for a specific type
    /// </summary>
    public class TypePerformanceResult
    {
        public Type Type { get; set; }
        public TimeSpan ReflectionTime { get; set; }
        public TimeSpan AotTime { get; set; }
        public double SpeedupRatio { get; set; }
        public bool TestPassed { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Suggested AOT factory attribute
    /// </summary>
    public class SuggestedAttribute
    {
        public Type Type { get; set; }
        public string AttributeCode { get; set; }
        public ConstructorInfo TargetConstructor { get; set; }
        public int Priority { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Migration statistics
    /// </summary>
    public class MigrationStatistics
    {
        public int TotalTypes { get; set; }
        public int TypesWithAotFactories { get; set; }
        public int CandidateTypes { get; set; }
        public int IncompatibleTypes { get; set; }
        public double AverageSpeedupRatio { get; set; }
        public double PotentialAotCoverage => TotalTypes > 0 ? (double)(TypesWithAotFactories + CandidateTypes) / TotalTypes * 100 : 0;
    }

    /// <summary>
    /// Analyzes the current codebase for AOT migration opportunities
    /// </summary>
    /// <param name="assembly">Assembly to analyze</param>
    /// <param name="config">Migration configuration</param>
    /// <returns>Migration analysis results</returns>
    public static MigrationResult AnalyzeMigration(Assembly assembly, MigrationConfig config = null)
    {
        config ??= new MigrationConfig();
        var result = new MigrationResult();

        // Initialize AOT factories
        AotObjectFactoryRegistry.Initialize();

        // Get all types to analyze
        var types = GetTypesToAnalyze(assembly, config);
        result.AnalyzedTypes = types.ToList();

        foreach (var type in types)
        {
            try
            {
                AnalyzeType(type, config, result);
            }
            catch (Exception ex)
            {
                result.IncompatibleTypes[type] = $"Analysis failed: {ex.Message}";
            }
        }

        // Calculate statistics
        result.Statistics = new MigrationStatistics
        {
            TotalTypes = result.AnalyzedTypes.Count,
            TypesWithAotFactories = result.TypesWithAotFactories.Count,
            CandidateTypes = result.CandidatesForAot.Count,
            IncompatibleTypes = result.IncompatibleTypes.Count,
            AverageSpeedupRatio = result.PerformanceResults.Count > 0
                ? result.PerformanceResults.Where(r => r.TestPassed).Average(r => r.SpeedupRatio)
                : 0
        };

        return result;
    }

    /// <summary>
    /// Runs performance comparison between reflection and AOT construction
    /// </summary>
    /// <param name="types">Types to test</param>
    /// <param name="config">Test configuration</param>
    /// <returns>Performance results</returns>
    public static async Task<List<TypePerformanceResult>> RunPerformanceTests(
        IEnumerable<Type> types, MigrationConfig config = null)
    {
        config ??= new MigrationConfig();
        var results = new List<TypePerformanceResult>();

        await Task.Run(() =>
        {
            foreach (var type in types)
            {
                var result = TestTypePerformance(type, config);
                if (result != null)
                    results.Add(result);
            }
        });

        return results;
    }

    /// <summary>
    /// Generates C# code for adding AOT factory attributes to types
    /// </summary>
    /// <param name="suggestions">Suggested attributes</param>
    /// <returns>C# code snippets</returns>
    public static string GenerateAttributeCode(IEnumerable<SuggestedAttribute> suggestions)
    {
        var code = new System.Text.StringBuilder();
        code.AppendLine("// Generated AOT factory attributes");
        code.AppendLine("// Add these attributes to the corresponding classes");

        foreach (var suggestion in suggestions)
        {
            code.AppendLine();
            code.AppendLine($"// {suggestion.Reason}");
            code.AppendLine($"[GenerateAotFactory(Priority = {suggestion.Priority})]");
            code.AppendLine($"public partial class {suggestion.Type.Name}");
            code.AppendLine("{");
            code.AppendLine("    // Existing implementation...");
            code.AppendLine("}");
        }

        return code.ToString();
    }

    /// <summary>
    /// Validates that AOT and reflection construction produce identical results
    /// </summary>
    /// <param name="type">Type to validate</param>
    /// <param name="constructorArgs">Constructor arguments to test</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public static bool ValidateType(Type type, params object[] constructorArgs)
    {
        try
        {
            var reflectionResult = ObjectConstructor.ConstructIfPossible(type, constructorArgs);
            var aotResult = AotObjectConstructor.ConstructIfPossible(type, constructorArgs);

            if (reflectionResult == null && aotResult == null)
                return true;

            if (reflectionResult == null || aotResult == null)
                return false;

            // Compare basic properties
            return reflectionResult.GetType() == aotResult.GetType();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets migration recommendations for a specific type
    /// </summary>
    /// <param name="type">Type to analyze</param>
    /// <returns>Migration recommendations</returns>
    public static string GetMigrationRecommendation(Type type)
    {
        if (AotObjectConstructor.HasAotFactory(type))
            return "Type already has AOT factory - no action needed";

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
            return "No public constructors found - AOT factory not applicable";

        if (constructors.Length == 1)
            return "Single constructor - ideal candidate for AOT factory";

        var markedConstructors = constructors.Where(c => c.GetCustomAttribute<UseWithObjectConstructorAttribute>() != null).ToList();
        if (markedConstructors.Count == 1)
            return "Multiple constructors but one marked with UseWithObjectConstructor - good candidate for AOT factory";

        return "Multiple constructors without clear preference - consider marking preferred constructor with UseWithAotFactoryAttribute";
    }

    private static IEnumerable<Type> GetTypesToAnalyze(Assembly assembly, MigrationConfig config)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType && t.GetConstructors().Any());

        if (config.IncludedNamespaces.Any())
        {
            types = types.Where(t => config.IncludedNamespaces.Any(ns =>
                t.Namespace?.StartsWith(ns) == true));
        }

        if (config.ExcludedTypes.Any())
        {
            types = types.Where(t => !config.ExcludedTypes.Contains(t));
        }

        return types;
    }

    private static void AnalyzeType(Type type, MigrationConfig config, MigrationResult result)
    {
        // Check if type already has AOT factory
        if (AotObjectConstructor.HasAotFactory(type))
        {
            result.TypesWithAotFactories.Add(type);
            return;
        }

        // Check if type is compatible with AOT
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (!constructors.Any())
        {
            result.IncompatibleTypes[type] = "No public constructors";
            return;
        }

        // Check for complex constructor parameters that might not be AOT-friendly
        if (constructors.Any(c => c.GetParameters().Any(p => IsComplexParameter(p.ParameterType))))
        {
            result.IncompatibleTypes[type] = "Has complex constructor parameters";
            return;
        }

        // Type is a candidate for AOT
        result.CandidatesForAot.Add(type);

        // Generate suggestions
        if (config.GenerateSuggestions)
        {
            var suggestion = GenerateSuggestion(type, constructors);
            if (suggestion != null)
                result.SuggestedAttributes.Add(suggestion);
        }

        // Run performance test
        var perfResult = TestTypePerformance(type, config);
        if (perfResult != null)
            result.PerformanceResults.Add(perfResult);
    }

    private static TypePerformanceResult TestTypePerformance(Type type, MigrationConfig config)
    {
        var result = new TypePerformanceResult { Type = type };

        try
        {
            // Find a suitable constructor for testing
            var constructor = FindTestableConstructor(type);
            if (constructor == null)
            {
                result.ErrorMessage = "No testable constructor found";
                return result;
            }

            var parameters = CreateTestParameters(constructor);

            // Test reflection performance
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < config.PerformanceTestIterations; i++)
            {
                ObjectConstructor.ConstructIfPossible(type, parameters);
            }
            stopwatch.Stop();
            result.ReflectionTime = stopwatch.Elapsed;

            // Test AOT performance
            stopwatch.Restart();
            for (int i = 0; i < config.PerformanceTestIterations; i++)
            {
                AotObjectConstructor.ConstructIfPossible(type, parameters);
            }
            stopwatch.Stop();
            result.AotTime = stopwatch.Elapsed;

            // Calculate speedup
            result.SpeedupRatio = result.ReflectionTime.TotalMilliseconds / result.AotTime.TotalMilliseconds;
            result.TestPassed = true;

            // Validate output if requested
            if (config.ValidateOutput)
            {
                var reflectionResult = ObjectConstructor.ConstructIfPossible(type, parameters);
                var aotResult = AotObjectConstructor.ConstructIfPossible(type, parameters);

                if (reflectionResult?.GetType() != aotResult?.GetType())
                {
                    result.TestPassed = false;
                    result.ErrorMessage = "Output validation failed";
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            result.TestPassed = false;
        }

        return result;
    }

    private static SuggestedAttribute GenerateSuggestion(Type type, ConstructorInfo[] constructors)
    {
        ConstructorInfo targetConstructor = null;
        int priority = 0;
        string reason = "Candidate for AOT factory";

        // Prefer constructors marked with UseWithObjectConstructorAttribute
        var markedConstructor = constructors.FirstOrDefault(c => c.GetCustomAttribute<UseWithObjectConstructorAttribute>() != null);
        if (markedConstructor != null)
        {
            targetConstructor = markedConstructor;
            priority = 10;
            reason = "Has UseWithObjectConstructorAttribute marking";
        }
        // Prefer single parameter constructors
        else if (constructors.Length == 1 && constructors[0].GetParameters().Length == 1)
        {
            targetConstructor = constructors[0];
            priority = 5;
            reason = "Single parameter constructor";
        }
        // Otherwise, take the first constructor
        else
        {
            targetConstructor = constructors[0];
            priority = 1;
            reason = "Default constructor selection";
        }

        return new SuggestedAttribute
        {
            Type = type,
            TargetConstructor = targetConstructor,
            Priority = priority,
            Reason = reason
        };
    }

    private static ConstructorInfo FindTestableConstructor(Type type)
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
        if (type == typeof(string)) return "test";
        if (type == typeof(int)) return 1;
        if (type == typeof(bool)) return true;
        if (type == typeof(double)) return 1.0;
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

    private static bool IsComplexParameter(Type type)
    {
        return !IsSimpleParameter(type) &&
               type != typeof(IRDMPPlatformRepositoryServiceLocator) &&
               type != typeof(ICatalogueRepository) &&
               type != typeof(IDataExportRepository) &&
               type != typeof(IDQERepository) &&
               !typeof(IRepository).IsAssignableFrom(type);
    }
}