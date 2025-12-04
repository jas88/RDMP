// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rdmp.Core.Repositories.Construction;

namespace Rdmp.Core.Curation.Data.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json converter that supports deserialization of objects without default constructors.
/// Finds a compatible constructor based on provided constructor objects and populates remaining properties from JSON.
/// </summary>
/// <remarks>
/// This is the System.Text.Json equivalent of <see cref="PickAnyConstructorJsonConverter"/>.
/// </remarks>
public class PickAnyConstructorJsonConverter : JsonConverterFactory
{
    private readonly object[] _constructorObjects;

    /// <summary>
    /// Creates a JSON deserializer that can use constructors matching <paramref name="constructorObjects"/>
    /// </summary>
    /// <param name="constructorObjects">Objects to pass to constructors</param>
    public PickAnyConstructorJsonConverter(params object[] constructorObjects)
    {
        _constructorObjects = constructorObjects ?? Array.Empty<object>();
    }

    /// <summary>
    /// Determines if this converter can handle the specified type
    /// </summary>
    public override bool CanConvert(Type typeToConvert)
    {
        // Don't handle value types
        if (typeToConvert.IsValueType)
            return false;

        // Check if there's a compatible constructor
        var constructors = GetConstructors(typeToConvert);

        if (constructors.Count == 0)
            return false;

        if (constructors.Count > 1)
            throw new ObjectLacksCompatibleConstructorException(
                $"There were {constructors.Count} compatible constructors for the constructorObjects provided");

        return true;
    }

    /// <summary>
    /// Creates the actual converter instance for the specific type
    /// </summary>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(PickAnyConstructorJsonConverterInner<>).MakeGenericType(typeToConvert);
        // Must wrap in object[] to pass as single parameter, not expanded params
        return (JsonConverter)Activator.CreateInstance(converterType, new object[] { _constructorObjects });
    }

    private Dictionary<ConstructorInfo, List<object>> GetConstructors(Type objectType) =>
        ObjectConstructor.GetConstructors(objectType, false, false, _constructorObjects);

    /// <summary>
    /// Inner generic converter that handles the actual serialization/deserialization
    /// </summary>
    private class PickAnyConstructorJsonConverterInner<T> : JsonConverter<T>
    {
        private readonly object[] _constructorObjects;

        public PickAnyConstructorJsonConverterInner(object[] constructorObjects)
        {
            _constructorObjects = constructorObjects;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Parse the entire JSON object into a document
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            // Find the compatible constructor
            var constructors = ObjectConstructor.GetConstructors(typeToConvert, false, false, _constructorObjects);

            if (constructors.Count == 0)
                throw new JsonException($"No compatible constructor found for type {typeToConvert.Name}");

            if (constructors.Count > 1)
                throw new JsonException($"Multiple compatible constructors found for type {typeToConvert.Name}");

            var constructor = constructors.First();

            // Invoke the constructor
            var instance = (T)constructor.Key.Invoke(constructor.Value.ToArray());

            // Populate properties from JSON
            PopulateObject(instance, root, options);

            // Call AfterConstruction callback if implemented
            if (instance is IPickAnyConstructorFinishedCallback callback)
                callback.AfterConstruction();

            return instance;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // Use default serialization for writing
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        /// <summary>
        /// Populates an object's properties from a JSON element
        /// </summary>
        private static void PopulateObject(T instance, JsonElement root, JsonSerializerOptions options)
        {
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var property in properties)
            {
                // Get the JSON property name (respecting naming policy)
                var jsonPropertyName = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;

                // Check if the JSON contains this property
                if (!root.TryGetProperty(jsonPropertyName, out var jsonValue))
                    continue;

                try
                {
                    // Deserialize the property value
                    var propertyValue = JsonSerializer.Deserialize(jsonValue.GetRawText(), property.PropertyType, options);
                    property.SetValue(instance, propertyValue);
                }
                catch (Exception ex)
                {
                    throw new JsonException($"Failed to deserialize property '{property.Name}' on type '{type.Name}'", ex);
                }
            }
        }
    }
}
