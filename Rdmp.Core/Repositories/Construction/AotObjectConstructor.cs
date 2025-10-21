// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.ImportExport;
using Rdmp.Core.MapsDirectlyToDatabaseTable;

namespace Rdmp.Core.Repositories.Construction;

/// <summary>
/// AOT-compatible object constructor that provides the same API as ObjectConstructor
/// but uses compiled delegates instead of reflection when available.
/// Falls back to the original reflection-based ObjectConstructor for types
/// that don't have AOT factories registered.
/// </summary>
public static class AotObjectConstructor
{
    private static readonly object _initLock = new();
    private static volatile bool _isInitialized = false;

    private const BindingFlags TargetBindingFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Ensures the AOT factory registry is initialized
    /// </summary>
    private static void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            lock (_initLock)
            {
                if (!_isInitialized)
                {
                    AotObjectFactoryRegistry.Initialize();
                    _isInitialized = true;
                }
            }
        }
    }

    /// <summary>
    /// Gets statistics about AOT vs reflection usage
    /// </summary>
    internal static AotUsageStatistics GetUsageStatistics()
    {
        EnsureInitialized();
        return AotUsageStatistics.Instance;
    }

    /// <summary>
    /// Constructs a new instance of Type t using the blank constructor
    /// </summary>
    /// <param name="t">The type to construct</param>
    /// <returns>A new instance of the specified type</returns>
    public static object Construct(Type t)
    {
        EnsureInitialized();

        // Try AOT factory first
        if (AotObjectFactoryRegistry.TryCreate(t, null, out var aotResult))
        {
            AotUsageStatistics.Instance.RecordAotUsage();
            return aotResult;
        }

        // Fall back to reflection
        AotUsageStatistics.Instance.RecordReflectionUsage();
        return ObjectConstructor.Construct(t);
    }

    /// <summary>
    /// Constructs a new instance of Type t using the default constructor or one that takes an IRDMPPlatformRepositoryServiceLocator
    /// </summary>
    /// <param name="t">The type to construct</param>
    /// <param name="serviceLocator">The service locator parameter</param>
    /// <param name="allowBlank">Whether to allow blank constructors</param>
    /// <returns>A new instance of the specified type</returns>
    public static object Construct(Type t, IRDMPPlatformRepositoryServiceLocator serviceLocator, bool allowBlank = true)
    {
        return Construct<IRDMPPlatformRepositoryServiceLocator>(t, serviceLocator, allowBlank);
    }

    /// <summary>
    /// Constructs a new instance of Type t using the default constructor or one that takes an ICatalogueRepository
    /// </summary>
    /// <param name="t">The type to construct</param>
    /// <param name="catalogueRepository">The catalogue repository parameter</param>
    /// <param name="allowBlank">Whether to allow blank constructors</param>
    /// <returns>A new instance of the specified type</returns>
    public static object Construct(Type t, ICatalogueRepository catalogueRepository, bool allowBlank = true)
    {
        return Construct<ICatalogueRepository>(t, catalogueRepository, allowBlank);
    }

    /// <summary>
    /// Constructs a new instance of Type objectType by invoking the constructor MyClass(IRepository x, DbDataReader r)
    /// </summary>
    /// <typeparam name="T">The repository type</typeparam>
    /// <param name="objectType">The type to construct</param>
    /// <param name="repositoryOfTypeT">The repository parameter</param>
    /// <param name="reader">The data reader parameter</param>
    /// <returns>A new instance that implements IMapsDirectlyToDatabaseTable</returns>
    public static IMapsDirectlyToDatabaseTable ConstructIMapsDirectlyToDatabaseObject<T>(Type objectType,
        T repositoryOfTypeT, DbDataReader reader) where T : IRepository
    {
        EnsureInitialized();

        // Try AOT factory first
        if (AotObjectFactoryRegistry.TryCreate(objectType, new object[] { repositoryOfTypeT, reader }, out var aotResult))
        {
            AotUsageStatistics.Instance.RecordAotUsage();
            return (IMapsDirectlyToDatabaseTable)aotResult;
        }

        // Fall back to reflection
        AotUsageStatistics.Instance.RecordReflectionUsage();
        return ObjectConstructor.ConstructIMapsDirectlyToDatabaseObject(objectType, repositoryOfTypeT, reader);
    }

    /// <summary>
    /// Constructs an instance of object of Type 'typeToConstruct' which should have a compatible constructor taking an object or interface compatible with T
    /// or a blank constructor (optionally)
    /// </summary>
    /// <typeparam name="T">The parameter type expected to be in the constructor</typeparam>
    /// <param name="typeToConstruct">The type to construct an instance of</param>
    /// <param name="constructorParameter1">A value to feed into the compatible constructor found for Type typeToConstruct in order to produce an instance</param>
    /// <param name="allowBlank">True to allow calling the blank constructor if no matching constructor is found that takes a T</param>
    /// <returns>A new instance of the specified type</returns>
    public static object Construct<T>(Type typeToConstruct, T constructorParameter1, bool allowBlank = true)
    {
        EnsureInitialized();

        // Try AOT factory first
        if (AotObjectFactoryRegistry.TryCreate(typeToConstruct, new object[] { constructorParameter1 }, out var aotResult))
        {
            AotUsageStatistics.Instance.RecordAotUsage();
            return aotResult;
        }

        // Fall back to reflection
        AotUsageStatistics.Instance.RecordReflectionUsage();
        return ObjectConstructor.Construct(typeToConstruct, constructorParameter1, allowBlank);
    }

    /// <summary>
    /// Returns all constructors defined for class 'type' that are compatible with any set or subset of the provided parameters
    /// </summary>
    /// <param name="type">The type to examine</param>
    /// <param name="allowBlankConstructor">Whether to allow blank constructors</param>
    /// <param name="allowPrivate">Whether to allow private constructors</param>
    /// <param name="parameterObjects">Parameters to match against</param>
    /// <returns>Dictionary of compatible constructors with the objects needed to invoke them</returns>
    public static Dictionary<ConstructorInfo, List<object>> GetConstructors(Type type, bool allowBlankConstructor,
        bool allowPrivate, params object[] parameterObjects)
    {
        return ObjectConstructor.GetConstructors(type, allowBlankConstructor, allowPrivate, parameterObjects);
    }

    /// <summary>
    /// Attempts to construct an instance of Type typeToConstruct using the provided constructorValues
    /// </summary>
    /// <param name="typeToConstruct">The type to construct</param>
    /// <param name="constructorValues">Constructor parameter values</param>
    /// <returns>A new instance of the specified type, or null if no compatible constructor was found</returns>
    public static object ConstructIfPossible(Type typeToConstruct, params object[] constructorValues)
    {
        EnsureInitialized();

        // Try AOT factory first
        if (AotObjectFactoryRegistry.TryCreate(typeToConstruct, constructorValues, out var aotResult))
        {
            AotUsageStatistics.Instance.RecordAotUsage();
            return aotResult;
        }

        // Fall back to reflection
        AotUsageStatistics.Instance.RecordReflectionUsage();
        return ObjectConstructor.ConstructIfPossible(typeToConstruct, constructorValues);
    }

    /// <summary>
    /// Returns the most likely constructor for creating new instances of a DatabaseEntity class
    /// </summary>
    /// <param name="type">The type to examine</param>
    /// <returns>The most suitable constructor for the type</returns>
    public static ConstructorInfo GetRepositoryConstructor(Type type)
    {
        return ObjectConstructor.GetRepositoryConstructor(type);
    }

    /// <summary>
    /// Checks if a type has an AOT factory registered
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if an AOT factory is registered, false otherwise</returns>
    public static bool HasAotFactory(Type type)
    {
        EnsureInitialized();
        return AotObjectFactoryRegistry.HasFactory(type);
    }

    /// <summary>
    /// Gets the percentage of constructions that used AOT factories
    /// </summary>
    /// <returns>Percentage of AOT usage (0-100)</returns>
    public static double GetAotUsagePercentage()
    {
        return AotUsageStatistics.Instance.GetAotUsagePercentage();
    }

    /// <summary>
    /// Resets usage statistics (for testing purposes)
    /// </summary>
    public static void ResetStatistics()
    {
        AotUsageStatistics.Instance.Reset();
    }
}

