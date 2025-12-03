// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using Rdmp.Core.Repositories;
using Rdmp.Core.Curation.Data.Serialization.SystemTextJson;

namespace Rdmp.Core.Curation.Data.Serialization;

/// <summary>
/// Facilitates the use of <see cref="DatabaseEntityJsonConverter"/> and <see cref="PickAnyConstructorJsonConverter"/>
/// by delegating to System.Text.Json-based serialization.
/// </summary>
/// <remarks>
/// This class now delegates to System.Text.Json for AOT compatibility.
/// Maintains backward compatibility with existing code using Newtonsoft.Json API signatures.
/// </remarks>
public static class JsonConvertExtensions
{
    /// <summary>
    /// Serialize the given object resolving any properties which are <see cref="DatabaseEntity"/> into pointers using <see cref="DatabaseEntityJsonConverter"/>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="repositoryLocator"></param>
    /// <returns></returns>
    public static string SerializeObject(object value, IRDMPPlatformRepositoryServiceLocator repositoryLocator)
    {
        return JsonSerializerExtensions.SerializeObject(value, repositoryLocator);
    }

    /// <summary>
    /// Deserializes a string created with <see cref="SerializeObject(object,IRDMPPlatformRepositoryServiceLocator)"/>.  This involves additional areas of functionality
    /// beyond basic JSON:
    ///
    /// <para>1. Any database pointer (e.g. Catalogue|123|guid) will be fetched and returned from the appropriate platform database (referenced by <paramref name="repositoryLocator"/>)</para>
    /// <para>2. Objects do not need a default constructor, instead <see cref="PickAnyConstructorJsonConverter"/> will be used with <paramref name="objectsForConstructingStuffWith"/></para>
    /// <para>3. Any objects implementing <see cref="IPickAnyConstructorFinishedCallback"/> will have <see cref="IPickAnyConstructorFinishedCallback.AfterConstruction"/> called</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <param name="repositoryLocator"></param>
    /// <param name="objectsForConstructingStuffWith"></param>
    /// <returns></returns>
    public static object DeserializeObject(string value, Type type,
        IRDMPPlatformRepositoryServiceLocator repositoryLocator, params object[] objectsForConstructingStuffWith)
    {
        return JsonSerializerExtensions.DeserializeObject(value, type, repositoryLocator, objectsForConstructingStuffWith);
    }
}