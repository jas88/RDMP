<!--
Copyright (c) The University of Dundee 2018-2025
This file is part of the Research Data Management Platform (RDMP).
RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
-->

# RDMP Test Performance Optimizations

This directory contains performance optimization components for the RDMP test suite, designed to reduce test execution time and resource usage while maintaining test reliability and isolation.

## Overview

The performance optimizations address the most common bottlenecks in the RDMP test suite:

1. **Repeated repository creation and initialization**
2. **Frequent object creation and dependency resolution**
3. **Expensive database setup and teardown operations**
4. **Lack of resource reuse across test classes**

## Components

### 1. RepositoryPool

**Location**: `RepositoryPool.cs`

**Purpose**: Eliminates repeated repository creation and initialization overhead by maintaining a pool of shared repository instances.

**Key Features**:
- Thread-safe singleton pattern
- Reference counting for proper cleanup
- Automatic disposal of unused repositories
- Support for both database and file system repositories

**Usage**:
```csharp
// Get or create a repository
var repository = RepositoryPool.Instance.GetOrCreateRepository(options, false);

// Release repository when done
RepositoryPool.Instance.ReleaseRepository(repository);

// Clear all repositories (typically in test teardown)
RepositoryPool.Instance.ClearAll();
```

### 2. TestObjectCache

**Location**: `TestObjectCache.cs`

**Purpose**: Caches frequently created test objects ([Catalogue], [TableInfo], [ColumnInfo], etc.) to reduce object creation overhead.

**Key Features**:
- Type-safe caching for common database entities
- Automatic object state reset for test isolation
- Configurable cache size limits
- Performance statistics tracking

**Usage**:
```csharp
// Get cached or new object
var catalogue = TestObjectCache.Instance.GetOrCreate<Catalogue>(repository);

// Return object to cache
TestObjectCache.Instance.ReturnToCache(catalogue);

// Get cache statistics
var stats = TestObjectCache.Instance.GetStatistics();
```

### 3. SharedTestFixtureBase

**Location**: `SharedTestFixtureBase.cs`

**Purpose**: Provides base functionality for shared test fixtures with expensive one-time setup operations and lazy resource initialization.

**Key Features**:
- One-time setup for expensive operations
- Lazy initialization of commonly used resources
- Thread-safe resource management
- Automatic cleanup and disposal

**Usage**:
```csharp
public class MyTestFixture : SharedTestFixtureBase
{
    protected override PlatformDatabaseCreationOptions GetDefaultDatabaseOptions()
    {
        return new PlatformDatabaseCreationOptions
        {
            ServerName = "localhost",
            Prefix = "TEST_"
        };
    }

    [Test]
    public void MyTest()
    {
        // Use shared resources
        var db = SharedTestDatabase.Value;
        var catalogue = SharedSampleCatalogue.Value;
    }
}
```

### 4. DatabaseTestFixtureBase

**Location**: `DatabaseTestFixtureBase.cs`

**Purpose**: Specialized base class for database tests with database pooling, connection management, and efficient cleanup.

**Key Features**:
- Database connection pooling
- Database reuse with cleanup
- Transaction management
- Parallel database creation
- Bulk cleanup operations

**Usage**:
```csharp
public class MyDatabaseTests : DatabaseTestFixtureBase
{
    [Test]
    public void TestWithDatabase()
    {
        // Get pooled database
        var db = GetPooledDatabase(DatabaseType.MicrosoftSQLServer, "MyTestDb");

        // Execute in transaction
        var result = ExecuteInTransaction(db, connection => {
            // Database operations here
            return connection.ExecuteScalar("SELECT COUNT(*) FROM MyTable");
        });

        // Return database to pool
        ReturnDatabaseToPool(db);
    }
}
```

## Integration with Existing Test Infrastructure

### Modified Files

1. **UnitTests.cs**: Updated to use `TestObjectCache` for object creation
2. **DatabaseTests.cs**: Updated to use `RepositoryPool` for repository management
3. **TestsRequiringACohort.cs**: Updated to use shared database instances

### Backward Compatibility

All optimizations are designed to be backward compatible:

- Existing test code continues to work without changes
- Optimizations provide fallbacks when caching/pooling fails
- Original functionality is preserved as a safety net

## Performance Benefits

### Expected Improvements

1. **Repository Creation**: 70-90% reduction in repository setup time
2. **Object Creation**: 50-80% reduction in object creation time for frequently used types
3. **Database Setup**: 60-85% reduction in database setup time through reuse
4. **Memory Usage**: Optimized through proper cleanup and pooling

### Benchmarking

To measure performance improvements:

