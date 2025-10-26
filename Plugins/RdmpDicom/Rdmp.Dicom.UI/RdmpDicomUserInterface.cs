// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using Rdmp.Dicom.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.Refreshing;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Providers.Nodes;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Dicom.ExternalApis;
using Rdmp.Core.CommandExecution;
using System.Collections.Generic;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Rdmp.Dicom.UI;

public class RdmpDicomUserInterface : PluginUserInterface, IRefreshBusSubscriber
{
    readonly IActivateItems _activator;

    public RdmpDicomUserInterface(IBasicActivateItems itemActivator) : base(itemActivator)
    {
        _activator = itemActivator as IActivateItems;
    }

    public override IEnumerable<IAtomicCommand> GetAdditionalRightClickMenuItems(object o)
    {
        if(_activator == null)
        {
            return Enumerable.Empty<IAtomicCommand>();
        }
        //IMPORTANT: if you are creating a menu array for a class in your own plugin instead create it as a Menu (See TagPromotionConfigurationMenu)

        var databaseEntity = o as DatabaseEntity;

        //allow clicking in Catalogue collection whitespace
        if (o is RDMPCollection.Catalogue)
            return new[] { new ExecuteCommandCreateNewImagingDataset(_activator) };
        return databaseEntity switch
        {
            Catalogue c => new IAtomicCommand[]
            {
                new ExecuteCommandCreateNewImagingDataset(_activator),
                new ExecuteCommandPromoteNewTag(_activator).SetTarget(databaseEntity),
                new Rdmp.Dicom.CommandExecution.ExecuteCommandCreateNewSemEHRCatalogue(_activator),
                new ExecuteCommandCompareImagingSchemas(_activator, c)
            },
            ProcessTask pt => new[] { new ExecuteCommandReviewIsolations(_activator, pt) },
            TableInfo => new[] { new ExecuteCommandPromoteNewTag(_activator).SetTarget(databaseEntity) },
            _ => o is AllExternalServersNode
                ? new[]
                {
                    new ExecuteCommandCreateNewExternalDatabaseServer(_activator, new SMIDatabasePatcher(),
                        PermissableDefaults.None)
                }
                : Array.Empty<IAtomicCommand>()
        };
    }

    public override object[] GetChildren(object model)
    {
        return null;
    }

    public override Image<Rgba32> GetImage(object concept, OverlayKind kind = OverlayKind.None)
    {
        return null;
    }

    public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
    {

    }

    public override bool CustomActivate(IMapsDirectlyToDatabaseTable o)
    {
        if(_activator == null)
        {
            return false;
        }

        if (o is not AggregateConfiguration ac) return base.CustomActivate(o);
        var api = new SemEHRApiCaller();

        if (!api.ShouldRun(ac)) return base.CustomActivate(o);
        var ui = new SemEHRUI(_activator, api, ac);
        ui.ShowDialog();
        return true;

    }
}