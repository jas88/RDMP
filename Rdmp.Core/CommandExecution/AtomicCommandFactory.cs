// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.CommandExecution.AtomicCommands.Alter;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Repositories;
using Rdmp.Core.Repositories.Construction;

namespace Rdmp.Core.CommandExecution;

/// <summary>
/// Builds lists of <see cref="IAtomicCommand"/> for any given RDMP object.
/// Uses constructor signature analysis to efficiently filter compatible commands before instantiation.
/// </summary>
public class AtomicCommandFactory : CommandFactoryBase
{
    private readonly IBasicActivateItems _activator;
    private readonly GoToCommandFactory _goto;

    // Category constants
    public const string Add = "Add";
    public const string Batching = "Batching";
    public const string New = "New";
    public const string GoTo = "Go To";
    public const string Extraction = "Extractability";
    public const string Metadata = "Metadata";
    public const string Alter = "Alter";
    public const string SetUsageContext = "Set Context";
    public const string SetContainerOperation = "Set Operation";
    public const string Dimensions = "Dimensions";
    public const string Advanced = "Advanced";
    public const string View = "View";
    public const string Deprecation = "Deprecation";
    public const string ViewParentTree = "View Parent Tree";

    /// <summary>
    /// Maps command types to the parameter types they accept (excluding IBasicActivateItems).
    /// Built once at startup for fast runtime lookups.
    /// </summary>
    private static readonly Lazy<Dictionary<Type, HashSet<Type>>> _commandCompatibility =
        new(BuildCompatibilityMap, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public AtomicCommandFactory(IBasicActivateItems activator)
    {
        _activator = activator;
        _goto = new GoToCommandFactory(_activator);
    }

    /// <summary>
    /// Analyzes all IAtomicCommand types to build a compatibility map based on constructor signatures.
    /// This is called once at startup to avoid repeated reflection during runtime.
    /// </summary>
    private static Dictionary<Type, HashSet<Type>> BuildCompatibilityMap()
    {
        var map = new Dictionary<Type, HashSet<Type>>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var cmdType in MEF.GetTypes<IAtomicCommand>())
        {
            var acceptedTypes = new HashSet<Type>();

            // Analyze all constructors to find accepted parameter types
            foreach (var ctor in cmdType.GetConstructors(flags))
            {
                foreach (var param in ctor.GetParameters())
                {
                    var paramType = param.ParameterType;

                    // Skip activator/repository types - we're only interested in domain objects
                    if (typeof(IBasicActivateItems).IsAssignableFrom(paramType) ||
                        typeof(IRepository).IsAssignableFrom(paramType))
                        continue;

                    // Handle arrays (e.g., params Catalogue[] catalogues)
                    if (paramType.IsArray)
                        acceptedTypes.Add(paramType.GetElementType()!);
                    else
                        acceptedTypes.Add(paramType);
                }
            }

            if (acceptedTypes.Count > 0)
                map[cmdType] = acceptedTypes;
        }

        return map;
    }

    /// <summary>
    /// Checks if a command type can potentially accept the given object type.
    /// Uses pre-computed compatibility map for fast O(1) lookup.
    /// </summary>
    private static bool IsCompatible(Type commandType, Type targetType)
    {
        if (!_commandCompatibility.Value.TryGetValue(commandType, out var acceptedTypes))
            return true; // No constructor parameters = accepts anything (e.g., blank constructor)

        // Check if target type matches or is assignable to any accepted type
        return acceptedTypes.Any(acceptedType => acceptedType.IsAssignableFrom(targetType));
    }