/// <summary>
/// Tracks usage statistics for AOT vs reflection object construction
/// </summary>
internal sealed class AotUsageStatistics
{
    private static readonly AotUsageStatistics _instance = new();
    public static AotUsageStatistics Instance => _instance;

    private long _aotUsageCount = 0;
    private long _reflectionUsageCount = 0;
    private readonly object _lock = new();

    private AotUsageStatistics() { }

    /// <summary>
    /// Records usage of AOT factory
    /// </summary>
    public void RecordAotUsage()
    {
        lock (_lock)
        {
            _aotUsageCount++;
        }
    }

    /// <summary>
    /// Records usage of reflection-based construction
    /// </summary>
    public void RecordReflectionUsage()
    {
        lock (_lock)
        {
            _reflectionUsageCount++;
        }
    }

    /// <summary>
    /// Gets the total number of constructions
    /// </summary>
    public long TotalConstructions
    {
        get
        {
            lock (_lock)
            {
                return _aotUsageCount + _reflectionUsageCount;
            }
        }
    }

    /// <summary>
    /// Gets the number of AOT constructions
    /// </summary>
    public long AotConstructions
    {
        get
        {
            lock (_lock)
            {
                return _aotUsageCount;
            }
        }
    }

    /// <summary>
    /// Gets the number of reflection constructions
    /// </summary>
    public long ReflectionConstructions
    {
        get
        {
            lock (_lock)
            {
                return _reflectionUsageCount;
            }
        }
    }

    /// <summary>
    /// Gets the percentage of constructions that used AOT factories
    /// </summary>
    /// <returns>Percentage of AOT usage (0-100)</returns>
    public double GetAotUsagePercentage()
    {
        lock (_lock)
        {
            var total = _aotUsageCount + _reflectionUsageCount;
            return total == 0 ? 0.0 : (double)_aotUsageCount / total * 100.0;
        }
    }

    /// <summary>
    /// Resets all statistics
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _aotUsageCount = 0;
            _reflectionUsageCount = 0;
        }
    }
}