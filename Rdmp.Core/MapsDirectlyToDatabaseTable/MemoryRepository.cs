// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Injection;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Revertable;
using Rdmp.Core.ReusableLibraryCode.Annotations;

namespace Rdmp.Core.MapsDirectlyToDatabaseTable;

/// <summary>
/// Implementation of <see cref="IRepository"/> which creates objects in memory instead of the database.
/// </summary>
public class MemoryRepository : IRepository
{
    protected int NextObjectId;

    public bool SupportsCommits => false;

    /// <summary>
    /// Type-indexed storage for O(1) lookups: Type -> (ID -> Object)
    /// </summary>
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<int, IMapsDirectlyToDatabaseTable>>
        _objectsByType = new();

    /// <summary>
    /// Precomputed type hierarchy: Interface/BaseClass -> ConcreteTypes[]
    /// Lazily built on first interface lookup to avoid IsAssignableFrom() overhead
    /// </summary>
    private FrozenDictionary<Type, Type[]> _typeHierarchy;
    private readonly object _typeHierarchyLock = new();

    /// <summary>
    /// Backward-compatible view of all objects as a concurrent hashset.
    /// Computed on-demand from type-indexed storage. Use sparingly - prefer type-specific methods.
    /// </summary>
    protected ConcurrentDictionary<IMapsDirectlyToDatabaseTable, byte> Objects
    {
        get
        {
            var result = new ConcurrentDictionary<IMapsDirectlyToDatabaseTable, byte>();
            foreach (var typeDict in _objectsByType.Values)
            foreach (var obj in typeDict.Values)
                result.TryAdd(obj, 0);
            return result;
        }
    }

    private readonly ConcurrentDictionary<IMapsDirectlyToDatabaseTable, HashSet<PropertyChangedExtendedEventArgs>>
        _propertyChanges = new();

    public event EventHandler<SaveEventArgs> Saving;
    public event EventHandler<IMapsDirectlyToDatabaseTableEventArgs> Inserting;
    public event EventHandler<IMapsDirectlyToDatabaseTableEventArgs> Deleting;

    /// <summary>
    /// Helper: Get or create the type-specific dictionary for a given type
    /// </summary>
    protected ConcurrentDictionary<int, IMapsDirectlyToDatabaseTable> GetOrCreateTypeDictionary(Type type)
    {
        return _objectsByType.GetOrAdd(type, _ =>
        {
            // Invalidate type hierarchy when a new type is added
            InvalidateTypeHierarchy();
            return new ConcurrentDictionary<int, IMapsDirectlyToDatabaseTable>();
        });
    }

    /// <summary>
    /// Helper: Add an object to type-indexed storage
    /// </summary>
    private bool AddToTypeIndex(IMapsDirectlyToDatabaseTable obj)
    {
        var typeDict = GetOrCreateTypeDictionary(obj.GetType());
        return typeDict.TryAdd(obj.ID, obj);
    }

    /// <summary>
    /// Helper: Remove an object from type-indexed storage
    /// </summary>
    private bool RemoveFromTypeIndex(IMapsDirectlyToDatabaseTable obj)
    {
        if (_objectsByType.TryGetValue(obj.GetType(), out var typeDict))
            return typeDict.TryRemove(obj.ID, out _);
        return false;
    }

    /// <summary>
    /// Helper: Check if an object exists in type-indexed storage
    /// </summary>
    private bool ContainsInTypeIndex(IMapsDirectlyToDatabaseTable obj)
    {
        return _objectsByType.TryGetValue(obj.GetType(), out var typeDict) && typeDict.ContainsKey(obj.ID);
    }

    /// <summary>
    /// Helper: Get the maximum ID across all types (for NextObjectId initialization)
    /// </summary>
    protected int GetMaxId()
    {
        var allIds = _objectsByType.Values
            .Where(typeDict => !typeDict.IsEmpty)
            .SelectMany(typeDict => typeDict.Keys);

        return allIds.Any() ? allIds.Max() : 0;
    }

