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
/// System.Text.Json converter factory for <see cref="Attribute"/>-derived types.
/// Excludes the inherited <see cref="Attribute.TypeId"/> property which contains a <see cref="Type"/>
/// boxed as <see cref="object"/>, causing serialization issues with the internal type representation.
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
