// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rdmp.Core.Repositories;

namespace Rdmp.Core.Curation.Data.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json extension methods that facilitate serialization/deserialization of RDMP objects
/// with database entity references and constructor injection.
/// </summary>
/// <remarks>
/// This is the System.Text.Json equivalent of <see cref="JsonConvertExtensions"/>.
/// Provides identical functionality for AOT compatibility.
/// </remarks>
public static class JsonSerializerExtensions
{
    /// <summary>
    /// Serialize the given object resolving any properties which are <see cref="DatabaseEntity"/> into pointers using <see cref="DatabaseEntityJsonConverter"/>
    /// </summary>
    /// <param name="value">Object to serialize</param>
    /// <param name="repositoryLocator">Platform database repository locator</param>
    /// <param name="writeIndented">Whether to format the output JSON with indentation</param>
    /// <returns>JSON string representation</returns>
    public static string SerializeObject(
        object value,
        IRDMPPlatformRepositoryServiceLocator repositoryLocator,
        bool writeIndented = false)
    {
        var options = CreateSerializerOptions(repositoryLocator, writeIndented);
        var typeToSerialize = value?.GetType() ?? typeof(object);
        return JsonSerializer.Serialize(value, typeToSerialize, options);
    }

    /// <summary>
    /// Deserializes a string created with <see cref="SerializeObject"/>.  This involves additional areas of functionality
    /// beyond basic JSON:
    ///
    /// <para>1. Any database pointer (e.g. Catalogue|123|guid) will be fetched and returned from the appropriate platform database (referenced by <paramref name="repositoryLocator"/>)</para>
    /// <para>2. Objects do not need a default constructor, instead <see cref="PickAnyConstructorJsonConverter"/> will be used with <paramref name="objectsForConstructingStuffWith"/></para>
    /// <para>3. Any objects implementing <see cref="IPickAnyConstructorFinishedCallback"/> will have <see cref="IPickAnyConstructorFinishedCallback.AfterConstruction"/> called</para>
    /// <para>4. Dictionaries with complex keys are supported via array serialization</para>
    /// </summary>
    /// <param name="value">JSON string to deserialize</param>
    /// <param name="type">Type to deserialize to</param>
    /// <param name="repositoryLocator">Platform database repository locator</param>
    /// <param name="objectsForConstructingStuffWith">Additional objects to pass to constructors</param>
    /// <returns>Deserialized object</returns>
    public static object DeserializeObject(
        string value,
        Type type,
        IRDMPPlatformRepositoryServiceLocator repositoryLocator,
        params object[] objectsForConstructingStuffWith)
    {
        var options = CreateDeserializerOptions(repositoryLocator, objectsForConstructingStuffWith);
        return JsonSerializer.Deserialize(value, type, options);
    }

    /// <summary>
    /// Deserializes a string to a strongly-typed object
    /// </summary>
    public static T DeserializeObject<T>(
        string value,
        IRDMPPlatformRepositoryServiceLocator repositoryLocator,
        params object[] objectsForConstructingStuffWith)
    {
        var options = CreateDeserializerOptions(repositoryLocator, objectsForConstructingStuffWith);
        return JsonSerializer.Deserialize<T>(value, options);
    }

    /// <summary>
    /// Creates serializer options configured with RDMP-specific converters
    /// </summary>
    private static JsonSerializerOptions CreateSerializerOptions(
        IRDMPPlatformRepositoryServiceLocator repositoryLocator,
        bool writeIndented)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNameCaseInsensitive = true
        };

        // Add custom converters
        options.Converters.Add(new DatabaseEntityJsonConverter(repositoryLocator));
        options.Converters.Add(new DictionaryAsArrayConverterFactory());
        options.Converters.Add(new TypeJsonConverterFactory());
        options.Converters.Add(new AttributeJsonConverterFactory());

        return options;
    }

    /// <summary>
    /// Creates deserializer options configured with RDMP-specific converters
    /// </summary>
    private static JsonSerializerOptions CreateDeserializerOptions(
        IRDMPPlatformRepositoryServiceLocator repositoryLocator,
        object[] objectsForConstructingStuffWith)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        // Add custom converters
        options.Converters.Add(new DatabaseEntityJsonConverter(repositoryLocator));
        options.Converters.Add(new PickAnyConstructorJsonConverter(
            new[] { repositoryLocator }.Union(objectsForConstructingStuffWith).ToArray()));
        options.Converters.Add(new DictionaryAsArrayConverterFactory());
        options.Converters.Add(new TypeJsonConverterFactory());
        options.Converters.Add(new AttributeJsonConverterFactory());

        return options;
    }
}
