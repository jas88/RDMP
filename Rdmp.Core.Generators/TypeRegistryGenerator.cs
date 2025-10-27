// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Rdmp.Core.Generators;

/// <summary>
/// Generates a compile-time type registry (FrozenDictionary) containing all types
/// from all referenced assemblies. This eliminates runtime reflection overhead and
/// assembly loading timing issues.
/// </summary>
[Generator]
public class TypeRegistryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Collect all types from the compilation
        var typesProvider = context.CompilationProvider
            .Select((compilation, _) =>
            {
                var types = new List<TypeInfo>();
                var currentAssemblyName = compilation.AssemblyName;

                // Get types from current compilation (can include internal)
                CollectTypes(compilation.Assembly, types, currentAssemblyName, includeInternal: true);

                // Get types from all referenced assemblies (public only)
                foreach (var reference in compilation.References)
                {
                    var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
                    if (symbol is IAssemblySymbol assemblySymbol)
                    {
                        CollectTypes(assemblySymbol, types, currentAssemblyName, includeInternal: false);
                    }
                }

                return types;
            });

        // Generate the source
        context.RegisterSourceOutput(typesProvider, (spc, types) =>
        {
            var source = GenerateTypeRegistry(types);
            spc.AddSource("CompiledTypeRegistry.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static void CollectTypes(IAssemblySymbol assembly, List<TypeInfo> types, string currentAssemblyName, bool includeInternal)
    {
        // Skip certain assemblies to reduce size
        var name = assembly.Name;
        if (name.StartsWith("System.") ||
            name.StartsWith("Microsoft.") ||
            name.StartsWith("netstandard") ||
            name.StartsWith("mscorlib") ||
            name.StartsWith("NUnit") ||
            name.StartsWith("coverlet") ||
            name.StartsWith("NSubstitute") ||
            name == "CommandLine")
            return;

        CollectTypesFromNamespace(assembly.GlobalNamespace, types, includeInternal);
    }

    private static void CollectTypesFromNamespace(INamespaceSymbol ns, List<TypeInfo> types, bool includeInternal)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                CollectTypesFromNamespace(childNs, types, includeInternal);
            }
            else if (member is INamedTypeSymbol type && ShouldIncludeType(type, includeInternal))
            {
                // Use fully qualified name with global:: to avoid namespace conflicts
                var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Some types can't be referenced even with global:: (nested private types, etc)
                // We'll validate this doesn't contain invalid characters
                if (fullName.Contains("<PrivateImplementationDetails>") ||
                    fullName.Contains("<<") ||
                    fullName.Contains(">>"))
                    return;

                types.Add(new TypeInfo
                {
                    FullName = fullName,
                    ShortName = type.Name,
                    Namespace = type.ContainingNamespace?.ToDisplayString() ?? ""
                });
            }
        }
    }

    private static bool ShouldIncludeType(INamedTypeSymbol type, bool includeInternal)
    {
        // Only include classes
        if (type.TypeKind != TypeKind.Class) return false;

        // Skip generic type definitions with unbound parameters
        if (type.IsGenericType && type.TypeParameters.Length > 0) return false;

        // Skip types in .Internal namespaces (experimental/private APIs)
        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        if (ns.Contains(".Internal.") || ns.EndsWith(".Internal")) return false;

        // Skip compiler-generated and special types
        if (type.Name.Contains("<") || type.Name.Contains(">") || type.Name.StartsWith("__")) return false;

        // Skip types marked as Obsolete/Experimental with diagnostic IDs (like NPG9001)
        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "ObsoleteAttribute" ||
                attr.AttributeClass?.Name == "ExperimentalAttribute")
            {
                // If it has a diagnostic ID, skip it
                if (attr.ConstructorArguments.Length > 0)
                    return false;
            }
        }

        // Check accessibility
        if (type.DeclaredAccessibility == Accessibility.Public)
            return true;

        if (type.DeclaredAccessibility == Accessibility.Internal && includeInternal)
            return true;

        // Skip private, protected, and other non-accessible types
        return false;
    }

    private static string GenerateTypeRegistry(List<TypeInfo> types)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#define HAS_COMPILED_TYPE_REGISTRY");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Frozen;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("namespace Rdmp.Core.Repositories;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Compile-time generated type registry containing all types from referenced assemblies.");
        sb.AppendLine($"/// Generated at compile-time with {types.Count} types.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("internal static partial class CompiledTypeRegistry");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly FrozenDictionary<string, Type> _typesByName;");
        sb.AppendLine();
        sb.AppendLine("    static CompiledTypeRegistry()");
        sb.AppendLine("    {");
        sb.AppendLine("        var dict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);");
        sb.AppendLine();

        // Group types by short name to handle duplicates
        var grouped = types.GroupBy(t => t.ShortName);

        foreach (var group in grouped)
        {
            if (group.Count() == 1)
            {
                var type = group.First();
                sb.AppendLine($"        // {type.FullName}");
                // Add all lookup variants
                AddTypeLookup(sb, type.ShortName, type.FullName);
                AddTypeLookup(sb, type.FullName, type.FullName);
            }
            else
            {
                // Multiple types with same short name - only store full names
                foreach (var type in group)
                {
                    sb.AppendLine($"        // {type.FullName} (short name conflict)");
                    AddTypeLookup(sb, type.FullName, type.FullName);
                }
            }
            sb.AppendLine();
        }

        sb.AppendLine("        _typesByName = dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static Type? GetType(string typeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        return _typesByName.TryGetValue(typeName, out var type) ? type : null;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static int TypeCount => _typesByName.Count;");
        sb.AppendLine();
        sb.AppendLine("    public static IEnumerable<KeyValuePair<string, Type>> GetAllTypes()");
        sb.AppendLine("    {");
        sb.AppendLine("        return _typesByName;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AddTypeLookup(StringBuilder sb, string key, string typeFullName)
    {
        // Escape quotes in key
        var escapedKey = key.Replace("\"", "\\\"");
        // Remove global:: prefix for the key lookup, but keep it for typeof()
        var lookupKey = key.Replace("global::", "");
        sb.AppendLine($"        dict.TryAdd(\"{escapedKey}\", typeof({typeFullName}));");
    }

    private class TypeInfo
    {
        public string FullName { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Namespace { get; set; } = "";
    }
}
