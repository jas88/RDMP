// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Rdmp.Core.Curation.ANOEngineering;
using Rdmp.Core.Curation.Data.ImportExport;

namespace Rdmp.Core.Curation.Data.Serialization.SystemTextJson;

/// <summary>
/// Source-generated JSON serialization context for RDMP types.
/// Provides AOT-compatible type metadata with IncludeFields enabled.
/// </summary>
/// <remarks>
/// <para>
/// This context is combined with runtime converters via TypeInfoResolverChain:
/// </para>
/// <list type="bullet">
/// <item><see cref="DatabaseEntityJsonConverter"/> for IMapsDirectlyToDatabaseTable references</item>
/// <item><see cref="PickAnyConstructorJsonConverter"/> for objects without default constructors</item>
/// <item><see cref="DictionaryAsArrayConverterFactory"/> for dictionaries with complex keys</item>
/// </list>
/// <para>
/// Database entity types (ColumnInfo, TableInfo, Catalogue, etc.) are handled by
/// DatabaseEntityJsonConverter at runtime, not by this source generator.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    IncludeFields = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(ForwardEngineerANOCataloguePlanManager))]
[JsonSerializable(typeof(ColumnInfoANOPlan))]
[JsonSerializable(typeof(Plan))]
[JsonSerializable(typeof(ExtractionCategory))]
[JsonSerializable(typeof(ExtractionCategory?))]
[JsonSerializable(typeof(ShareDefinition))]
[JsonSerializable(typeof(List<ShareDefinition>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(bool?))]
[JsonSerializable(typeof(object))]
public partial class RdmpJsonSerializerContext : JsonSerializerContext
{
}
