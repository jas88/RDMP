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
/// System.Text.Json converter for <see cref="Type"/> that serializes types as their assembly-qualified names.
/// </summary>
/// <remarks>
/// System.Text.Json does not support serialization of <see cref="Type"/> by default.
/// This converter serializes types as strings containing their assembly-qualified names,
/// which can then be resolved back to types using <see cref="Type.GetType(string)"/>.
/// </remarks>
public class TypeJsonConverter : JsonConverter<Type>
{
    /// <summary>
    /// Reads a type from its assembly-qualified name string representation
    /// </summary>
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected string token for Type, got {reader.TokenType}");

        var typeName = reader.GetString();
        if (string.IsNullOrEmpty(typeName))
            return null;

        var type = Type.GetType(typeName, throwOnError: false);
        if (type == null)
            throw new JsonException($"Could not resolve type '{typeName}'");

        return type;
    }

    /// <summary>
    /// Writes a type as its assembly-qualified name string
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.AssemblyQualifiedName);
    }
}
