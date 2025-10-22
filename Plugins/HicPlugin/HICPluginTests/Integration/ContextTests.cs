﻿using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Caching.Pipeline;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataLoad.Modules.DataProvider;
using Rdmp.Core.Repositories;
using SCIStorePlugin.Cache.Pipeline;
using SCIStorePlugin.Data;
using Tests.Common;

namespace SCIStorePluginTests.Integration;

internal class PipelineDatabaseTestHelper
{
    public Pipeline Pipe { get; private set; }

    public IPipelineUseCase PipelineUseCase { get; private set; }

    public void Setup(CatalogueRepository catalogueRepository)
    {
        //cleanup old one
        foreach (var c in catalogueRepository.GetAllObjects<Pipeline>().Where(p => p.Name.Equals("Deleteme")))
            c.DeleteInDatabase();

        Pipe = new Pipeline(catalogueRepository, "Deleteme");

        var component = new PipelineComponent(catalogueRepository, Pipe, typeof(SCIStoreWebServiceSource), -100, "bob");
        var destination = new PipelineComponent(catalogueRepository, Pipe, typeof(SCIStoreCacheDestination), 100, "destination");

        //setting the source correctly
        Pipe.SourcePipelineComponent_ID = component.ID;
        Pipe.DestinationPipelineComponent_ID = destination.ID;
        Pipe.SaveToDatabase();

        var config = new WebServiceConfiguration(catalogueRepository) { Username = "bob", Password = "fish" };

        var arg = component.CreateArgumentsForClassIfNotExists<SCIStoreWebServiceSource>()
            .Single(a => a.Name.Equals("Configuration"));
        arg.SetValue(config);
        arg.SaveToDatabase();

        var args = destination.CreateArgumentsForClassIfNotExists<SCIStoreCacheDestination>()
            .ToList();

        arg = args.Single(a => a.Name.Equals("HealthBoard"));
        arg.SetValue(HealthBoard.T);
        arg.SaveToDatabase();

        arg = args.Single(a => a.Name.Equals("Discipline"));
        arg.SetValue(Discipline.Biochemistry);
        arg.SaveToDatabase();

        var testDirHelper = new TestDirectoryHelper(GetType());
        testDirHelper.SetUp();
        var root = LoadDirectory.CreateDirectoryStructure(testDirHelper.Directory, "Test", true).RootPath.FullName;
        var _lmd = new LoadMetadata(catalogueRepository, "JobDateGenerationStrategyFactoryTestsIntegration")
        {
            LocationOfForLoadingDirectory = Path.Join(root, "Data", "ForLoading"),
            LocationOfForArchivingDirectory = Path.Join(root, "Data", "ForArchiving"),
            LocationOfCacheDirectory = Path.Join(root, "Cahce"),
            LocationOfExecutablesDirectory = Path.Join(root, "Executables"),
        };
        _lmd.SaveToDatabase();

        var _lp = new LoadProgress(catalogueRepository, _lmd)
        {
            OriginDate = new DateTime(2001, 1, 1),
            DataLoadProgress = new DateTime(2001, 1, 1)
        };

        _lp.SaveToDatabase();

        var _cp = new CacheProgress(catalogueRepository, _lp)
        {
            Pipeline_ID = Pipe.ID
        };
        _cp.SaveToDatabase();

        PipelineUseCase = new CachingPipelineUseCase(_cp);
    }

    public void Teardown()
    {
        Pipe.DeleteInDatabase();
    }
}

public class ContextTests : DatabaseTests
{
    private PipelineDatabaseTestHelper _pipelineDatabaseHelper;

    private Exception _setupException;

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        try
        {
            _pipelineDatabaseHelper = new PipelineDatabaseTestHelper();
            _pipelineDatabaseHelper.Setup((CatalogueRepository)CatalogueRepository);
        }
        catch (Exception e)
        {
            _setupException = e;
        }
    }

    [OneTimeTearDown]
    public void AfterAllTests()
    {
        _pipelineDatabaseHelper.Teardown();
    }


    [Test]
    public void GetPipelineWorks()
    {
        if (_setupException != null)
            throw new Exception("Crashed during setup", _setupException);

        Assert.That(_pipelineDatabaseHelper.Pipe, Is.Not.Null);
    }
}