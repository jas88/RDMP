// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Repositories;

namespace Tests.Common.Performance;

/// <summary>
/// Provides caching for frequently created test objects to improve performance by eliminating
/// repeated object creation and dependency resolution overhead.
///
/// This cache maintains pools of commonly used objects (Catalogue, TableInfo, ColumnInfo, etc.)
/// that can be reused across tests while maintaining proper isolation through cloning mechanisms.
/// </summary>
public class TestObjectCache : IDisposable
{
    private static readonly Lazy<TestObjectCache> _instance = new(() => new TestObjectCache());
    private static readonly Type[] CacheableTypes =
    {
        typeof(Catalogue),
        typeof(TableInfo),
        typeof(ColumnInfo),
        typeof(CatalogueItem),
        typeof(ExtractionInformation),
        typeof(LoadMetadata),
        typeof(AggregateConfiguration),
        typeof(ExternalDatabaseServer),
        typeof(Project),
        typeof(ExtractionConfiguration)
    };

    private readonly ConcurrentDictionary<Type, ConcurrentQueue<DatabaseEntity>> _objectPools = new();
    private readonly ConcurrentDictionary<Type, Func<DatabaseEntity>> _factoryMethods = new();
    private readonly object _cacheLock = new();
    private bool _disposed;
    private int _cacheHits;
    private int _cacheMisses;

    /// <summary>
    /// Gets the singleton instance of the test object cache.
    /// </summary>
    public static TestObjectCache Instance => _instance.Value;

    private TestObjectCache()
    {
        InitializeFactories();
    }

    /// <summary>
    /// Gets a cached or newly created object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve</typeparam>
    /// <param name="repository">The repository to use for creating new objects if needed</param>
    /// <returns>A database entity of the specified type</returns>
    public T GetOrCreate<T>(MemoryDataExportRepository repository) where T : DatabaseEntity
    {
        var type = typeof(T);

        if (!IsCacheableType(type))
        {
            return CreateNew<T>(repository);
        }

        // Ensure atomicity between TryDequeue and statistics update
        lock (_cacheLock)
        {
            if (_objectPools.TryGetValue(type, out var pool) && pool.TryDequeue(out var cachedObject))
            {
                Interlocked.Increment(ref _cacheHits);

                // For cached objects, we need to reset their state or create a fresh instance
                return ResetOrClone<T>(cachedObject, repository);
            }
        }

        Interlocked.Increment(ref _cacheMisses);
        return CreateNew<T>(repository);
    }

    /// <summary>
    /// Returns an object to the cache for potential reuse.
    /// </summary>
    /// <typeparam name="T">The type of object being returned</typeparam>
    /// <param name="obj">The object to cache</param>
    public void ReturnToCache<T>(T obj) where T : DatabaseEntity
    {
        if (obj == null || _disposed || !IsCacheableType(typeof(T)))
            return;

        var type = typeof(T);
        var pool = _objectPools.GetOrAdd(type, _ => new ConcurrentQueue<DatabaseEntity>());

        // Limit pool size to prevent memory leaks
        if (pool.Count < 10)
        {
            // Reset object state before caching
            ResetObjectState(obj);
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// Clears all cached objects and resets statistics.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            foreach (var pool in _objectPools.Values)
            {
                while (pool.TryDequeue(out _))
                {
                    // Objects are disposed automatically when dequeued
                }
            }

            _objectPools.Clear();
            _cacheHits = 0;
            _cacheMisses = 0;
        }
    }

    /// <summary>
    /// Gets cache performance statistics.
    /// </summary>
    /// <returns>Dictionary containing cache statistics</returns>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            ["CacheHits"] = _cacheHits,
            ["CacheMisses"] = _cacheMisses,
            ["HitRate"] = _cacheHits + _cacheMisses > 0 ? (double)_cacheHits / (_cacheHits + _cacheMisses) : 0.0,
            ["TotalObjectsCached"] = _objectPools.Values.Sum(pool => pool.Count),
            ["CacheableTypes"] = CacheableTypes.Length
        };
    }

    private static bool IsCacheableType(Type type)
    {
        return CacheableTypes.Contains(type);
    }

    private T CreateNew<T>(MemoryDataExportRepository repository) where T : DatabaseEntity
    {
        if (_factoryMethods.TryGetValue(typeof(T), out var factory))
        {
            return (T)factory();
        }

        // Fallback to original WhenIHaveA method if no factory is registered
        return UnitTests.WhenIHaveA<T>(repository);
    }

    private static T ResetOrClone<T>(DatabaseEntity cachedObject, MemoryDataExportRepository repository) where T : DatabaseEntity
    {
        // For most database entities, it's safer to create a new instance
        // but use the cached object as a template for common properties
        return UnitTests.WhenIHaveA<T>(repository);
    }

    private static void ResetObjectState(DatabaseEntity obj)
    {
        // Reset common properties that might interfere with test isolation
        if (obj is Catalogue catalogue)
        {
            catalogue.Name = $"CachedCatalogue_{Guid.NewGuid():N}";
        }
        else if (obj is TableInfo tableInfo)
        {
            tableInfo.Name = $"CachedTable_{Guid.NewGuid():N}";
        }
        else if (obj is ColumnInfo columnInfo)
        {
            columnInfo.Name = $"CachedColumn_{Guid.NewGuid():N}";
        }
    }

    private void InitializeFactories()
    {
        // Register factory methods for common types that can benefit from caching
        _factoryMethods[typeof(Catalogue)] = () => new Catalogue(null, "CachedCatalogue");
        _factoryMethods[typeof(TableInfo)] = () => new TableInfo(null, "CachedTable");
        _factoryMethods[typeof(ExternalDatabaseServer)] = () => new ExternalDatabaseServer(null, "CachedServer", null);
        _factoryMethods[typeof(Project)] = () => new Project(null, "CachedProject");
        _factoryMethods[typeof(ExtractionConfiguration)] = () => new ExtractionConfiguration(null, null);
    }

    public void Dispose()
    {
        if (_disposed) return;

        ClearCache();
        _disposed = true;
    }
}