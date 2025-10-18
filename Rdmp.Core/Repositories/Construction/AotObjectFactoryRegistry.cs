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

namespace Rdmp.Core.Repositories.Construction;

/// <summary>
/// Thread-safe registry for AOT-compatible object factories
/// Provides fast constructor lookup and object creation without reflection
/// </summary>
public static class AotObjectFactoryRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<object>> _blankConstructorFactories = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<Type[], Func<object, object>>> _singleParameterFactories = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<(Type, Type), Func<object, object, object>>> _doubleParameterFactories = new();
    private static readonly ConcurrentDictionary<Type, Func<object[], object>> _variableParameterFactories = new();
    private static readonly ConcurrentDictionary<Type, IAotObjectFactory> _customFactories = new();

    private static readonly ReaderWriterLockSlim _registrationLock = new();
    private static volatile bool _isInitialized = false;
    private static readonly object _initLock = new();

    /// <summary>
    /// Gets whether the registry has been initialized with generated factories
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the number of registered factories
    /// </summary>
    public static int FactoryCount => _blankConstructorFactories.Count +
                                       _singleParameterFactories.Sum(kvp => kvp.Value.Count) +
                                       _doubleParameterFactories.Sum(kvp => kvp.Value.Count) +
                                       _variableParameterFactories.Count +
                                       _customFactories.Count;

    /// <summary>
    /// Registers a factory for objects with blank constructors
    /// </summary>
    /// <typeparam name="T">The type to construct</typeparam>
    /// <param name="factory">The factory delegate</param>
    public static void RegisterBlankConstructorFactory<T>(Func<T> factory)
    {
        RegisterBlankConstructorFactory(typeof(T), () => factory());
    }

    /// <summary>
    /// Registers a factory for objects with blank constructors
    /// </summary>
    /// <param name="targetType">The type to construct</param>
    /// <param name="factory">The factory delegate</param>
    public static void RegisterBlankConstructorFactory(Type targetType, Func<object> factory)
    {
        if (targetType == null) throw new ArgumentNullException(nameof(targetType));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        _blankConstructorFactories.AddOrUpdate(targetType, factory, (_, _) => factory);
    }

    /// <summary>
    /// Registers a factory for objects with single parameter constructors
    /// </summary>
    /// <typeparam name="T">The type to construct</typeparam>
    /// <typeparam name="TParam">The parameter type</typeparam>
    /// <param name="factory">The factory delegate</param>
    public static void RegisterSingleParameterFactory<T, TParam>(Func<TParam, T> factory)
    {
        RegisterSingleParameterFactory(typeof(T), typeof(TParam), param => factory((TParam)param));
    }

    /// <summary>
    /// Registers a factory for objects with single parameter constructors
    /// </summary>
    /// <param name="targetType">The type to construct</param>
    /// <param name="parameterType">The parameter type</param>
    /// <param name="factory">The factory delegate</param>
    public static void RegisterSingleParameterFactory(Type targetType, Type parameterType, Func<object, object> factory)
    {
        if (targetType == null) throw new ArgumentNullException(nameof(targetType));
        if (parameterType == null) throw new ArgumentNullException(nameof(parameterType));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var factories = _singleParameterFactories.GetOrAdd(targetType, _ => new Dictionary<Type[], Func<object, object>>());

        lock (factories)
        {
            var key = new[] { parameterType };
            factories[key] = factory;
        }
    }

    /// <summary>
    /// Registers a factory for objects with double parameter constructors
    /// </summary>
    /// <typeparam name="T">The type to construct</typeparam>
    /// <typeparam name="TParam1">The first parameter type</typeparam>
    /// <typeparam name="TParam2">The second parameter type</typeparam>
    /// <param name="factory">The factory delegate</param>
    public static void RegisterDoubleParameterFactory<T, TParam1, TParam2>(Func<TParam1, TParam2, T> factory)
    {
        RegisterDoubleParameterFactory(typeof(T), typeof(TParam1), typeof(TParam2),
            (param1, param2) => factory((TParam1)param1, (TParam2)param2));
    }

    /// <summary>
    /// Registers a factory for objects with double parameter constructors
    /// </summary>
    /// <param name="targetType">The type to construct</param>
    /// <param name="parameterType1">The first parameter type</param>
    /// <param name="parameterType2">The second parameter type</param>
    /// <param name="factory">The factory delegate</param>
    public static void RegisterDoubleParameterFactory(Type targetType, Type parameterType1, Type parameterType2,
        Func<object, object, object> factory)
    {
        if (targetType == null) throw new ArgumentNullException(nameof(targetType));
        if (parameterType1 == null) throw new ArgumentNullException(nameof(parameterType1));
        if (parameterType2 == null) throw new ArgumentNullException(nameof(parameterType2));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var factories = _doubleParameterFactories.GetOrAdd(targetType, _ => new Dictionary<(Type, Type), Func<object, object, object>>());

        lock (factories)
        {
            var key = (parameterType1, parameterType2);
            factories[key] = factory;
        }
    }

    /// <summary>
    /// Registers a factory for objects with variable parameter constructors
    /// </summary>
    /// <typeparam name="T">The type to construct</typeparam>
    /// <param name="factory">The factory delegate</param>
    public static void RegisterVariableParameterFactory<T>(Func<object[], T> factory)
    {
        RegisterVariableParameterFactory(typeof(T), parameters => factory(parameters));
    }

    /// <summary>
    /// Registers a factory for objects with variable parameter constructors
    /// </summary>
    /// <param name="targetType">The type to construct</param>
    /// <param name="factory">The factory delegate</param>
    public static void RegisterVariableParameterFactory(Type targetType, Func<object[], object> factory)
    {
        if (targetType == null) throw new ArgumentNullException(nameof(targetType));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        _variableParameterFactories.AddOrUpdate(targetType, factory, (_, _) => factory);
    }

    /// <summary>
    /// Registers a custom factory implementation
    /// </summary>
    /// <param name="factory">The custom factory</param>
    public static void RegisterCustomFactory(IAotObjectFactory factory)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        _customFactories.AddOrUpdate(factory.TargetType, factory, (_, _) => factory);
    }

    /// <summary>
    /// Attempts to create an instance using a registered factory
    /// </summary>
    /// <param name="targetType">The type to construct</param>
    /// <param name="parameters">Constructor parameters</param>
    /// <param name="instance">The created instance</param>
    /// <returns>True if a factory was found and used, false otherwise</returns>
    public static bool TryCreate(Type targetType, object[] parameters, out object instance)
    {
        if (targetType == null)
        {
            instance = null;
            return false;
        }

        // Try custom factories first
        if (_customFactories.TryGetValue(targetType, out var customFactory))
        {
            try
            {
                instance = customFactory.Create(parameters);
                return true;
            }
            catch
            {
                // Fall back to other factory types
            }
        }

        // Try blank constructor
        if ((parameters == null || parameters.Length == 0) && _blankConstructorFactories.TryGetValue(targetType, out var blankFactory))
        {
            try
            {
                instance = blankFactory();
                return true;
            }
            catch
            {
                // Fall back to other factory types
            }
        }

        // Try single parameter factory
        if (parameters?.Length == 1 && TryCreateSingleParameter(targetType, parameters[0], out instance))
        {
            return true;
        }

        // Try double parameter factory
        if (parameters?.Length == 2 && TryCreateDoubleParameter(targetType, parameters[0], parameters[1], out instance))
        {
            return true;
        }

        // Try variable parameter factory
        if (_variableParameterFactories.TryGetValue(targetType, out var variableFactory))
        {
            try
            {
                instance = variableFactory(parameters);
                return true;
            }
            catch
            {
                // Fall back to other methods
            }
        }

        instance = null;
        return false;
    }

    /// <summary>
    /// Attempts to create an instance using a registered factory with a single parameter
    /// </summary>
    private static bool TryCreateSingleParameter(Type targetType, object parameter, out object instance)
    {
        if (_singleParameterFactories.TryGetValue(targetType, out var factories))
        {
            var parameterType = parameter?.GetType();

            lock (factories)
            {
                // Look for exact type match first
                var exactMatch = factories.FirstOrDefault(kvp => kvp.Key.Length == 1 && kvp.Key[0] == parameterType);
                if (exactMatch.Key != null)
                {
                    instance = exactMatch.Value(parameter);
                    return true;
                }

                // Look for assignable type match
                var assignableMatch = factories.FirstOrDefault(kvp =>
                    kvp.Key.Length == 1 && parameterType != null && kvp.Key[0].IsAssignableFrom(parameterType));
                if (assignableMatch.Key != null)
                {
                    instance = assignableMatch.Value(parameter);
                    return true;
                }
            }
        }

        instance = null;
        return false;
    }

    /// <summary>
    /// Attempts to create an instance using a registered factory with two parameters
    /// </summary>
    private static bool TryCreateDoubleParameter(Type targetType, object parameter1, object parameter2, out object instance)
    {
        if (_doubleParameterFactories.TryGetValue(targetType, out var factories))
        {
            var param1Type = parameter1?.GetType();
            var param2Type = parameter2?.GetType();

            lock (factories)
            {
                // Look for exact type match first
                var exactMatch = factories.FirstOrDefault(kvp =>
                    kvp.Key.Item1 == param1Type && kvp.Key.Item2 == param2Type);
                if (exactMatch.Key != null)
                {
                    instance = exactMatch.Value(parameter1, parameter2);
                    return true;
                }

                // Look for assignable type match
                var assignableMatch = factories.FirstOrDefault(kvp =>
                    param1Type != null && kvp.Key.Item1.IsAssignableFrom(param1Type) &&
                    param2Type != null && kvp.Key.Item2.IsAssignableFrom(param2Type));
                if (assignableMatch.Key != null)
                {
                    instance = assignableMatch.Value(parameter1, parameter2);
                    return true;
                }
            }
        }

        instance = null;
        return false;
    }

    /// <summary>
    /// Checks if a factory is registered for the specified type
    /// </summary>
    /// <param name="targetType">The type to check</param>
    /// <returns>True if a factory is registered, false otherwise</returns>
    public static bool HasFactory(Type targetType)
    {
        if (targetType == null) return false;

        return _blankConstructorFactories.ContainsKey(targetType) ||
               _singleParameterFactories.ContainsKey(targetType) ||
               _doubleParameterFactories.ContainsKey(targetType) ||
               _variableParameterFactories.ContainsKey(targetType) ||
               _customFactories.ContainsKey(targetType);
    }

    /// <summary>
    /// Gets all registered target types
    /// </summary>
    /// <returns>Array of registered target types</returns>
    public static Type[] GetRegisteredTypes()
    {
        var types = new HashSet<Type>();

        foreach (var key in _blankConstructorFactories.Keys)
            types.Add(key);

        foreach (var key in _singleParameterFactories.Keys)
            types.Add(key);

        foreach (var key in _doubleParameterFactories.Keys)
            types.Add(key);

        foreach (var key in _variableParameterFactories.Keys)
            types.Add(key);

        foreach (var key in _customFactories.Keys)
            types.Add(key);

        return types.ToArray();
    }

    /// <summary>
    /// Clears all registered factories (for testing purposes)
    /// </summary>
    public static void Clear()
    {
        _registrationLock.EnterWriteLock();
        try
        {
            _blankConstructorFactories.Clear();
            _singleParameterFactories.Clear();
            _doubleParameterFactories.Clear();
            _variableParameterFactories.Clear();
            _customFactories.Clear();
            _isInitialized = false;
        }
        finally
        {
            _registrationLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Initializes the registry with generated factories
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        lock (_initLock)
        {
            if (_isInitialized) return;

            _registrationLock.EnterWriteLock();
            try
            {
                // Try to call the generated registration method
                try
                {
                    var assembly = typeof(AotObjectFactoryRegistry).Assembly;
                    var factoryType = assembly.GetType("Rdmp.Core.Generated.Factories.AotObjectFactories");
                    var registerMethod = factoryType?.GetMethod("RegisterAllFactories");

                    registerMethod?.Invoke(null, null);
                }
                catch
                {
                    // Generated factories not available, that's okay
                }

                _isInitialized = true;
            }
            finally
            {
                _registrationLock.ExitWriteLock();
            }
        }
    }
}