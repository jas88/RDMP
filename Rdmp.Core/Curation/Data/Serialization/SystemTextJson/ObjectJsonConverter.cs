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
/// System.Text.Json converter for <see cref="object"/> typed values.
/// Converts JsonElement to appropriate CLR types during deserialization.
/// </summary>
/// <remarks>
/// System.Text.Json leaves object-typed values as JsonElement by default.
/// This converter converts them to proper CLR types (string, int, double, bool, etc.)
/// to match Newtonsoft.Json behavior and allow proper SQL parameter binding.
/// </remarks>
public class ObjectJsonConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadValue(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Use the actual runtime type for serialization
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    /// <summary>
    /// Reads a JSON value and converts it to the appropriate CLR type
    /// </summary>
    private static object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.String:
                // Return strings as-is - don't try to parse as DateTime/Guid
                // The calling code knows the expected type and will convert as needed
                return reader.GetString();

            case JsonTokenType.Number:
                // Try integer types first (prefer long for large values)
                if (reader.TryGetInt32(out var int32))
                    return int32;
                if (reader.TryGetInt64(out var int64))
                    return int64;
                // Fall back to double for floating point
                if (reader.TryGetDouble(out var dbl))
                    return dbl;
                // Last resort: decimal
                if (reader.TryGetDecimal(out var dec))
                    return dec;
                return reader.GetDouble();

            case JsonTokenType.StartArray:
                // Read array as object[]
                var list = new System.Collections.Generic.List<object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(ReadValue(ref reader, options));
                }
                return list.ToArray();

            case JsonTokenType.StartObject:
                // Read object as Dictionary<string, object>
                var dict = new System.Collections.Generic.Dictionary<string, object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException($"Expected PropertyName, got {reader.TokenType}");

                    var propertyName = reader.GetString();
                    reader.Read();
                    dict[propertyName] = ReadValue(ref reader, options);
                }
                return dict;

            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }
    }

    /// <summary>
    /// Converts a JsonElement to the appropriate CLR type.
    /// Useful for post-processing deserialized objects that contain JsonElement values.
    /// </summary>
    public static object ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.String:
                // Return strings as-is - don't try to parse as DateTime/Guid
                // The calling code knows the expected type and will convert as needed
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt32(out var i32))
                    return i32;
                if (element.TryGetInt64(out var i64))
                    return i64;
                if (element.TryGetDouble(out var d))
                    return d;
                if (element.TryGetDecimal(out var dec))
                    return dec;
                return element.GetDouble();

            case JsonValueKind.Array:
                var arr = new object[element.GetArrayLength()];
                var idx = 0;
                foreach (var item in element.EnumerateArray())
                {
                    arr[idx++] = ConvertJsonElement(item);
                }
                return arr;

            case JsonValueKind.Object:
                var dict = new System.Collections.Generic.Dictionary<string, object>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElement(prop.Value);
                }
                return dict;

            default:
                return element.ToString();
        }
    }
}