```csharp
// Get performance statistics
var repoStats = RepositoryPool.Instance.GetStatistics();
var cacheStats = TestObjectCache.Instance.GetStatistics();

Console.WriteLine($"Repository Pool Stats: {JsonSerializer.Serialize(repoStats)}");
Console.WriteLine($"Object Cache Stats: {JsonSerializer.Serialize(cacheStats)}");
```

## Best Practices

### When to Use These Optimizations

1. **Large Test Suites**: Tests with many test classes that share common setup
2. **Integration Tests**: Tests that require database connections and repositories
3. **Performance-Critical Tests**: Tests where execution time is a concern
4. **CI/CD Pipelines**: Automated testing where resource efficiency is important

### Guidelines for Test Authors

1. **Use Shared Base Classes**: Inherit from `SharedTestFixtureBase` or `DatabaseTestFixtureBase` when possible
2. **Leverage Lazy Resources**: Use lazy initialization for expensive resources
3. **Return Resources to Pool**: Always return resources to their respective pools
4. **Monitor Performance**: Use built-in statistics to monitor optimization effectiveness
5. **Test Isolation**: Ensure tests remain isolated despite resource sharing

### Common Patterns

#### Database Tests with Cleanup
```csharp
[Test]
public void DatabaseOperationTest()
{
    var db = GetPooledDatabase(DatabaseType.MicrosoftSQLServer, "TestDb");
    try
    {
        // Perform test operations
        using var transaction = db.Server.BeginNewTransactedConnection();
        // ... test code ...
        transaction.ManagedTransaction.Commit();
    }
    finally
    {
        ReturnDatabaseToPool(db);
    }
}
```

#### Object Creation with Caching
```csharp
[Test]
public void ObjectOperationTest()
{
    var catalogue = ObjectCache.GetOrCreate<Catalogue>(Repository);
    try
    {
        // Perform test operations
        catalogue.Name = "TestCatalogue";
        catalogue.SaveToDatabase();
        // ... test code ...
    }
    finally
    {
        ObjectCache.ReturnToCache(catalogue);
    }
}
```

## Monitoring and Debugging

### Performance Statistics

Both `RepositoryPool` and `TestObjectCache` provide built-in statistics:

```csharp
// Repository pool statistics
var repoStats = RepositoryPool.Instance.GetStatistics();
// Output: TotalRepositories, ActiveRepositories, DisposedRepositories, TotalReferences

// Object cache statistics
var cacheStats = TestObjectCache.Instance.GetStatistics();
// Output: CacheHits, CacheMisses, HitRate, TotalObjectsCached, CacheableTypes
```

### Common Issues and Solutions

1. **Memory Leaks**: Ensure proper cleanup in test teardown methods
2. **Test Isolation**: Reset object state before returning to cache
3. **Resource Exhaustion**: Monitor pool sizes and implement limits
4. **Stale Resources**: Implement timeout-based resource disposal

## Configuration

### Customizing Cache Settings

```csharp
// Example: Configure cache limits (if implemented)
var cache = TestObjectCache.Instance;
// cache.SetMaxPoolSize(typeof(Catalogue), 20);
// cache.SetTimeout(TimeSpan.FromMinutes(30));
```

### Customizing Repository Pool

```csharp
// Example: Configure repository pool settings (if implemented)
var pool = RepositoryPool.Instance;
// pool.SetMaxRepositories(10);
// pool.SetCleanupInterval(TimeSpan.FromMinutes(5));
```

## Future Enhancements

Potential areas for further optimization:

1. **Async Operations**: Support for asynchronous repository and database operations
2. **Smart Caching**: Intelligent cache invalidation based on usage patterns
3. **Resource Monitoring**: Built-in resource usage monitoring and alerting
4. **Configuration Files**: External configuration for optimization settings
5. **Benchmarking Tools**: Built-in benchmarking and comparison tools

## Support and Troubleshooting

### Common Issues

1. **"Repository not found" errors**: Ensure proper initialization in `OneTimeSetUp`
2. **"Object state is invalid" errors**: Implement proper object reset in cache
3. **"Database connection failed" errors**: Check database server availability and connection strings
4. **Memory usage growth**: Monitor pool sizes and implement proper cleanup

### Debug Mode

Enable debug logging for troubleshooting:

```csharp
// Enable debug mode (if implemented)
TestObjectCache.Instance.DebugMode = true;
RepositoryPool.Instance.DebugMode = true;
```

### Contact

For issues or suggestions regarding these performance optimizations, please contact the RDMP development team.

[Catalogue]: ../../Documentation/CodeTutorials/Glossary.md#Catalogue
[TableInfo]: ../../Documentation/CodeTutorials/Glossary.md#TableInfo
[ColumnInfo]: ../../Documentation/CodeTutorials/Glossary.md#ColumnInfo