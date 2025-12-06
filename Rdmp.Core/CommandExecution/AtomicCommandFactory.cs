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
using Rdmp.Core.Providers.Nodes;
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

    /// <summary>
    /// Maps SuggestedCategory values to default weights for menu ordering.
    /// Lower weights appear first in menus. Separators are added between integer buckets.
    /// </summary>
    private static readonly Dictionary<string, float> CategoryWeights = new()
    {
        { GoTo, -100f },        // Navigation commands at very top
        { Add, -99f },          // Add/create commands near top
        { New, -99f },          // New commands near top
        { View, -98f },         // View commands high priority
        { Extraction, -97f },   // Extraction settings
        { Dimensions, -96f },   // Dimension settings
        { Metadata, -95f },     // Metadata operations
        { Alter, -90f },        // Alter/modify operations
        { SetContainerOperation, -85f },
        { SetUsageContext, -85f },
        { Deprecation, -50f },  // Deprecation commands mid-priority
        { Advanced, 1f },       // Advanced operations lower priority
        { Batching, 2f }        // Batch operations at bottom
    };

    /// <summary>
    /// Maps specific command types to their default weights when no category is set.
    /// This restores the menu ordering that was previously achieved through explicit
    /// Weight assignments in the manual yield statements.
    /// </summary>
    private static readonly Dictionary<Type, float> TypeWeights = new()
    {
        // View commands - highest priority
        { typeof(ExecuteCommandViewData), -99.5f },
        { typeof(ExecuteCommandViewLogs), -99.4f },
        { typeof(ExecuteCommandViewExtractionSql), -99.3f },

        // Add commands
        { typeof(ExecuteCommandAddNewCatalogueItem), -99.9f },
        { typeof(ExecuteCommandAddNewAggregateGraph), -98.9f },
        { typeof(ExecuteCommandAddNewSupportingDocument), -87.8f },
        { typeof(ExecuteCommandAddNewSupportingSqlTable), -87.9f },

        // Extraction commands
        { typeof(ExecuteCommandMakeCatalogueInternal), -99.01f },
        { typeof(ExecuteCommandMakeCatalogueNotInternal), -99.01f },
        { typeof(ExecuteCommandMakeCatalogueProjectSpecific), -99.01f },
        { typeof(ExecuteCommandSetExtractionIdentifier), -99.01f },

        // Clone/association commands
        { typeof(ExecuteCommandCloneCohortIdentificationConfiguration), -50.4f },
        { typeof(ExecuteCommandCloneExtractionConfiguration), 1.3f },
        { typeof(ExecuteCommandClonePipeline), -50.4f },

        // Configuration commands
        { typeof(ExecuteCommandFreezeExtractionConfiguration), 1.2f },
        { typeof(ExecuteCommandUnfreezeExtractionConfiguration), 1.2f },
        { typeof(ExecuteCommandResetExtractionProgress), 1.4f },
        { typeof(ExecuteCommandGenerateReleaseDocument), -99.4f },

        // Common menu items - give negative weight so they separate from custom commands (Weight -1 = bucket -1)
        { typeof(ExecuteCommandAddFavourite), -1f }
        // Note: ExecuteCommandAddToSession, ExecuteCommandRefreshObject, ExecuteCommandShowKeywordHelp,
        // ExecuteCommandViewCommits are in Rdmp.UI and will get weights applied there
    };

    /// <summary>
    /// Applies a default weight to the command based on its SuggestedCategory or Type.
    /// Only sets weight if command hasn't already been assigned a non-zero weight.
    /// </summary>
    private static void ApplyDefaultWeight(IAtomicCommand cmd)
    {
        // Don't override explicitly set weights
        if (cmd.Weight != 0)
            return;

        // First try category-based weight
        if (cmd.SuggestedCategory != null && CategoryWeights.TryGetValue(cmd.SuggestedCategory, out var categoryWeight))
        {
            cmd.Weight = categoryWeight;
            return;
        }

        // Fall back to type-based weight
        if (TypeWeights.TryGetValue(cmd.GetType(), out var typeWeight))
            cmd.Weight = typeWeight;
    }

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
            ApplyDefaultWeight(cmd);
            yield return cmd;
        }

        // Special case: Activate command (always try if activatable)
        if (_activator.CanActivate(o))
        {
            var activateCmd = new ExecuteCommandActivate(_activator, o);
            ApplyDefaultWeight(activateCmd);
            yield return activateCmd;
        }

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

            // Try to construct the command with available parameters (using AOT-compatible constructor)
            var cmd = AotObjectConstructor.ConstructIfPossible(cmdType, _activator, o) as IAtomicCommand;

            // Only yield commands that were successfully constructed and are possible
            if (cmd?.IsImpossible == false)
            {
                ApplyDefaultWeight(cmd);
                yield return cmd;
            }
        }

        // Special case: ArbitraryFolderNode with CommandGetter delegate
        // Commands from CommandGetter get weight -1.0 to create a separate bucket from auto-discovered
        // commands (bucket 0) while staying above GoTo commands (bucket -100). This ensures a separator
        // is added between custom folder commands and common menu items.
        // Note: The bucket is calculated as (int)weight, so -1.0 gives bucket -1, distinct from bucket 0.
        if (o is ArbitraryFolderNode f && f.CommandGetter != null)
            foreach (var cmd in f.CommandGetter())
            {
                if (cmd.Weight == 0)
                    cmd.Weight = -1.0f;
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
        var allDisableable = many.Cast<object>().All(o => o is IDisableable);
        var allHasFolder = many.Cast<object>().All(o => o is IHasFolder);
        var allTableInfo = many.Cast<object>().All(o => o is TableInfo);
        var allCatalogueItem = many.Cast<object>().All(o => o is CatalogueItem);
        var allDeleteable = many.Cast<object>().All(o => o is IDeleteable);
        var allExtractionFilterParameterSet = many.Cast<object>().All(o => o is ExtractionFilterParameterSet);
        var allDeprecated = many.Cast<object>().All(o => o is IMightBeDeprecated d && d.IsDeprecated);
        var allUnDeprecated = many.Cast<object>().All(o => o is IMightBeDeprecated d && !d.IsDeprecated);

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
