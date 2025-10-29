// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core.CommandExecution;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories.Construction;
using Rdmp.Dicom.TagPromotionSchema;
using Rdmp.Core.ReusableLibraryCode.Checks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rdmp.Dicom.CommandExecution;

/// <summary>
/// Adds a new column to a <see cref="Catalogue"/> based on either a dicom tag (with inferred datatype) or an explict name/datatype combination
/// </summary>
public class ExecuteCommandAddTag : BasicCommandExecution
{
    private readonly List<TagColumnAdder> _adders = new List<TagColumnAdder>();

    /// <summary>
    /// Add the given tag to the table under the <paramref name="catalogue"/>.
    /// </summary>
    /// <param name="activator">UI abstraction layer</param>
    /// <param name="catalogue">The Catalogue you want to add the tag to.  Must have a single table under it.</param>
    /// <param name="column">The name of a dicom tag</param>
    /// <param name="dataType">Optional.  Pass null to lookup the dicom tags datatype automatically (recommended).  Pass a value to use an explicit SQL DBMS datatype instead.</param>
    public ExecuteCommandAddTag(BasicActivateItems activator, ICatalogue catalogue,string column,string dataType)
        : this(activator,new[] { catalogue },column,dataType)
    {

    }

    /// <summary>
    /// Overload for use with the command line or for when adding the same column to multiple Catalogues
    /// </summary>
    /// <param name="activator">UI abstraction layer</param>
    /// <param name="catalogue">The Catalogue you want to add the tag to.  Must have a single table under it.</param>
    /// <param name="column">The name of a dicom tag</param>
    /// <param name="dataType">Optional.  Pass null to lookup the dicom tags datatype automatically (recommended).  Pass a value to use an explicit SQL DBMS datatype instead.</param>
    [UseWithObjectConstructor]
    public ExecuteCommandAddTag(BasicActivateItems activator,ICatalogue[] catalogues,
        [DemandsInitialization("Name of the new column you want created.")]
        string column,
        [DemandsInitialization("Optional when column is the name of a Dicom Tag e.g. StudyInstanceUID")]
        string dataType):base(activator)
    {
        foreach(var c in catalogues)
        {
            _adders.Add(BuildTagAdder(c,column, dataType));

            // once we can't process any Catalogue we should stop investigating
            if (IsImpossible)
                break;
        }
    }

    private TagColumnAdder BuildTagAdder(ICatalogue catalogue, string column, string dataType)
    {
        var tables = catalogue.GetTableInfosIdeallyJustFromMainTables();

        if (tables.Length != 1)
        {
            SetImpossible($"There are {tables.Length} tables mapped under Catalogue {catalogue}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(column))
        {
            SetImpossible("Column name must be supplied");
            return null;
        }

        var syntax = tables[0].GetQuerySyntaxHelper();

        //if user hasn't listed a specific datatype, guess it from the column
        if (string.IsNullOrWhiteSpace(dataType))
        {
            var available = TagColumnAdder.GetAvailableTags();

            if (!available.Contains(column))
            {
                var similar = available.Where(c => c.Contains(column)).ToArray();

                if (similar.Any())
                {
                    SetImpossible(
                        $"Could not find a tag called '{column}'. Possibly  you meant:{Environment.NewLine}{string.Join(Environment.NewLine, similar)}");
                    return null;
                }

                SetImpossible($"Could not find a tag called '{column}' or any like it");
                return null;
            }

            try
            {
                dataType = TagColumnAdder.GetDataTypeForTag(column, syntax.TypeTranslater);
            }
            catch (Exception e)
            {
                throw new Exception("No dataType was specified and column name could not be resolved to a DicomTag", e);
            }

        }

        return new TagColumnAdder(column, dataType, (TableInfo)tables[0], new AcceptAllCheckNotifier());
    }

    public override void Execute()
    {
        base.Execute();

        foreach(var adder in _adders)
            adder.Execute();
    }
}