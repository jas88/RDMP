// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using DicomTypeTranslation.TableCreation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Icons.IconProvision;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.SimpleDialogs.SqlDialogs;
using Rdmp.Core.ReusableLibraryCode.Icons.IconProvision;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.IO;
using System.Windows.Forms;

namespace Rdmp.Dicom.UI.CommandExecution.AtomicCommands;

/// <summary>
/// Shows the differences made to a table after creation from a given imaging template
/// </summary>
class ExecuteCommandCompareImagingSchemas : BasicUICommandExecution
{
    private readonly ITableInfo _tableInfo;

    public ExecuteCommandCompareImagingSchemas(IActivateItems activator, Catalogue c):base(activator)
    {
        var tis = c.GetTableInfosIdeallyJustFromMainTables();

        if(tis.Length != 1)
            SetImpossible($"Catalogue has {tis.Length} underlying TableInfos");
        else
            _tableInfo = tis[0];
    }

    public override Image<Rgba32> GetImage(IIconProvider iconProvider)
    {
        return iconProvider.GetImage(RDMPConcept.Diff);
    }
    public override void Execute()
    {
        base.Execute();

        // Get the template for diff
        var file = Activator.SelectFile("Template File","Imaging Template","*.it");

        if(file == null)
            return;

        var templateCollection = ImageTableTemplateCollection.LoadFrom(File.ReadAllText(file.FullName));

        var comparer = new LiveVsTemplateComparer(_tableInfo,templateCollection);

        var viewer = new SQLBeforeAndAfterViewer(comparer.LiveSql,comparer.TemplateSql,"Your Database", "Template","Differences between live table and image template",MessageBoxButtons.OK); // lgtm[cs/local-not-disposed]
        viewer.Show();
    }

}