    /// <summary>
    /// Returns all commands that could be run involving <paramref name="o"/> in order of most useful to least useful.
    /// Commands are auto-discovered and filtered by constructor compatibility before instantiation.
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public IEnumerable<IAtomicCommand> CreateCommands(object o)
    {
        // Special case: GoTo commands (always try these)
        foreach (var cmd in _goto.GetCommands(o))
        {
            cmd.SuggestedCategory = GoTo;
            yield return cmd;
        }

        // Special case: Activate command (always try if activatable)
        if (_activator.CanActivate(o))
            yield return new ExecuteCommandActivate(_activator, o);

        // Auto-discover all compatible commands
        var targetType = o.GetType();

        foreach (var cmdType in MEF.GetTypes<IAtomicCommand>())
        {
            // Skip ExecuteCommandActivate - already handled above
            if (cmdType == typeof(ExecuteCommandActivate))
                continue;

            // Fast compatibility check before attempting construction
            if (!IsCompatible(cmdType, targetType))
                continue;

            // Try to construct the command with available parameters
            var cmd = AotObjectConstructor.ConstructIfPossible(cmdType, _activator, o) as IAtomicCommand;

            // Only yield commands that were successfully constructed and are possible
            if (cmd?.IsImpossible == false)
                yield return cmd;
        }
    }

    /// <summary>
    /// Returns commands that can operate on multiple objects at once (batch operations).
    /// Single-pass enumeration for efficiency.
    /// </summary>
    /// <param name="many">Collection of objects to create batch commands for</param>
    /// <returns>Enumerable of batch commands that can operate on all objects in the collection</returns>
    public IEnumerable<IAtomicCommand> CreateManyObjectCommands(ICollection many)
    {
        var allDisableable = true;
        var allHasFolder = true;
        var allTableInfo = true;
        var allCatalogueItem = true;
        var allDeleteable = true;
        var allExtractionFilterParameterSet = true;
        var allDeprecated = true;
        var allUnDeprecated = true;

        foreach (var o in many)
        {
            allDisableable &= o is IDisableable;
            allHasFolder &= o is IHasFolder;
            allTableInfo &= o is TableInfo;
            allCatalogueItem &= o is CatalogueItem;
            allDeleteable &= o is IDeleteable;
            allExtractionFilterParameterSet &= o is ExtractionFilterParameterSet;

            if (o is IMightBeDeprecated d)
            {
                allDeprecated &= d.IsDeprecated;
                allUnDeprecated &= !d.IsDeprecated;
            }
            else
            {
                allDeprecated = false;
                allUnDeprecated = false;
            }
        }

        if (allDisableable)
            yield return new ExecuteCommandDisableOrEnable(_activator, many.Cast<IDisableable>().ToArray());
        if (allHasFolder)
            yield return new ExecuteCommandPutIntoFolder(_activator, many.Cast<IHasFolder>().ToArray(), null);
        if (allTableInfo)
            yield return new ExecuteCommandScriptTables(_activator, many.Cast<TableInfo>().ToArray(), null, null, null);
        if (allCatalogueItem)
            yield return new ExecuteCommandChangeExtractionCategory(_activator,
                many.Cast<CatalogueItem>()
                    .Select(ci => ci.ExtractionInformation)
                    .Where(ei => ei != null).ToArray(), null);
        if (allDeleteable)
            yield return new ExecuteCommandDelete(_activator, many.Cast<IDeleteable>().ToArray())
            { SuggestedShortcut = "Delete" };
        if (allExtractionFilterParameterSet)
            yield return new ExecuteCommandAddMissingParameters(_activator,
                many.Cast<ExtractionFilterParameterSet>().ToArray());

        // Deprecate/UnDeprecate many items at once if all share the same state (all true or all false)
        if (allDeprecated)
            yield return new ExecuteCommandDeprecate(_activator, many.Cast<IMightBeDeprecated>().ToArray(), false)
            {
                SuggestedShortcut = "UnDeprecate"
            };
        else if (allUnDeprecated)
            yield return new ExecuteCommandDeprecate(_activator, many.Cast<IMightBeDeprecated>().ToArray(), true)
            {
                SuggestedShortcut = "Deprecate"
            };
    }
}
