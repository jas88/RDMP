// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using Microsoft.Data.SqlClient;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Logging;

namespace LoadModules.Extensions.AutomationPlugins.Tests;

public class ObjectCreationTests : TestsRequiringAnAutomationPluginRepository
{
    [Test]
    public void CreateAllObjects()
    {
        //Schedule
        var proj = new Project(Repo.DataExportRepository, "My cool project");
        var schedule = new AutomateExtractionSchedule(Repo, proj);

        Assert.Multiple(() =>
        {
            Assert.That(schedule.Exists(), Is.True);
            Assert.That(proj.ID, Is.EqualTo(schedule.Project_ID));
        });

        //Configurations
        var config = new ExtractionConfiguration(Repo.DataExportRepository, proj)
        {
            Name = "Configuration1"
        };
        config.SaveToDatabase();

        //Permission to use a given configuration
        var automate = new AutomateExtraction(Repo,schedule,config);
        Assert.Multiple(() =>
        {
            Assert.That(config.ID, Is.EqualTo(automate.ExtractionConfigurationId));
            Assert.That(automate.Disabled, Is.EqualTo(false));
            Assert.That(automate.BaselineDate, Is.Null);
        });

        //Baseline (for when an extraction executes and a baseline is created for the datasets)

        //should cascade delete everything
        schedule.DeleteInDatabase();

        config.DeleteInDatabase();
        proj.DeleteInDatabase();

        Assert.Multiple(() =>
        {
            Assert.That(schedule.Exists(), Is.False);
            Assert.That(proj.Exists(), Is.False);
        });

    }

    [Test]
    public void CreateQueuedExecution()
    {
        var project = new Project(Repo.DataExportRepository,"proj");
        var configuration= new ExtractionConfiguration(Repo.DataExportRepository,project);

        var p = new Pipeline(Repo.CatalogueRepository);

        var que = new QueuedExtraction(Repo, configuration, p, DateTime.Now.AddHours(1));
        Assert.Multiple(() =>
        {
            Assert.That(que.Exists(), Is.True);
            Assert.That(que.DueDate, Is.GreaterThan(DateTime.Now));
        });

        que.DeleteInDatabase();

        p.DeleteInDatabase();

        project.DeleteInDatabase();

    }

    [Test]
    public void SimulateExtractExecutionOfANewBaseline()
    {
        //Schedule
        var proj = new Project(Repo.DataExportRepository, "My cool project");
        var config = new ExtractionConfiguration(Repo.DataExportRepository, proj);

        var cata = new Catalogue(Repo.CatalogueRepository, "MyDataset");
        var ds = new ExtractableDataSet(Repo.DataExportRepository,cata);

        var schedule = new AutomateExtractionSchedule(Repo, proj);
        var automateConfig = new AutomateExtraction(Repo, schedule, config);

        var results = new SuccessfullyExtractedResults(Repo, "Select * from CannonballLand", automateConfig,ds);

        //shouldn't be able to create a second audit of the same ds
        Assert.Throws<SqlException>(() => new SuccessfullyExtractedResults(Repo, "Select * from FantasyLand", automateConfig, ds));

        Assert.That(results.Exists(), Is.True);

        //ensures accumulator only has the lifetime of a single data load execution
        var acc = IdentifierAccumulator.GetInstance(DataLoadInfo.Empty);
        acc.AddIdentifierIfNotSee("123");
        acc.AddIdentifierIfNotSee("12");
        acc.AddIdentifierIfNotSee("123");

        acc.CommitCurrentState(Repo,automateConfig);

        var dt = automateConfig.GetIdentifiersTable();
        Assert.That(dt.Rows, Has.Count.EqualTo(2));

        //next dataset executes in parallel race conditions galore!
        acc = IdentifierAccumulator.GetInstance(DataLoadInfo.Empty);
        acc.AddIdentifierIfNotSee("22");
        acc.CommitCurrentState(Repo, automateConfig);

        dt = automateConfig.GetIdentifiersTable();
        Assert.That(dt.Rows, Has.Count.EqualTo(3));

        automateConfig.ClearBaselines();
        dt = automateConfig.GetIdentifiersTable();
        Assert.That(dt.Rows, Is.Empty);
    }
}