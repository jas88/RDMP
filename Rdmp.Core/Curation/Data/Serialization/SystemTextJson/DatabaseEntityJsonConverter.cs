// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rdmp.Core.Curation.Data.ImportExport;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Repositories;

namespace Rdmp.Core.Curation.Data.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json implementation of database entity serialization.
/// Serializes <see cref="IMapsDirectlyToDatabaseTable"/> objects as references (Type|ID|SharingUID)
/// instead of full object graphs, allowing objects to be resolved from the database.
/// </summary>
/// <remarks>
/// This is the System.Text.Json equivalent of <see cref="DatabaseEntityJsonConverter"/>.
/// Provides identical functionality for AOT compatibility.
/// </remarks>
public class DatabaseEntityJsonConverter : JsonConverter<IMapsDirectlyToDatabaseTable>
{
    private readonly ShareManager _shareManager;

    /// <summary>
    /// Creates a new System.Text.Json serializer for database entities
    /// </summary>
    /// <param name="repositoryLocator">Platform database repository locator</param>
    public DatabaseEntityJsonConverter(IRDMPPlatformRepositoryServiceLocator repositoryLocator)
    {
        _shareManager = new ShareManager(repositoryLocator);
    }

    /// <summary>
    /// Serializes a database entity as a persistence string reference
    /// </summary>
    /// <param name="writer">JSON writer</param>
    /// <param name="value">Database entity to serialize</param>
    /// <param name="options">Serialization options</param>
    public override void Write(Utf8JsonWriter writer, IMapsDirectlyToDatabaseTable value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        // Write the actual concrete type name so we can deserialize interfaces/base classes correctly
        writer.WriteString("$type", value.GetType().Name);
        writer.WriteString("PersistenceString", _shareManager.GetPersistenceString(value));
        writer.WriteEndObject();
    }

    /// <summary>
    /// Deserializes a persistence string reference back to a database entity
    /// </summary>
    /// <param name="reader">JSON reader</param>
    /// <param name="typeToConvert">Type to convert to</param>
    /// <param name="options">Serialization options</param>
    /// <returns>Resolved database entity</returns>
    /// <exception cref="JsonException">Thrown if JSON format is invalid</exception>
    public override IMapsDirectlyToDatabaseTable Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject token, got {reader.TokenType}");

        string typeName = null;
        string persistenceString = null;

        // Read all properties in the object
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");

            var propertyName = reader.GetString();

            // Read the property value
            if (!reader.Read())
                throw new JsonException($"Unexpected end of JSON while reading value for property '{propertyName}'");

            switch (propertyName)
            {
                case "$type":
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException("Expected String token for $type value");
                    typeName = reader.GetString();
                    break;
                case "PersistenceString":
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException("Expected String token for PersistenceString value");
                    persistenceString = reader.GetString();
                    break;
                default:
                    // Skip unknown properties
                    reader.Skip();
                    break;
            }
        }

        if (string.IsNullOrEmpty(persistenceString))
            throw new JsonException("PersistenceString property not found or empty");

        var resolvedObject = _shareManager.GetObjectFromPersistenceString(persistenceString);
        return resolvedObject;
    }

    /// <summary>
    /// This converter can handle any type that implements <see cref="IMapsDirectlyToDatabaseTable"/>
    /// </summary>
    public override bool CanConvert(Type typeToConvert) =>
        typeof(IMapsDirectlyToDatabaseTable).IsAssignableFrom(typeToConvert);
}
