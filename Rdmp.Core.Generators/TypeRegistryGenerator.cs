// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Rdmp.Core.Generators;

/// <summary>
/// Generates the compile time type registration of an RDMP assembly: a list of every Type declared in the assembly being
/// compiled (classes, structs, records, interfaces, enums and delegates, generic or not, public or not, nested or not), which
/// a module initializer hands to Rdmp.Core.Repositories.CompiledTypeRegistry so that MEF can find the Types without scanning
/// the assembly with reflection.
///
/// <para>Only the assembly's own Types are listed, so a registration never refers to an assembly which is not deployed:
/// each RDMP assembly runs this generator itself (it is referenced by every project in Directory.Build.props).  The Types
/// are listed in the order in which the compiler writes them to metadata, which is the order reflection returns them in,
/// so that MEF resolves short name clashes the same way whichever lists the Types.</para>
///
/// <para>Types which the compiler generates (closures, iterators, anonymous types etc.) have no symbols so are not listed.
/// Nested Types which cannot be named from outside their containing Type (private, protected and private protected) are
/// fetched with GetNestedType from the nearest containing Type which can be named, and file local Types by their metadata
/// name.</para>
/// </summary>
[Generator]
public sealed class TypeRegistryGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Metadata name of the Rdmp.Core class which the generated module initializer registers the Types with
    /// </summary>
    private const string RegistryMetadataName = "Rdmp.Core.Repositories.CompiledTypeRegistry";

    /// <summary>
    /// Metadata name of the assembly attribute which marks an assembly as having a generated registration
    /// </summary>
    private const string AttributeMetadataName = "Rdmp.Core.Repositories.CompiledTypeRegistryAttribute";

    /// <summary>
    /// Start of the name of the generated class (in <see cref="GeneratedNamespace"/>) which registers the Types.  The rest
    /// is the assembly name, so that the class has a different full name in each assembly (an assembly which can see the
    /// internals of another would otherwise see two)
    /// </summary>
    private const string GeneratedClassNamePrefix = "GeneratedTypeRegistration_";

    private const string GeneratedNamespace = "Rdmp.Core.Repositories.Generated";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // The output is a string, so later steps only run again when the list of Types changes
        var source = context.CompilationProvider.Select(static (compilation, ct) => Generate(compilation, ct));

        context.RegisterSourceOutput(source, static (spc, s) =>
        {
            if (s != null)
                spc.AddSource("GeneratedTypeRegistration.g.cs", SourceText.From(s, Encoding.UTF8));
        });
    }

    private static string Generate(Compilation compilation, CancellationToken ct)
    {
        // Nothing to register with in an assembly which can see neither the registry nor its attribute (one which does
        // not reference Rdmp.Core).  MEF only looks for Types in Rdmp.Core and the assemblies which reference it.
        if (compilation.GetTypeByMetadataName(RegistryMetadataName) == null ||
            compilation.GetTypeByMetadataName(AttributeMetadataName) == null)
            return null;

        var entries = GetTypesInMetadataOrder(compilation.SourceModule.GlobalNamespace, ct)
            // Types with unspeakable names (e.g. the class holding top level statements) cannot be referred to
            .Where(static t => t.CanBeReferencedByName && t.TypeKind != TypeKind.Error)
            .Select(GetTypeExpression)
            .ToList();

        return GenerateSource(GetGeneratedClassName(compilation.AssemblyName), entries);
    }

    private static string GetGeneratedClassName(string assemblyName)
    {
        var sb = new StringBuilder(GeneratedClassNamePrefix);

        foreach (var c in assemblyName ?? "")
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');

        return sb.ToString();
    }

    /// <summary>
    /// Returns the Types declared in the module in the order the compiler writes them to the metadata table of Type definitions (and so
    /// the order in which reflection's GetTypes returns them): Types which are not nested in namespace order (each namespace's
    /// Types, then its child namespaces last first), followed by all nested Types breadth first.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetTypesInMetadataOrder(INamespaceSymbol globalNamespace, CancellationToken ct)
    {
        var topLevel = new List<INamedTypeSymbol>();
        var namespaces = new Stack<INamespaceSymbol>();
        namespaces.Push(globalNamespace);

        while (namespaces.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var member in namespaces.Pop().GetMembers())
                if (member is INamespaceSymbol childNamespace)
                    namespaces.Push(childNamespace);
                else if (member is INamedTypeSymbol type)
                    topLevel.Add(type);
        }

        var nested = new Queue<INamedTypeSymbol>();

        foreach (var type in topLevel)
        {
            yield return type;

            foreach (var nestedType in type.GetTypeMembers())
                nested.Enqueue(nestedType);
        }

        while (nested.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var type = nested.Dequeue();
            yield return type;

            foreach (var nestedType in type.GetTypeMembers())
                nested.Enqueue(nestedType);
        }
    }

    /// <summary>
    /// Returns C# which evaluates to the System.Type of <paramref name="type"/> from the generated class
    /// </summary>
    private static string GetTypeExpression(INamedTypeSymbol type)
    {
        // The Type and the Types containing it, outermost first
        var chain = new List<INamedTypeSymbol>();
        for (var t = type; t != null; t = t.ContainingType)
            chain.Insert(0, t);

        // The Types from the outermost which can be named in typeof from another Type in the assembly
        var nameable = chain[0].IsFileLocal ? 0 : chain.TakeWhile(CanBeNamedFromAnotherTypeInTheAssembly).Count();

        if (nameable == chain.Count)
            return $"typeof({GetTypeOfName(type)})";

        var expression = nameable == 0
            ? $"InThisAssembly(\"{GetMetadataFullName(chain[0])}\")"
            : $"typeof({GetTypeOfName(chain[nameable - 1])})";

        for (var i = Max(nameable, 1); i < chain.Count; i++)
            expression = $"Nested({expression}, \"{chain[i].MetadataName}\")";

        return expression;
    }

    private static int Max(int a, int b) => a > b ? a : b;

    private static bool CanBeNamedFromAnotherTypeInTheAssembly(INamedTypeSymbol type) =>
        !type.IsFileLocal &&
        type.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;

    /// <summary>
    /// Returns the name to use in typeof for <paramref name="type"/> e.g. global::Ns.Outer&lt;,&gt;.Inner (generic Types are
    /// unbound)
    /// </summary>
    private static string GetTypeOfName(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();

        if (type.ContainingType != null)
        {
            sb.Append(GetTypeOfName(type.ContainingType)).Append('.');
        }
        else
        {
            sb.Append("global::");
            if (!type.ContainingNamespace.IsGlobalNamespace)
                sb.Append(string.Join(".", GetNamespaceParts(type.ContainingNamespace).Select(EscapeIdentifier))).Append('.');
        }

        sb.Append(EscapeIdentifier(type.Name));

        if (type.Arity > 0)
            sb.Append('<').Append(',', type.Arity - 1).Append('>');

        return sb.ToString();
    }

    /// <summary>
    /// Returns the full name of a Type which is not nested as it appears in metadata (which for a file local Type includes
    /// the prefix the compiler adds to its name)
    /// </summary>
    private static string GetMetadataFullName(INamedTypeSymbol type) =>
        type.ContainingNamespace.IsGlobalNamespace
            ? type.MetadataName
            : $"{string.Join(".", GetNamespaceParts(type.ContainingNamespace))}.{type.MetadataName}";

    private static IEnumerable<string> GetNamespaceParts(INamespaceSymbol ns)
    {
        var parts = new Stack<string>();
        for (var n = ns; n is { IsGlobalNamespace: false }; n = n.ContainingNamespace)
            parts.Push(n.Name);

        return parts;
    }

    private static string EscapeIdentifier(string identifier) =>
        SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? $"@{identifier}" : identifier;

    private static string GenerateSource(string className, List<string> entries)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by Rdmp.Core.Generators.TypeRegistryGenerator: the Types declared in this assembly, for MEF");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS0612, CS0618 // Obsolete Types are registered like any others");
        sb.AppendLine();
        sb.AppendLine($"[assembly: global::{AttributeMetadataName}]");
        sb.AppendLine();
        sb.AppendLine($"namespace {GeneratedNamespace}");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.CodeDom.Compiler.GeneratedCode(\"Rdmp.Core.Generators.TypeRegistryGenerator\", \"2.0.0\")]");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.CompilerGenerated]");
        sb.AppendLine($"    internal static class {className}");
        sb.AppendLine("    {");
        sb.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("        internal static void Register() =>");
        sb.AppendLine($"            global::{RegistryMetadataName}.Register(typeof({className}).Assembly, GetTypes);");
        sb.AppendLine();
        sb.AppendLine($"        // {entries.Count} Types, in metadata order");
        sb.AppendLine("        private static global::System.Type[] GetTypes() => new global::System.Type[]");
        sb.AppendLine("        {");

        foreach (var entry in entries)
            sb.Append("            ").Append(entry).AppendLine(",");

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        private static global::System.Type Nested(");
        sb.AppendLine("            [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(");
        sb.AppendLine("                global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes |");
        sb.AppendLine("                global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]");
        sb.AppendLine("            global::System.Type declaringType, string name) =>");
        sb.AppendLine("            declaringType.GetNestedType(name,");
        sb.AppendLine("                global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic) ??");
        sb.AppendLine("            throw new global::System.TypeLoadException($\"Nested Type '{name}' of '{declaringType}' was not found\");");
        sb.AppendLine();
        sb.AppendLine("        private static global::System.Type InThisAssembly(string fullName) =>");
        sb.AppendLine($"            typeof({className}).Assembly.GetType(fullName, throwOnError: true, ignoreCase: false)!;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
