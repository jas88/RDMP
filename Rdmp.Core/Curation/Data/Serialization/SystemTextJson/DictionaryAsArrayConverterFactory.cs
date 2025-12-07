// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rdmp.Core.Curation.Data.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json converter factory that serializes dictionaries with complex (non-string) keys as arrays.
/// Format: [[key1, value1], [key2, value2], ...]
/// </summary>
/// <remarks>
/// This is the System.Text.Json equivalent of <see cref="DictionaryAsArrayResolver"/>.
/// Standard JSON only supports string keys in objects, so this converter serializes
/// dictionaries as arrays of key-value pairs to support complex keys.
/// </remarks>
public class DictionaryAsArrayConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines if the type is a dictionary type that needs array-based serialization
    /// </summary>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        var genericDef = typeToConvert.GetGenericTypeDefinition();
        return genericDef == typeof(Dictionary<,>);
    }

    /// <summary>
    /// Creates the appropriate converter for the dictionary type
    /// </summary>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var keyType = typeToConvert.GetGenericArguments()[0];
        var valueType = typeToConvert.GetGenericArguments()[1];

        var converterType = typeof(DictionaryAsArrayConverter<,>).MakeGenericType(keyType, valueType);
        return (JsonConverter)Activator.CreateInstance(converterType);
    }
}

/// <summary>
/// Generic dictionary converter that serializes as array of [key, value] pairs
/// </summary>
public class DictionaryAsArrayConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
{
    public override Dictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected StartArray token, got {reader.TokenType}");

        var result = new Dictionary<TKey, TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"Expected StartArray for key-value pair, got {reader.TokenType}");

            // Read key
            if (!reader.Read())
                throw new JsonException("Unexpected end of JSON while reading dictionary key");

            var key = JsonSerializer.Deserialize<TKey>(ref reader, options);

            // Read value
            if (!reader.Read())
                throw new JsonException("Unexpected end of JSON while reading dictionary value");

            var value = JsonSerializer.Deserialize<TValue>(ref reader, options);

            // Read end of pair array
            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException("Expected EndArray for key-value pair");

            result[key] = value;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();

        foreach (var kvp in value)
        {
            writer.WriteStartArray();
            JsonSerializer.Serialize(writer, kvp.Key, options);
            JsonSerializer.Serialize(writer, kvp.Value, options);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }
}
