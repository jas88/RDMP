// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rdmp.Core.MapsDirectlyToDatabaseTable;

/// <summary>
/// High-performance property accessor cache using compiled expression trees.
/// Provides ~1000-10000x faster property access compared to reflection after initial compilation.
/// Thread-safe with per-type pre-populated FrozenDictionary for optimal read performance.
/// All properties for a type are compiled upfront and stored in a read-optimized frozen dictionary.
/// </summary>
public static class PropertyAccessorCache
{
    // Per-type frozen cache: each Type gets its own FrozenDictionary with all properties pre-populated
    private static readonly ConcurrentDictionary<Type, FrozenDictionary<string, PropertyAccessor>> TypeCaches = new();

    /// <summary>
    /// Gets a cached property accessor for the specified type and property name.
    /// On first access to a type, compiles expression trees for ALL public instance properties.
    /// Subsequent accesses use the frozen dictionary for optimal read performance.
    /// </summary>
    public static PropertyAccessor GetAccessor(Type type, string propertyName)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        // Get or create the frozen cache for this type (pre-populated with all properties)
        var cache = TypeCaches.GetOrAdd(type, BuildFrozenCacheForType);

        // Lookup in the frozen dictionary (optimized for reads)
        if (!cache.TryGetValue(propertyName, out var accessor))
            throw new ArgumentException($"Property '{propertyName}' not found on type '{type.FullName}'");

        return accessor;
    }

    /// <summary>
    /// Gets a cached property accessor for a specific object instance.
    /// </summary>
    public static PropertyAccessor GetAccessor(object obj, string propertyName)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        return GetAccessor(obj.GetType(), propertyName);
    }

    /// <summary>
    /// Builds a frozen dictionary containing PropertyAccessors for all public instance properties of the given type.
    /// This is called once per type and the result is cached permanently.
    /// </summary>
    private static FrozenDictionary<string, PropertyAccessor> BuildFrozenCacheForType(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        return properties.ToFrozenDictionary(
            p => p.Name,
            p => new PropertyAccessor(p)
        );
    }
}

/// <summary>
/// Encapsulates compiled getter and setter delegates for fast property access.
/// </summary>
public sealed class PropertyAccessor
{
    private readonly Func<object, object> _getter;
    private readonly Action<object, object> _setter;
    private readonly PropertyInfo _propertyInfo;

    internal PropertyAccessor(PropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        _getter = CreateGetter(propertyInfo);
        _setter = propertyInfo.CanWrite ? CreateSetter(propertyInfo) : null;
    }

    /// <summary>
    /// Gets the value of the property for the specified object.
    /// </summary>
    public object GetValue(object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        return _getter(obj);
    }

    /// <summary>
    /// Sets the value of the property for the specified object.
    /// </summary>
    public void SetValue(object obj, object value)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (_setter == null)
            throw new InvalidOperationException($"Property '{_propertyInfo.Name}' is read-only");

        _setter(obj, value);
    }

    /// <summary>
    /// Gets the PropertyInfo for this accessor.
    /// </summary>
    public PropertyInfo PropertyInfo => _propertyInfo;

    /// <summary>
    /// Gets the property type.
    /// </summary>
    public Type PropertyType => _propertyInfo.PropertyType;

    private static Func<object, object> CreateGetter(PropertyInfo property)
    {
        // Create: (object obj) => (object)((TInstance)obj).Property
        var objParam = Expression.Parameter(typeof(object), "obj");
        var instanceCast = Expression.Convert(objParam, property.DeclaringType);
        var propertyAccess = Expression.Property(instanceCast, property);
        var resultCast = Expression.Convert(propertyAccess, typeof(object));

        return Expression.Lambda<Func<object, object>>(resultCast, objParam).Compile();
    }

    private static Action<object, object> CreateSetter(PropertyInfo property)
    {
        // Create: (object obj, object value) => ((TInstance)obj).Property = (TProperty)value
        var objParam = Expression.Parameter(typeof(object), "obj");
        var valueParam = Expression.Parameter(typeof(object), "value");

        var instanceCast = Expression.Convert(objParam, property.DeclaringType);
        var propertyAccess = Expression.Property(instanceCast, property);
        var valueCast = Expression.Convert(valueParam, property.PropertyType);
        var assign = Expression.Assign(propertyAccess, valueCast);

        return Expression.Lambda<Action<object, object>>(assign, objParam, valueParam).Compile();
    }
}
