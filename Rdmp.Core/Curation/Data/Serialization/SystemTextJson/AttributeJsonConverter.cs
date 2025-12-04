// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rdmp.Core.Curation.Data.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json converter factory for <see cref="Attribute"/>-derived types.
/// Excludes the inherited <see cref="Attribute.TypeId"/> property which contains a <see cref="Type"/>
/// boxed as <see cref="object"/>, causing serialization issues with RuntimeType.
/// </summary>
public class AttributeJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeof(Attribute).IsAssignableFrom(typeToConvert) && typeToConvert != typeof(Attribute);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(AttributeJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType);
    }
}

/// <summary>
/// Generic converter for attribute types that excludes TypeId
/// </summary>
public class AttributeJsonConverter<T> : JsonConverter<T> where T : Attribute
{
    private static readonly PropertyInfo[] SerializableProperties;

    static AttributeJsonConverter()
    {
        // Get all public instance properties except TypeId (inherited from Attribute)
        SerializableProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != "TypeId" && p.CanRead)
            .ToArray();
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject token for {typeof(T).Name}, got {reader.TokenType}");

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        // Find a constructor - prefer parameterless, then any with matching properties
        var constructor = typeof(T).GetConstructors()
            .OrderBy(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (constructor == null)
            throw new JsonException($"No public constructor found for type {typeof(T).Name}");

        // Build constructor arguments
        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var jsonPropertyName = options.PropertyNamingPolicy?.ConvertName(param.Name) ?? param.Name;

            // Try exact match first, then case-insensitive
            if (root.TryGetProperty(jsonPropertyName, out var jsonValue) ||
                (options.PropertyNameCaseInsensitive && TryGetPropertyCaseInsensitive(root, param.Name, out jsonValue)))
            {
                args[i] = JsonSerializer.Deserialize(jsonValue.GetRawText(), param.ParameterType, options);
            }
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
            else
            {
                args[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
            }
        }

        var instance = (T)constructor.Invoke(args);

        // Set remaining writable properties
        foreach (var prop in SerializableProperties.Where(p => p.CanWrite))
        {
            var jsonPropertyName = options.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;

            if (root.TryGetProperty(jsonPropertyName, out var jsonValue) ||
                (options.PropertyNameCaseInsensitive && TryGetPropertyCaseInsensitive(root, prop.Name, out jsonValue)))
            {
                var value = JsonSerializer.Deserialize(jsonValue.GetRawText(), prop.PropertyType, options);
                prop.SetValue(instance, value);
            }
        }

        return instance;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        foreach (var prop in SerializableProperties)
        {
            var propValue = prop.GetValue(value);
            var jsonPropertyName = options.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;

            writer.WritePropertyName(jsonPropertyName);
            JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
        }

        writer.WriteEndObject();
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }
}
