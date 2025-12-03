// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

#nullable enable

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Rdmp.Core.ReusableLibraryCode;

/// <summary>
/// Provides AOT-compatible type resolution for common BCL types and database column types.
/// This replaces Type.GetType(string) calls which are not AOT-compatible.
/// </summary>
public static class AotTypeResolver
{
    /// <summary>
    /// Registry of common .NET BCL types that appear in database schemas and CSV type specifications.
    /// Uses FrozenDictionary for optimal performance in AOT scenarios.
    /// </summary>
    private static readonly FrozenDictionary<string, Type> _bclTypes;

    static AotTypeResolver()
    {
        var dict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Primitive types - most common in database schemas
            ["System.String"] = typeof(string),
            ["string"] = typeof(string),
            ["System.Int32"] = typeof(int),
            ["int"] = typeof(int),
            ["Int32"] = typeof(int),
            ["System.Int64"] = typeof(long),
            ["long"] = typeof(long),
            ["Int64"] = typeof(long),
            ["System.Int16"] = typeof(short),
            ["short"] = typeof(short),
            ["Int16"] = typeof(short),
            ["System.Byte"] = typeof(byte),
            ["byte"] = typeof(byte),
            ["Byte"] = typeof(byte),
            ["System.SByte"] = typeof(sbyte),
            ["sbyte"] = typeof(sbyte),
            ["SByte"] = typeof(sbyte),

            // Floating point types
            ["System.Double"] = typeof(double),
            ["double"] = typeof(double),
            ["Double"] = typeof(double),
            ["System.Single"] = typeof(float),
            ["float"] = typeof(float),
            ["Single"] = typeof(float),
            ["System.Decimal"] = typeof(decimal),
            ["decimal"] = typeof(decimal),
            ["Decimal"] = typeof(decimal),

            // Boolean
            ["System.Boolean"] = typeof(bool),
            ["bool"] = typeof(bool),
            ["Boolean"] = typeof(bool),

            // DateTime types
            ["System.DateTime"] = typeof(DateTime),
            ["DateTime"] = typeof(DateTime),
            ["System.DateTimeOffset"] = typeof(DateTimeOffset),
            ["DateTimeOffset"] = typeof(DateTimeOffset),
            ["System.TimeSpan"] = typeof(TimeSpan),
            ["TimeSpan"] = typeof(TimeSpan),

            // Guid
            ["System.Guid"] = typeof(Guid),
            ["Guid"] = typeof(Guid),

            // Byte array (common for binary data)
            ["System.Byte[]"] = typeof(byte[]),
            ["byte[]"] = typeof(byte[]),
            ["Byte[]"] = typeof(byte[]),

            // Object (for variant/dynamic columns)
            ["System.Object"] = typeof(object),
            ["object"] = typeof(object),
            ["Object"] = typeof(object),

            // UInt types (less common but present in some databases)
            ["System.UInt32"] = typeof(uint),
            ["uint"] = typeof(uint),
            ["UInt32"] = typeof(uint),
            ["System.UInt64"] = typeof(ulong),
            ["ulong"] = typeof(ulong),
            ["UInt64"] = typeof(ulong),
            ["System.UInt16"] = typeof(ushort),
            ["ushort"] = typeof(ushort),
            ["UInt16"] = typeof(ushort),

            // Char (occasionally used)
            ["System.Char"] = typeof(char),
            ["char"] = typeof(char),
            ["Char"] = typeof(char),
        };

        _bclTypes = dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to resolve a type name to a Type object using AOT-compatible lookup.
    /// First checks the BCL type registry, then falls back to CompiledTypeRegistry if available.
    /// </summary>
    /// <param name="typeName">The type name to resolve (e.g., "System.String", "string", "Int32")</param>
    /// <returns>The resolved Type, or null if not found</returns>
    public static Type? GetType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        // First check BCL types (most common case for database schemas and CSV typing)
        if (_bclTypes.TryGetValue(typeName, out var bclType))
            return bclType;

#if HAS_COMPILED_TYPE_REGISTRY
        // Fall back to CompiledTypeRegistry for RDMP types
        var registryType = Repositories.CompiledTypeRegistry.GetType(typeName);
        if (registryType != null)
            return registryType;
#endif

        // For AOT compatibility, we don't call Type.GetType() as a fallback
        // If a type is needed that's not in the registry, it should be added explicitly
        return null;
    }

    /// <summary>
    /// Gets the count of BCL types in the registry (for diagnostics)
    /// </summary>
    public static int BclTypeCount => _bclTypes.Count;

    /// <summary>
    /// Gets all registered BCL types (for diagnostics and testing)
    /// </summary>
    public static IEnumerable<KeyValuePair<string, Type>> GetAllBclTypes() => _bclTypes;
}
