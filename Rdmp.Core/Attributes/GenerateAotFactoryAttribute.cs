// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;

namespace Rdmp.Core.Attributes;

/// <summary>
/// Attribute to mark classes for AOT (Ahead-of-Time) object factory generation.
/// When applied to a class, the source generator will create optimized constructor delegates
/// that eliminate the need for reflection during object construction, improving performance
/// in Native AOT scenarios.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateAotFactoryAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the priority of this factory when multiple constructors are available.
    /// Higher values indicate higher priority. Default is 0.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether to generate factories for all constructors or only public ones.
    /// Default is false (only public constructors).
    /// </summary>
    public bool IncludeNonPublicConstructors { get; set; }

    /// <summary>
    /// Gets or sets specific constructor parameter types to generate factories for.
    /// If null or empty, factories will be generated for all compatible constructors.
    /// </summary>
    public Type[] TargetConstructorParameters { get; set; }

    /// <summary>
    /// Gets or sets whether to generate a variable-parameter factory for dynamic constructor selection.
    /// Default is true.
    /// </summary>
    public bool GenerateVariableFactory { get; set; }

    /// <summary>
    /// Gets or sets whether the generated factory should be registered automatically in the AotObjectFactoryRegistry.
    /// Default is true.
    /// </summary>
    public bool AutoRegister { get; set; }

    /// <summary>
    /// Initializes a new instance of the GenerateAotFactoryAttribute with default settings.
    /// </summary>
    public GenerateAotFactoryAttribute()
    {
        Priority = 0;
        IncludeNonPublicConstructors = false;
        TargetConstructorParameters = null;
        GenerateVariableFactory = true;
        AutoRegister = true;
    }

    /// <summary>
    /// Initializes a new instance of the GenerateAotFactoryAttribute with specified priority.
    /// </summary>
    /// <param name="priority">The priority for constructor selection when multiple constructors exist</param>
    public GenerateAotFactoryAttribute(int priority)
    {
        Priority = priority;
        IncludeNonPublicConstructors = false;
        TargetConstructorParameters = null;
        GenerateVariableFactory = true;
        AutoRegister = true;
    }

    /// <summary>
    /// Initializes a new instance of the GenerateAotFactoryAttribute with custom settings.
    /// </summary>
    /// <param name="priority">The priority for constructor selection</param>
    /// <param name="includeNonPublicConstructors">Whether to include non-public constructors</param>
    /// <param name="generateVariableFactory">Whether to generate variable-parameter factory</param>
    /// <param name="autoRegister">Whether to auto-register in the factory registry</param>
    public GenerateAotFactoryAttribute(
        int priority = 0,
        bool includeNonPublicConstructors = false,
        bool generateVariableFactory = true,
        bool autoRegister = true)
    {
        Priority = priority;
        IncludeNonPublicConstructors = includeNonPublicConstructors;
        TargetConstructorParameters = null;
        GenerateVariableFactory = generateVariableFactory;
        AutoRegister = autoRegister;
    }

    /// <summary>
    /// Initializes a new instance of the GenerateAotFactoryAttribute targeting specific constructor parameters.
    /// </summary>
    /// <param name="targetConstructorParameters">Array of parameter types for the target constructor</param>
    /// <param name="priority">The priority for constructor selection</param>
    /// <param name="generateVariableFactory">Whether to generate variable-parameter factory</param>
    /// <param name="autoRegister">Whether to auto-register in the factory registry</param>
    public GenerateAotFactoryAttribute(
        Type[] targetConstructorParameters,
        int priority = 0,
        bool generateVariableFactory = true,
        bool autoRegister = true)
    {
        Priority = priority;
        IncludeNonPublicConstructors = false;
        TargetConstructorParameters = targetConstructorParameters;
        GenerateVariableFactory = generateVariableFactory;
        AutoRegister = autoRegister;
    }
}

/// <summary>
/// Attribute to mark constructors for preferential AOT factory generation.
/// This is useful when a class has multiple constructors and you want to specify
/// which one should be used for AOT generation.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
public sealed class UseWithAotFactoryAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the priority for this constructor when multiple constructors are marked.
    /// Higher values indicate higher priority. Default is 0.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Initializes a new instance of the UseWithAotFactoryAttribute with default priority.
    /// </summary>
    public UseWithAotFactoryAttribute()
    {
        Priority = 0;
    }

    /// <summary>
    /// Initializes a new instance of the UseWithAotFactoryAttribute with specified priority.
    /// </summary>
    /// <param name="priority">The priority for constructor selection</param>
    public UseWithAotFactoryAttribute(int priority)
    {
        Priority = priority;
    }
}