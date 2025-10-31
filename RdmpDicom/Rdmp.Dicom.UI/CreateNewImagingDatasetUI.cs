// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FAnsi.Discovery;
using Rdmp.Dicom.CommandExecution;
using Rdmp.Dicom.PipelineComponents.DicomSources;
using Rdmp.UI.ItemActivation;
using DicomTypeTranslation.TableCreation;
using Rdmp.UI.SimpleDialogs;

namespace Rdmp.Dicom.UI;

public partial class CreateNewImagingDatasetUI : Form
{
    private readonly IActivateItems _activator;

    public CreateNewImagingDatasetUI(IActivateItems activator)
    {
        _activator = activator;

        InitializeComponent();
        serverDatabaseTableSelector1.HideTableComponents();
    }

    private void serverDatabaseTableSelector1_Load(object sender, EventArgs e)
    {

    }

    private bool CreateDatabaseIfNotExists(DiscoveredDatabase db)
    {
        if (db == null)
        {
            MessageBox.Show("Choose a database");
            return false;
        }

        if (db.Exists()) return true;
        if (MessageBox.Show($"Create database '{db}'", "Create", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return false;
        db.Create();

        return true;
    }

    private void btnCreateSuiteWithTemplate_Click(object sender, EventArgs e)
    {
        string filename;
        using (OpenFileDialog ofd = new()
               {
                   Filter = "Imaging Template|*.it"
               })
        {
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            filename = ofd.FileName;
        }

        try
        {
            var yaml = File.ReadAllText(filename);
            var template = ImageTableTemplateCollection.LoadFrom(yaml);
            CreateSuite(template);
        }
        catch (Exception exception)
        {
            ExceptionViewer.Show(exception);
        }
    }

    private void CreateSuite(ImageTableTemplateCollection template)
    {
        var db = serverDatabaseTableSelector1.GetDiscoveredDatabase();

        if (!CreateDatabaseIfNotExists(db))
            return;


        DirectoryInfo dir = null;
        using (FolderBrowserDialog dialog = new()
               {
                   Description = "Select Project Directory (For Sql scripts/Executables etc)"
               })
        {
            //if we are creating a load we need to know where to store load scripts etc
            if (cbCreateLoad.Checked)
                if (dialog.ShowDialog() == DialogResult.OK)
                    dir = new(dialog.SelectedPath);
                else
                    return;
        }

        var cmd = new ExecuteCommandCreateNewImagingDatasetSuite(_activator.RepositoryLocator, db, dir)
        {
            DicomSourceType = rbJsonSources.Checked
                ? typeof(DicomDatasetCollectionSource)
                : typeof(DicomFileCollectionSource),
            CreateCoalescer = cbMergeNullability.Checked,
            CreateLoad = cbCreateLoad.Checked,
            TablePrefix = tbPrefix.Text,
            Template = template
        };

        cmd.Execute();

        var firstCata = cmd.NewCataloguesCreated.First();
        if (firstCata != null)
            _activator.Publish(firstCata);

        MessageBox.Show("Create Suite Completed");
    }

    private void cbCreateLoad_CheckedChanged(object sender, EventArgs e)
    {
        cbMergeNullability.Enabled = cbCreateLoad.Checked;
        rbFileSources.Enabled = cbCreateLoad.Checked;
        rbJsonSources.Enabled = cbCreateLoad.Checked;
    }

}