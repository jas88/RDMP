# AOT Object Factory API Guide

## Overview

This guide provides comprehensive documentation for the AOT-friendly Object Factory API, which replaces the legacy ObjectConstructor system with improved performance, type safety, and AOT compilation support.

## Table of Contents

1. [Introduction](#introduction)
2. [Core API Overview](#core-api-overview)
3. [Object Construction Methods](#object-construction-methods)
4. [Constructor Resolution](#constructor-resolution)
5. [Type Registry Integration](#type-registry-integration)
6. [Performance Considerations](#performance-considerations)
7. [Advanced Usage Patterns](#advanced-usage-patterns)
8. [Migration from ObjectConstructor](#migration-from-objectconstructor)
9. [API Reference](#api-reference)

## Introduction

The AOT Object Factory provides:

- **AOT-friendly design**: No runtime code generation
- **High performance**: Optimized constructor resolution
- **Type safety**: Compile-time validation where possible
- **Flexibility**: Supports complex constructor patterns
- **Backward compatibility**: Works with existing RDMP code

### Key Features

- Automatic constructor parameter resolution
- Support for inheritance and interfaces
- Fallback to blank constructors
- Constructor selection with attributes
- Thread-safe operation
- Comprehensive error handling

## Core API Overview

### Main Classes

- **ObjectConstructor** - Primary object construction API
- **MEF** - Type lookup and registry integration
- **UseWithObjectConstructorAttribute** - Constructor selection hints
- **ObjectLacksCompatibleConstructorException** - Construction error handling

### Namespace Structure

```csharp
using Rdmp.Core.Repositories.Construction;
using Rdmp.Core.Repositories;
```

## Object Construction Methods

### Basic Construction

#### Blank Constructor

```csharp
// Construct with parameterless constructor
var instance = ObjectConstructor.Construct(typeof(MyClass));

// Generic version
var instance = ObjectConstructor.Construct<MyClass>();
```

#### Single Parameter Constructor

```csharp
// Construct with repository parameter
var instance = ObjectConstructor.Construct(typeof(MyClass), repository);

// Allow fallback to blank constructor
var instance = ObjectConstructor.Construct(typeof(MyClass), repository, allowBlank: true);

// Disallow fallback (throws if no matching constructor)
var instance = ObjectConstructor.Construct(typeof(MyClass), repository, allowBlank: false);
```

#### Database Entity Construction

```csharp
// Construct database entities with reader
var entity = ObjectConstructor.ConstructIMapsDirectlyToDatabaseObject<Catalogue>(
    typeof(Catalogue), repository, dataReader);

// Generic version
var entity = ObjectConstructor.ConstructIMapsDirectlyToDatabaseObject(
    typeof(Catalogue), repository, dataReader);
```

#### Flexible Construction

```csharp
// Construct with any compatible parameters
var parameters = new object[] { "string", 42, true };
var instance = ObjectConstructor.ConstructIfPossible(typeof(MyClass), parameters);

// Returns null if no compatible constructor found
```

### Constructor Analysis

#### Get Available Constructors

```csharp
// Get all compatible constructors
var constructors = ObjectConstructor.GetConstructors(
    type: typeof(MyClass),
    allowBlankConstructor: true,
    allowPrivate: false,
    parameterObjects: new object[] { repository, "parameter" }
);

// Returns: Dictionary<ConstructorInfo, List<object>>
```

#### Get Repository Constructor

```csharp
// Get the preferred constructor for database entities
var constructor = ObjectConstructor.GetRepositoryConstructor(typeof(Catalogue));

// Returns: ConstructorInfo
```

## Constructor Resolution

### Resolution Algorithm

The ObjectConstructor follows this resolution algorithm:

1. **Exact Type Match**: Find constructor with exact parameter type matches
2. **Assignable Type Match**: Find constructor with compatible parameter types
3. **Attribute Preference**: Prefer constructors marked with `[UseWithObjectConstructor]`
4. **Single Winner**: Select the best match, throw if multiple equally good matches
5. **Fallback**: Use blank constructor if allowed

### Constructor Selection Examples

#### Single Constructor

```csharp
public class SimpleClass
{
    public SimpleClass(ICatalogueRepository repository)
    {
        Repository = repository;
    }
}

// Resolution: Direct match
var instance = ObjectConstructor.Construct(typeof(SimpleClass), repository);
```

#### Multiple Constructors

```csharp
public class ComplexClass
{
    [UseWithObjectConstructor] // Preferred constructor
    public ComplexClass(ICatalogueRepository repository, ILogManager logger)
    {
        Repository = repository;
        Logger = logger;
    }

    public ComplexClass(ICatalogueRepository repository)
    {
        Repository = repository;
        Logger = new DefaultLogManager();
    }

    public ComplexClass()
    {
        Repository = null;
        Logger = new DefaultLogManager();
    }
}

// Resolution: Uses decorated constructor when possible
var instance = ObjectConstructor.Construct(typeof(ComplexClass), repository, logger);

// Falls back to single-parameter constructor
var instance2 = ObjectConstructor.Construct(typeof(ComplexClass), repository);

// Falls back to blank constructor if allowed
var instance3 = ObjectConstructor.Construct(typeof(ComplexClass), null, allowBlank: true);
```

#### Inheritance Support

```csharp
public class BaseClass
{
    public BaseClass(IRepository repository) { }
}

public class DerivedClass : BaseClass
{
    public DerivedClass(ICatalogueRepository repository) : base(repository) { }
}

// Resolution: Works with inheritance
var instance = ObjectConstructor.Construct(typeof(DerivedClass), catalogueRepository);
```

### Error Handling

#### Common Exceptions

```csharp
try
{
    var instance = ObjectConstructor.Construct(typeof(MyClass), incompatibleParameter);
}
catch (ObjectLacksCompatibleConstructorException ex)
{
    Console.WriteLine($"No compatible constructor found: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

#### Safe Construction Pattern

```csharp
public static T SafeConstruct<T>(Type type, params object[] parameters) where T : class
{
    try
    {
        return ObjectConstructor.ConstructIfPossible(type, parameters) as T;
    }
    catch (ObjectLacksCompatibleConstructorException)
    {
        return null;
    }
}
```

## Type Registry Integration

### MEF Integration

The ObjectFactory integrates seamlessly with the MEF type registry:

```csharp
// Get type from registry
var type = MEF.GetType("MyNamespace.MyClass");
if (type != null)
{
    // Construct using ObjectFactory
    var instance = ObjectConstructor.Construct(type, repository);
}
```

### Generic Type Resolution

```csharp
// Get generic implementations
var handlerTypes = MEF.GetGenericTypes(typeof(IDataHandler<>), typeof(string));
foreach (var handlerType in handlerTypes)
{
    var handler = ObjectConstructor.Construct(handlerType, additionalParams);
}
```

### Type-safe Construction

```csharp
public static T CreatePlugin<T>(string typeName, params object[] args)
{
    var type = MEF.GetType(typeName);
    if (type == null)
        throw new InvalidOperationException($"Type '{typeName}' not found");

    if (!typeof(T).IsAssignableFrom(type))
        throw new InvalidOperationException($"Type '{typeName}' does not implement {typeof(T).Name}");

    return MEF.CreateA<T>(typeName, args);
}
```

## Performance Considerations

### Caching

The ObjectFactory automatically caches constructor information:

```csharp
// First call - performs reflection
var instance1 = ObjectConstructor.Construct(typeof(MyClass), repository);

// Subsequent calls - uses cached constructor info
var instance2 = ObjectConstructor.Construct(typeof(MyClass), repository);
```

### Best Practices

1. **Reuse type instances**: Don't repeatedly call `typeof()` in loops
2. **Batch constructions**: Group similar operations
3. **Avoid excessive parameters**: Keep constructor signatures simple
4. **Use appropriate fallbacks**: Configure `allowBlank` correctly

```csharp
// Good - cached type info
private static readonly Type MyPluginType = typeof(MyPlugin);

// Bad - repeated reflection
for (int i = 0; i < 100; i++)
{
    var type = Type.GetType("MyNamespace.MyPlugin"); // Slow!
    var instance = ObjectConstructor.Construct(type);
}
```

### Performance Monitoring

```csharp
public static T TimedConstruction<T>(Type type, params object[] parameters)
{
    var sw = Stopwatch.StartNew();
    var instance = ObjectConstructor.ConstructIfPossible(type, parameters);
    sw.Stop();

    if (sw.ElapsedMilliseconds > 100)
    {
        Console.WriteLine($"Slow construction: {type.Name} took {sw.ElapsedMilliseconds}ms");
    }

    return (T)instance;
}
```

## Advanced Usage Patterns

### Factory Pattern Implementation

```csharp
public class PluginFactory
{
    private readonly ICatalogueRepository _repository;
    private readonly Dictionary<string, Type> _typeCache = new();

    public PluginFactory(ICatalogueRepository repository)
    {
        _repository = repository;
    }

    public T CreatePlugin<T>(string typeName, params object[] additionalParams)
    {
        var type = GetCachedType(typeName);
        var parameters = new[] { _repository }.Concat(additionalParams).ToArray();

        return ObjectConstructor.ConstructIfPossible(type, parameters) as T
               ?? throw new InvalidOperationException($"Failed to create plugin {typeName}");
    }

    private Type GetCachedType(string typeName)
    {
        if (!_typeCache.TryGetValue(typeName, out var type))
        {
            type = MEF.GetType(typeName)
                   ?? throw new InvalidOperationException($"Type '{typeName}' not found");
            _typeCache[typeName] = type;
        }
        return type;
    }
}
```

### Dependency Injection Container

```csharp
public class SimpleContainer
{
    private readonly Dictionary<Type, object> _singletons = new();
    private readonly Dictionary<Type, Type> _registrations = new();

    public void Register<TInterface, TImplementation>() where TImplementation : TInterface
    {
        _registrations[typeof(TInterface)] = typeof(TImplementation);
    }

    public T Resolve<T>()
    {
        if (_singletons.TryGetValue(typeof(T), out var singleton))
            return (T)singleton;

        if (_registrations.TryGetValue(typeof(T), out var implementationType))
        {
            var instance = ObjectConstructor.Construct(implementationType, this);
            _singletons[typeof(T)] = instance;
            return (T)instance;
        }

        throw new InvalidOperationException($"Type {typeof(T).Name} not registered");
    }
}
```

### Plugin Loader

```csharp
public class PluginLoader
{
    private readonly ICatalogueRepository _repository;

    public PluginLoader(ICatalogueRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<T> LoadPlugins<T>()
    {
        var pluginTypes = MEF.GetTypes<T>();
        var plugins = new List<T>();

        foreach (var type in pluginTypes)
        {
            try
            {
                var plugin = ObjectConstructor.Construct(type, _repository) as T;
                if (plugin != null)
                {
                    plugins.Add(plugin);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load plugin {type.Name}: {ex.Message}");
            }
        }

        return plugins;
    }
}
```

### Constructor Validation

```csharp
public static class ConstructorValidator
{
    public static bool ValidateConstructor<T>(params object[] parameters)
    {
        return ValidateConstructor(typeof(T), parameters);
    }

    public static bool ValidateConstructor(Type type, params object[] parameters)
    {
        var constructors = ObjectConstructor.GetConstructors(type, false, false, parameters);
        return constructors.Any();
    }

    public static ConstructorInfo GetBestConstructor<T>(params object[] parameters)
    {
        var constructors = ObjectConstructor.GetConstructors(typeof(T), false, false, parameters);
        return constructors.Keys.FirstOrDefault();
    }
}
```

## Migration from ObjectConstructor

### API Changes

| Old API | New API | Notes |
|---------|---------|-------|
| `new ObjectConstructor()` | `ObjectConstructor` (static) | Static class now |
| `Construct(t, repo)` | `Construct(t, repo)` | Same signature |
| `Construct(repo, t, allowBlank)` | `Construct(t, repo, allowBlank)` | Parameter order changed |
| `ConstructIfPossible(t, args)` | `ConstructIfPossible(t, args)` | Same signature |

### Migration Examples

#### Before (Legacy)
```csharp
var constructor = new ObjectConstructor();
var instance = constructor.Construct(typeof(MyClass), repository, true);
```

#### After (New API)
```csharp
var instance = ObjectConstructor.Construct(typeof(MyClass), repository, allowBlank: true);
```

### Compatibility Layer

For gradual migration, create a compatibility layer:

```csharp
public class LegacyObjectConstructor
{
    public object Construct(Type type, ICatalogueRepository repository, bool allowBlank = true)
    {
        return ObjectConstructor.Construct(type, repository, allowBlank);
    }

    public object ConstructIfPossible(Type type, params object[] args)
    {
        return ObjectConstructor.ConstructIfPossible(type, args);
    }
}
```

## API Reference

### ObjectConstructor Class

#### Static Methods

```csharp
// Basic construction
public static object Construct(Type t)
public static object Construct(Type t, ICatalogueRepository repository, bool allowBlank = true)
public static object Construct(Type t, IRDMPPlatformRepositoryServiceLocator repository, bool allowBlank = true)

// Generic construction
public static T Construct<T>(Type typeToConstruct, T constructorParameter1, bool allowBlank = true)

// Database entity construction
public static IMapsDirectlyToDatabaseTable ConstructIMapsDirectlyToDatabaseObject<T>(
    Type objectType, T repositoryOfTypeT, DbDataReader reader) where T : IRepository

// Flexible construction
public static object ConstructIfPossible(Type typeToConstruct, params object[] constructorValues)

// Constructor analysis
public static Dictionary<ConstructorInfo, List<object>> GetConstructors(
    Type type, bool allowBlankConstructor, bool allowPrivate, params object[] parameterObjects)

public static ConstructorInfo GetRepositoryConstructor(Type type)
```

### UseWithObjectConstructorAttribute

```csharp
[AttributeUsage(AttributeTargets.Constructor)]
public class UseWithObjectConstructorAttribute : Attribute
{
    // Marks the preferred constructor when multiple are available
}
```

### ObjectLacksCompatibleConstructorException

```csharp
public class ObjectLacksCompatibleConstructorException : Exception
{
    public ObjectLacksCompatibleConstructorException(string message) : base(message) { }
    public ObjectLacksCompatibleConstructorException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

### MEF Integration Methods

```csharp
// Type lookup
public static Type GetType(string typeName)
public static Type GetType(string typeName, Type expectedBaseClass)

// Type enumeration
public static IEnumerable<Type> GetTypes<T>()
public static IEnumerable<Type> GetGenericTypes(Type genericType, Type typeOfT)
public static IEnumerable<Type> GetAllTypes()

// Object creation
public static T CreateA<T>(string typeToCreate, params object[] args)

// Utilities
public static string GetCSharpNameForType(Type t)
public static IReadOnlyDictionary<string, Exception> ListBadAssemblies()
```

## Examples and Patterns

### Complete Example: Plugin Management

```csharp
public class PluginManager
{
    private readonly ICatalogueRepository _repository;
    private readonly Dictionary<string, object> _pluginCache = new();

    public PluginManager(ICatalogueRepository repository)
    {
        _repository = repository;
    }

    public T GetPlugin<T>(string pluginName) where T : class
    {
        var cacheKey = $"{typeof(T).Name}:{pluginName}";

        if (_pluginCache.TryGetValue(cacheKey, out var cachedPlugin))
            return (T)cachedPlugin;

        var typeName = $"MyPlugins.{pluginName}";
        var type = MEF.GetType(typeName);

        if (type == null)
            throw new InvalidOperationException($"Plugin '{pluginName}' not found");

        var plugin = ObjectConstructor.Construct(type, _repository) as T
                   ?? throw new InvalidOperationException($"Failed to create plugin '{pluginName}'");

        _pluginCache[cacheKey] = plugin;
        return plugin;
    }

    public void RefreshCache()
    {
        _pluginCache.Clear();
    }

    public IEnumerable<string> ListAvailablePlugins<T>()
    {
        return MEF.GetTypes<T>()
                 .Select(t => t.Name.Replace(typeof(T).Name, ""))
                 .Where(name => !string.IsNullOrEmpty(name));
    }
}
```

### Factory Method Pattern

```csharp
public abstract class DataProcessorFactory
{
    public static IDataProcessor CreateProcessor(string processorType, ICatalogueRepository repository)
    {
        var typeName = $"Rdmp.Core.DataProcessors.{processorType}Processor";
        var type = MEF.GetType(typeName);

        if (type == null)
            throw new ArgumentException($"Unknown processor type: {processorType}");

        return ObjectConstructor.Construct(type, repository) as IDataProcessor
               ?? throw new InvalidOperationException($"Failed to create processor: {processorType}");
    }

    public static bool CanCreateProcessor(string processorType)
    {
        var typeName = $"Rdmp.Core.DataProcessors.{processorType}Processor";
        var type = MEF.GetType(typeName);
        return type != null;
    }
}
```

---

*Last updated: October 2025*
*Version: 2.0.0*