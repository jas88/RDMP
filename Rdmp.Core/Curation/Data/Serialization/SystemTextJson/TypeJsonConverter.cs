// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rdmp.Core.Curation.Data.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json converter for <see cref="System.Type"/> instances.
/// Serializes types as their AssemblyQualifiedName string for compatibility with Newtonsoft.Json behavior.
/// </summary>
/// <remarks>
/// System.Text.Json does not support Type serialization by default for security reasons.
/// This converter provides explicit opt-in support matching Newtonsoft.Json's TypeNameHandling behavior.
/// </remarks>
public class TypeJsonConverter : JsonConverter<Type>
{
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected string token for Type, got {reader.TokenType}");

        var typeName = reader.GetString();
        if (string.IsNullOrEmpty(typeName))
            return null;

        // Try to resolve the type - first try exact match, then search loaded assemblies
        var type = Type.GetType(typeName);
        if (type != null)
            return type;

        // Search loaded assemblies for partial type names (for backward compatibility)
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        throw new JsonException($"Could not resolve type: {typeName}");
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Use AssemblyQualifiedName for full fidelity, matching Newtonsoft.Json behavior
        writer.WriteStringValue(value.AssemblyQualifiedName);
    }
}
