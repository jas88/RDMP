// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.Linq;
using FAnsi.Discovery;
using Rdmp.Core.Repositories;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using DicomTypeTranslation.TableCreation;
using Rdmp.Core.CommandExecution;

namespace Rdmp.Dicom.CommandExecution;

public class ExecuteCommandCreateNewImagingDataset:BasicCommandExecution
{
    private readonly ImageTableTemplate _tableTemplate;
    private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
    private readonly DiscoveredTable _expectedTable;

    public ICatalogue NewCatalogueCreated { get; private set; }

    public ExecuteCommandCreateNewImagingDataset(IRDMPPlatformRepositoryServiceLocator repositoryLocator, DiscoveredTable expectedTable, ImageTableTemplate tableTemplate)
    {
        _repositoryLocator = repositoryLocator;
        _expectedTable = expectedTable;
        _tableTemplate = tableTemplate;
    }

    public override void Execute()
    {
        base.Execute();

        var tableCreator = new ImagingTableCreation(_expectedTable.Database.Server.GetQuerySyntaxHelper());
        tableCreator.CreateTable(_expectedTable, _tableTemplate);

        var importer = new TableInfoImporter(_repositoryLocator.CatalogueRepository, _expectedTable);
        importer.DoImport(out var tis, out var cis);

        var engineer = new ForwardEngineerCatalogue(tis, cis);
        engineer.ExecuteForwardEngineering(out var cata, out _, out var eis);

        var patientIdentifier = eis.SingleOrDefault(e => e.GetRuntimeName().Equals("PatientID"));

        if(patientIdentifier != null)
        {
            patientIdentifier.IsExtractionIdentifier = true;
            patientIdentifier.SaveToDatabase();
        }
        var seriesEi = eis.SingleOrDefault(e => e.GetRuntimeName().Equals("SeriesInstanceUID"));
        if (seriesEi != null)
        {
            seriesEi.IsExtractionIdentifier = true;
            seriesEi.SaveToDatabase();
        }

        //make it extractable
        _=new ExtractableDataSet(_repositoryLocator.DataExportRepository, cata);

        NewCatalogueCreated = cata;
    }
}