// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.
using Rdmp.Core.Curation.Data;
using System.Linq;

namespace Rdmp.Core.CommandExecution.AtomicCommands;
public sealed class ExecuteCommandDeleteDataset: BasicCommandExecution
{
    private readonly Curation.Data.Dataset _dataset;
    private readonly IBasicActivateItems _activator;
public ExecuteCommandDeleteDataset(IBasicActivateItems activator, [DemandsInitialization("The Dataset to delete")]Curation.Data.Dataset dataset)
    {
        _dataset = dataset;
        _activator = activator;
    }

    public override void Execute()
    {
        base.Execute();
        // Optimized: Use GetAllObjectsWhere to filter in SQL instead of loading entire ColumnInfo table
        var columnItemsLinkedToDataset = _activator.RepositoryLocator.CatalogueRepository
            .GetAllObjectsWhere<ColumnInfo>("Dataset_ID", _dataset.ID);
        foreach (var col in columnItemsLinkedToDataset)
        {
            col.Dataset_ID = null;
            col.SaveToDatabase();
        }
        _dataset.DeleteInDatabase();
    }
}