    /// <summary>
    /// Helper: Check if repository is empty
    /// </summary>
    protected bool IsEmpty => _objectsByType.Values.All(typeDict => typeDict.IsEmpty);

    /// <summary>
    /// Build precomputed type hierarchy for interface/base class lookups.
    /// Maps each interface/base class to all concrete types that implement/inherit from it.
    /// </summary>
    private FrozenDictionary<Type, Type[]> BuildTypeHierarchy()
    {
        var hierarchy = new Dictionary<Type, HashSet<Type>>();

        // Get all concrete types currently in storage
        var concreteTypes = _objectsByType.Keys.ToArray();

        // For each concrete type, find all its interfaces and base classes
        foreach (var concreteType in concreteTypes)
        {
            // Add all interfaces
            foreach (var iface in concreteType.GetInterfaces())
            {
                if (!hierarchy.ContainsKey(iface))
                    hierarchy[iface] = new HashSet<Type>();
                hierarchy[iface].Add(concreteType);
            }

            // Add base class chain
            var baseType = concreteType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (!hierarchy.ContainsKey(baseType))
                    hierarchy[baseType] = new HashSet<Type>();
                hierarchy[baseType].Add(concreteType);
                baseType = baseType.BaseType;
            }
        }

        // Convert to FrozenDictionary for optimal lookup performance
        return hierarchy.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());
    }

    /// <summary>
    /// Get concrete types that implement/inherit from the given interface or base class.
    /// Uses precomputed hierarchy for O(1) lookup instead of IsAssignableFrom() checks.
    /// </summary>
    private Type[] GetConcreteTypesFor(Type interfaceOrBaseType)
    {
        // Lazy-build type hierarchy on first use
        if (_typeHierarchy == null)
        {
            lock (_typeHierarchyLock)
            {
                if (_typeHierarchy == null)
                    _typeHierarchy = BuildTypeHierarchy();
            }
        }

        // Return precomputed list of concrete types, or empty if not found
        return _typeHierarchy.TryGetValue(interfaceOrBaseType, out var concreteTypes)
            ? concreteTypes
            : Array.Empty<Type>();
    }

    /// <summary>
    /// Invalidate type hierarchy cache when new types are added to storage.
    /// Called when a new type dictionary is created.
    /// </summary>
    private void InvalidateTypeHierarchy()
    {
        lock (_typeHierarchyLock)
        {
            _typeHierarchy = null;
        }
    }

    public virtual void InsertAndHydrate<T>(T toCreate, Dictionary<string, object> constructorParameters)
        where T : IMapsDirectlyToDatabaseTable
    {
        NextObjectId++;
        toCreate.ID = NextObjectId;

        foreach (var kvp in constructorParameters)
        {
            var val = kvp.Value;

            //don't set nulls
            if (val == DBNull.Value)
                val = null;

            var prop = toCreate.GetType().GetProperty(kvp.Key);

            var strVal = kvp.Value as string;

            SetValue(toCreate, prop, strVal, val);
        }

        toCreate.Repository = this;

        AddToTypeIndex(toCreate);

        toCreate.PropertyChanged += toCreate_PropertyChanged;

        NewObjectPool.Add(toCreate);

        Inserting?.Invoke(this, new IMapsDirectlyToDatabaseTableEventArgs(toCreate));
    }

    protected virtual void SetValue<T>(T toCreate, PropertyInfo prop, string strVal, object val)
        where T : IMapsDirectlyToDatabaseTable
    {
        if (val == null)
        {
            prop.SetValue(toCreate, null);
            return;
        }

        var underlying = Nullable.GetUnderlyingType(prop.PropertyType);
        var type = underlying ?? prop.PropertyType;

        if (type.IsEnum)
        {
            prop.SetValue(toCreate, strVal != null ? Enum.Parse(type, strVal) : Enum.ToObject(type, val));
        }
        else
            prop.SetValue(toCreate, Convert.ChangeType(val, type));
    }

    protected void toCreate_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var changes = (PropertyChangedExtendedEventArgs)e;
        var onObject = (IMapsDirectlyToDatabaseTable)sender;

        //if we don't know about this object yet
        _propertyChanges.TryAdd(onObject, new HashSet<PropertyChangedExtendedEventArgs>());

        //if we already knew of a previous change
        var collision = _propertyChanges[onObject].SingleOrDefault(c => c.PropertyName.Equals(changes.PropertyName));

        //throw away that knowledge
        if (collision != null)
            _propertyChanges[onObject].Remove(collision);

        //we know about this change now
        _propertyChanges[onObject].Add(changes);
    }


    public T GetObjectByID<T>(int id) where T : IMapsDirectlyToDatabaseTable
    {
        if (id == 0)
            return default;

        var requestedType = typeof(T);

        // Fast path: Exact concrete type match - O(1)
        if (_objectsByType.TryGetValue(requestedType, out var typeDict) &&
            typeDict.TryGetValue(id, out var obj))
        {
            return (T)obj;
        }

        // Medium path: Interface/base class - use precomputed hierarchy
        if (requestedType.IsInterface || requestedType.IsAbstract)
        {
            var concreteTypes = GetConcreteTypesFor(requestedType);

            foreach (var concreteType in concreteTypes)
            {
                if (_objectsByType.TryGetValue(concreteType, out typeDict) &&
                    typeDict.TryGetValue(id, out obj))
                {
                    return (T)obj;
                }
            }
        }

        throw new KeyNotFoundException($"Could not find {typeof(T).Name} with ID {id}");
    }

    public T[] GetAllObjects<T>() where T : IMapsDirectlyToDatabaseTable
    {
        var requestedType = typeof(T);
        var results = new List<T>();

        // Fast path: Check if exact type exists in storage
        if (_objectsByType.TryGetValue(requestedType, out var typeDict))
        {
            results.AddRange(typeDict.Values.Cast<T>());
        }

        // Also check for subclasses/implementations using precomputed hierarchy
        // This handles: interfaces, abstract classes, AND concrete base classes with subclasses
        var concreteTypes = GetConcreteTypesFor(requestedType);
        foreach (var concreteType in concreteTypes)
        {
            // Skip the exact type we already added above
            if (concreteType == requestedType)
                continue;

            if (_objectsByType.TryGetValue(concreteType, out typeDict))
            {
                results.AddRange(typeDict.Values.Cast<T>());
            }
        }

        return results.OrderBy(o => o.ID).ToArray();
    }

    public T[] GetAllObjectsWhere<T>(string property, object value1) where T : IMapsDirectlyToDatabaseTable
    {
        var prop = typeof(T).GetProperty(property);

        return Objects.Keys.OfType<T>().Where(o => Equals(prop.GetValue(o), value1)).OrderBy(o => o.ID).ToArray();
    }

    public T[] GetAllObjectsWhere<T>(string property1, object value1, ExpressionType operand, string property2,
        object value2) where T : IMapsDirectlyToDatabaseTable
    {
        var prop1 = typeof(T).GetProperty(property1);
        var prop2 = typeof(T).GetProperty(property2);

        return operand switch
        {
            ExpressionType.AndAlso => Objects.Keys.OfType<T>()
                .Where(o => Equals(prop1.GetValue(o), value1) && Equals(prop2.GetValue(o), value2))
                .OrderBy(o => o.ID).ToArray(),
            ExpressionType.OrElse => Objects.Keys.OfType<T>()
                .Where(o => Equals(prop1.GetValue(o), value1) || Equals(prop2.GetValue(o), value2))
                .OrderBy(o => o.ID).ToArray(),
            _ => throw new NotSupportedException("operand")
        };
    }

    public IEnumerable<IMapsDirectlyToDatabaseTable> GetAllObjects(Type t)
    {
        // O(m) where m = count of type t only
        if (_objectsByType.TryGetValue(t, out var typeDict))
        {
            return typeDict.Values;
        }

        return Enumerable.Empty<IMapsDirectlyToDatabaseTable>();
    }

    public T[] GetAllObjectsWithParent<T>(IMapsDirectlyToDatabaseTable parent) where T : IMapsDirectlyToDatabaseTable
    {
        //e.g. Catalogue_ID
        var propertyName = $"{parent.GetType().Name}_ID";

        var prop = typeof(T).GetProperty(propertyName);
        var requestedType = typeof(T);

        // Fast path: Exact concrete type match
        if (_objectsByType.TryGetValue(requestedType, out var typeDict))
        {
            return typeDict.Values.Cast<T>()
                .Where(o => prop.GetValue(o) as int? == parent.ID)
                .OrderBy(o => o.ID)
                .ToArray();
        }

        // Medium path: Interface/base class - use precomputed hierarchy
        if (requestedType.IsInterface || requestedType.IsAbstract)
        {
            var concreteTypes = GetConcreteTypesFor(requestedType);
            var results = new List<T>();

            foreach (var concreteType in concreteTypes)
            {
                if (_objectsByType.TryGetValue(concreteType, out typeDict))
                {
                    results.AddRange(typeDict.Values.Cast<T>()
                        .Where(o => prop.GetValue(o) as int? == parent.ID));
                }
            }

            return results.OrderBy(o => o.ID).ToArray();
        }

        return Array.Empty<T>();
    }

    public T[] GetAllObjectsWithParent<T, T2>(T2 parent) where T : IMapsDirectlyToDatabaseTable, IInjectKnown<T2>
        where T2 : IMapsDirectlyToDatabaseTable
    {
        //e.g. Catalogue_ID
        var propertyName = $"{typeof(T2).Name}_ID";

        var prop = typeof(T).GetProperty(propertyName);
        var requestedType = typeof(T);

        // Fast path: Exact concrete type match
        if (_objectsByType.TryGetValue(requestedType, out var typeDict))
        {
            return typeDict.Values.Cast<T>()
                .Where(o => prop.GetValue(o) as int? == parent.ID)
                .OrderBy(o => o.ID)
                .ToArray();
        }

        // Medium path: Interface/base class - use precomputed hierarchy
        if (requestedType.IsInterface || requestedType.IsAbstract)
        {
            var concreteTypes = GetConcreteTypesFor(requestedType);
            var results = new List<T>();

            foreach (var concreteType in concreteTypes)
            {
                if (_objectsByType.TryGetValue(concreteType, out typeDict))
                {
                    results.AddRange(typeDict.Values.Cast<T>()
                        .Where(o => prop.GetValue(o) as int? == parent.ID));
                }
            }

            return results.OrderBy(o => o.ID).ToArray();
        }

        return Array.Empty<T>();
    }

    public virtual void SaveToDatabase(IMapsDirectlyToDatabaseTable oTableWrapperObject)
    {
        Saving?.Invoke(this, new SaveEventArgs(oTableWrapperObject));

        var typeDict = GetOrCreateTypeDictionary(oTableWrapperObject.GetType());

        // If saving a new reference to an existing object then we should update our tracked
        // objects to the latest reference since the old one is stale
        if (typeDict.TryGetValue(oTableWrapperObject.ID, out var existing) &&
            !ReferenceEquals(existing, oTableWrapperObject))
        {
            typeDict[oTableWrapperObject.ID] = oTableWrapperObject;
        }
        else
        {
            // New object, add it
            typeDict.TryAdd(oTableWrapperObject.ID, oTableWrapperObject);
        }

        //forget about property changes (since it's 'saved' now)
        _propertyChanges.TryRemove(oTableWrapperObject, out _);
    }

    public virtual void DeleteFromDatabase(IMapsDirectlyToDatabaseTable oTableWrapperObject)
    {
        CascadeDeletes(oTableWrapperObject);

        RemoveFromTypeIndex(oTableWrapperObject);

        //forget about property changes (since it's been deleted)
        _propertyChanges.TryRemove(oTableWrapperObject, out _);

        Deleting?.Invoke(this, new IMapsDirectlyToDatabaseTableEventArgs(oTableWrapperObject));
    }

    /// <summary>
    /// Override to replicate any database delete cascade relationships (e.g. deleting all
    /// CatalogueItem when a Catalogue is deleted)
    /// </summary>
    /// <param name="oTableWrapperObject"></param>
    protected virtual void CascadeDeletes(IMapsDirectlyToDatabaseTable oTableWrapperObject)
    {
    }

    public void RevertToDatabaseState([NotNull] IMapsDirectlyToDatabaseTable mapsDirectlyToDatabaseTable)
    {
        //Mark any cached data as out of date
        if (mapsDirectlyToDatabaseTable is IInjectKnown inject)
            inject.ClearAllInjections();

        if (!_propertyChanges.TryGetValue(mapsDirectlyToDatabaseTable, out var changedExtendedEventArgsSet))
            return;

        var type = mapsDirectlyToDatabaseTable.GetType();

        foreach (var e in changedExtendedEventArgsSet.ToArray()) //call ToArray to avoid cyclical events on SetValue
        {
            var prop = type.GetProperty(e.PropertyName);
            prop.SetValue(mapsDirectlyToDatabaseTable, e.OldValue); //reset the old values
        }

        //forget about all changes now
        _propertyChanges.TryRemove(mapsDirectlyToDatabaseTable, out _);
    }

    [NotNull]
    public RevertableObjectReport HasLocalChanges(IMapsDirectlyToDatabaseTable mapsDirectlyToDatabaseTable)
    {
        //if we don't know about it then it was deleted
        if (!ContainsInTypeIndex(mapsDirectlyToDatabaseTable))
            return new RevertableObjectReport { Evaluation = ChangeDescription.DatabaseCopyWasDeleted };

        //if it has no changes (since a save)
        if (!_propertyChanges.TryGetValue(mapsDirectlyToDatabaseTable, out var changedExtendedEventArgsSet))
            return new RevertableObjectReport { Evaluation = ChangeDescription.NoChanges };

        //we have local 'unsaved' changes
        var type = mapsDirectlyToDatabaseTable.GetType();
        var differences = changedExtendedEventArgsSet.Select(
                d => new RevertablePropertyDifference(type.GetProperty(d.PropertyName), d.NewValue, d.OldValue))
            .ToList();

        return new RevertableObjectReport(differences) { Evaluation = ChangeDescription.DatabaseCopyDifferent };
    }

    /// <inheritdoc/>
    public bool AreEqual(IMapsDirectlyToDatabaseTable obj1, object obj2)
    {
        if (obj1 == null && obj2 != null)
            return false;

        if (obj2 == null && obj1 != null)
            return false;

        if (obj1 == null && obj2 == null)
            throw new NotSupportedException(
                "Why are you comparing two null things against one another with this method?");

        return obj1.GetType() == obj2.GetType() && obj1.ID == ((IMapsDirectlyToDatabaseTable)obj2).ID;
    }

    /// <inheritdoc/>
    public int GetHashCode(IMapsDirectlyToDatabaseTable obj1) => obj1.GetType().GetHashCode() * obj1.ID;

    public Version GetVersion() => GetType().Assembly.GetName().Version;


    public bool StillExists<T>(int allegedParent) where T : IMapsDirectlyToDatabaseTable
    {
        var requestedType = typeof(T);

        // Fast path: Exact concrete type match - O(1)
        if (_objectsByType.TryGetValue(requestedType, out var typeDict) && typeDict.ContainsKey(allegedParent))
            return true;

        // Medium path: Interface/base class - use precomputed hierarchy
        if (requestedType.IsInterface || requestedType.IsAbstract)
        {
            var concreteTypes = GetConcreteTypesFor(requestedType);

            foreach (var concreteType in concreteTypes)
            {
                if (_objectsByType.TryGetValue(concreteType, out typeDict) &&
                    typeDict.ContainsKey(allegedParent))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool StillExists(IMapsDirectlyToDatabaseTable o) => ContainsInTypeIndex(o);

    public bool StillExists(Type objectType, int objectId)
    {
        // O(1) lookup using type-indexed dictionary
        return _objectsByType.TryGetValue(objectType, out var typeDict) && typeDict.ContainsKey(objectId);
    }

    public IMapsDirectlyToDatabaseTable GetObjectByID(Type objectType, int objectId)
    {
        // O(1) lookup using type-indexed dictionary
        if (_objectsByType.TryGetValue(objectType, out var typeDict) &&
            typeDict.TryGetValue(objectId, out var obj))
        {
            return obj;
        }

        throw new KeyNotFoundException(
            $"Could not find object of Type '{objectType}' with ID '{objectId}' in {nameof(MemoryRepository)}");
    }

    public IEnumerable<T> GetAllObjectsInIDList<T>(IEnumerable<int> ids) where T : IMapsDirectlyToDatabaseTable
    {
        var requestedType = typeof(T);
        var hs = new HashSet<int>(ids);

        // Fast path: Exact concrete type match
        if (_objectsByType.TryGetValue(requestedType, out var typeDict))
        {
            return typeDict.Values.Cast<T>()
                .Where(o => hs.Contains(o.ID))
                .OrderBy(o => o.ID);
        }

        // Medium path: Interface/base class - use precomputed hierarchy
        if (requestedType.IsInterface || requestedType.IsAbstract)
        {
            var concreteTypes = GetConcreteTypesFor(requestedType);
            var results = new List<T>();

            foreach (var concreteType in concreteTypes)
            {
                if (_objectsByType.TryGetValue(concreteType, out typeDict))
                {
                    results.AddRange(typeDict.Values.Cast<T>()
                        .Where(o => hs.Contains(o.ID)));
                }
            }

            return results.OrderBy(o => o.ID);
        }

        return Enumerable.Empty<T>();
    }

    public IEnumerable<IMapsDirectlyToDatabaseTable> GetAllObjectsInIDList(Type elementType, IEnumerable<int> ids)
    {
        // O(k) where k = count of requested IDs
        if (!_objectsByType.TryGetValue(elementType, out var typeDict))
            return Enumerable.Empty<IMapsDirectlyToDatabaseTable>();

        var hs = new HashSet<int>(ids);
        return typeDict.Values.Where(o => hs.Contains(o.ID));
    }

    public void SaveSpecificPropertyOnlyToDatabase(IMapsDirectlyToDatabaseTable entity, string propertyName,
        object propertyValue)
    {
        var prop = entity.GetType().GetProperty(propertyName);
        prop.SetValue(entity, propertyValue);
        SaveToDatabase(entity);
    }


    public IMapsDirectlyToDatabaseTable[] GetAllObjectsInDatabase()
    {
        // Flatten all type dictionaries
        return _objectsByType.Values
            .SelectMany(typeDict => typeDict.Values)
            .OrderBy(o => o.ID)
            .ToArray();
    }

    public bool SupportsObjectType(Type type) => typeof(IMapsDirectlyToDatabaseTable).IsAssignableFrom(type);

    public void TestConnection()
    {
    }

    public virtual void Clear()
    {
        _objectsByType.Clear();
        InvalidateTypeHierarchy();
    }

    public Type[] GetCompatibleTypes()
    {
        return
            GetType().Assembly.GetTypes()
                .Where(
                    t =>
                        typeof(IMapsDirectlyToDatabaseTable).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && !t.IsInterface

                        //nothing called spontaneous
                        && !t.Name.Contains("Spontaneous")

                        //or with a spontaneous base class
                        && (t.BaseType == null || !t.BaseType.Name.Contains("Spontaneous"))
                ).ToArray();
    }


    public IDisposable BeginNewTransaction() => new EmptyDisposeable();

    public void EndTransaction(bool commit)
    {
    }
}