// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;

namespace Rdmp.Core.Repositories.Construction;

/// <summary>
/// AOT-compatible factory interface for creating objects with blank constructors
/// </summary>
/// <typeparam name="T">The type to construct</typeparam>
public interface IAotObjectFactory<T>
{
    /// <summary>
    /// Creates a new instance of T using the blank constructor
    /// </summary>
    /// <returns>A new instance of T</returns>
    T Create();
}

/// <summary>
/// AOT-compatible factory interface for creating objects with single parameter constructors
/// </summary>
/// <typeparam name="T">The type to construct</typeparam>
/// <typeparam name="TParam1">The type of the first constructor parameter</typeparam>
public interface IAotObjectFactory<T, in TParam1>
{
    /// <summary>
    /// Creates a new instance of T using a constructor that takes TParam1
    /// </summary>
    /// <param name="param1">The first constructor parameter</param>
    /// <returns>A new instance of T</returns>
    T Create(TParam1 param1);
}

/// <summary>
/// AOT-compatible factory interface for creating objects with double parameter constructors
/// </summary>
/// <typeparam name="T">The type to construct</typeparam>
/// <typeparam name="TParam1">The type of the first constructor parameter</typeparam>
/// <typeparam name="TParam2">The type of the second constructor parameter</typeparam>
public interface IAotObjectFactory<T, in TParam1, in TParam2>
{
    /// <summary>
    /// Creates a new instance of T using a constructor that takes TParam1 and TParam2
    /// </summary>
    /// <param name="param1">The first constructor parameter</param>
    /// <param name="param2">The second constructor parameter</param>
    /// <returns>A new instance of T</returns>
    T Create(TParam1 param1, TParam2 param2);
}

/// <summary>
/// AOT-compatible factory interface for creating objects with variable parameter constructors
/// </summary>
/// <typeparam name="T">The type to construct</typeparam>
public interface IAotVariableObjectFactory<T>
{
    /// <summary>
    /// Creates a new instance of T using a constructor that matches the provided parameters
    /// </summary>
    /// <param name="parameters">The constructor parameters</param>
    /// <returns>A new instance of T</returns>
    T Create(params object[] parameters);
}

/// <summary>
/// Base interface for all AOT object factories providing type information
/// </summary>
public interface IAotObjectFactory
{
    /// <summary>
    /// Gets the type that this factory creates
    /// </summary>
    Type TargetType { get; }

    /// <summary>
    /// Gets the parameter types this factory expects
    /// </summary>
    Type[] ParameterTypes { get; }

    /// <summary>
    /// Creates a new instance using the provided parameters
    /// </summary>
    /// <param name="parameters">Constructor parameters</param>
    /// <returns>A new instance of the target type</returns>
    object Create(params object[] parameters);
}

/// <summary>
/// Generic delegate for AOT object construction with no parameters
/// </summary>
/// <typeparam name="T">The type to construct</typeparam>
/// <returns>A new instance of T</returns>
public delegate T AotConstructor<T>();

/// <summary>
/// Generic delegate for AOT object construction with one parameter
/// </summary>
/// <typeparam name="T">The type to construct</typeparam>
/// <typeparam name="TParam1">The first parameter type</typeparam>
/// <param name="param1">The first parameter</param>
/// <returns>A new instance of T</returns>
public delegate T AotConstructor<T, in TParam1>(TParam1 param1);

/// <summary>
/// Generic delegate for AOT object construction with two parameters
/// </summary>
/// <typeparam name="T">The type to construct</typeparam>
/// <typeparam name="TParam1">The first parameter type</typeparam>
/// <typeparam name="TParam2">The second parameter type</typeparam>
/// <param name="param1">The first parameter</param>
/// <param name="param2">The second parameter</param>
/// <returns>A new instance of T</returns>
public delegate T AotConstructor<T, in TParam1, in TParam2>(TParam1 param1, TParam2 param2);

/// <summary>
/// Generic delegate for AOT object construction with variable parameters
/// </summary>
/// <typeparam name="T">The type to construct</typeparam>
/// <param name="parameters">Constructor parameters</param>
/// <returns>A new instance of T</returns>
public delegate T AotVariableConstructor<T>(params object[] parameters);