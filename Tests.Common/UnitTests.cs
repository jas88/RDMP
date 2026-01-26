// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FAnsi;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandLine.Interactive;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cache;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Curation.Data.Cohort.Joinables;
using Rdmp.Core.Curation.Data.Dashboarding;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.Governance;
using Rdmp.Core.Curation.Data.ImportExport;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.Curation.Data.Remoting;
using Rdmp.Core.Curation.Data.Spontaneous;
using Rdmp.Core.Curation.DataHelper.RegexRedaction;
using Rdmp.Core.Databases;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataRelease;
using Rdmp.Core.DataExport.DataRelease.Audit;
using Rdmp.Core.DataExport.DataRelease.Potential;
using Rdmp.Core.DataLoad.Modules.DataFlowOperations;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.Setting;

namespace Tests.Common;

/// <summary>
/// Base class for all tests that want to create objects only in memory (and not in database like <see cref="DatabaseTests"/>)
/// </summary>
[Category("Unit")]
[CancelAfter(60000)] // 60 second timeout per test to identify hanging tests
public abstract class UnitTests
{
    protected MemoryDataExportRepository Repository = new();
    protected IRDMPPlatformRepositoryServiceLocator RepositoryLocator { get; private set; }

    //These types do not have to be supported by the method WhenIHaveA
    protected HashSet<string> SkipTheseTypes = new(new string[]
    {
        "TestColumn",
        "ExtractableCohort",
        "DQEGraphAnnotation",
        "Evaluation",
        "WindowLayout",
        "Dataset",
        // AutomationPlugins types - plugin-specific entities being integrated into core
        "SuccessfullyExtractedResults",
        "AutomateExtractionSchedule",
        "QueuedExtraction",
        "AutomateExtraction"
    });


    public UnitTests()
    {
        RepositoryLocator = new RepositoryProvider(Repository);
    }

    /// <summary>
    /// Returns an <see cref="IBasicActivateItems"/> based on the <see cref="RepositoryLocator"/>
    /// (or <paramref name="locator"/> if specified) that throws if input is sought (e.g.
    /// <see cref="IBasicActivateItems.YesNo(DialogArgs)"/>)
    /// </summary>
    /// <returns></returns>
    protected IBasicActivateItems GetActivator(IRDMPPlatformRepositoryServiceLocator locator = null) =>
        new ConsoleInputManager(locator ?? RepositoryLocator, ThrowImmediatelyCheckNotifier.Quiet)
        { DisallowInput = true };

    /// <summary>
    /// Override to do stuff before your first instance is constructed
    /// </summary>
    [OneTimeSetUp]
    protected virtual void OneTimeSetUp()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        ImplementationManager.Load<MicrosoftSQLImplementation>();
        ImplementationManager.Load<MySqlImplementation>();
        ImplementationManager.Load<OracleImplementation>();
        ImplementationManager.Load<PostgreSqlImplementation>();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Loads FAnsi implementations for all supported DBMS platforms into memory
    /// </summary>
    [SetUp]
    protected virtual void SetUp()
    {
        Console.WriteLine($"[TEST START] {TestContext.CurrentContext.Test.FullName}");
        Console.Out.Flush();
    }

    /// <summary>
    /// Creates a minimum viable object of Type T.  This includes the object and any dependencies e.g. a
    /// <see cref="ColumnInfo"/> cannot exist without a <see cref="TableInfo"/>.
    /// </summary>
    /// <typeparam name="T">Type of object you want to create</typeparam>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">If there is not yet an implementation for the given T.  Feel free to write one.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected T WhenIHaveA<T>() where T : DatabaseEntity => WhenIHaveA<T>(Repository);


    /// <summary>
    /// Factory registry mapping DatabaseEntity types to their creation functions.
    /// Built once at startup using lazy initialization.
    /// </summary>
    private static readonly Lazy<Dictionary<Type, Func<MemoryDataExportRepository, DatabaseEntity>>> _entityFactories =
        new(BuildEntityFactories, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Builds the factory registry for all supported DatabaseEntity types.
    /// This is called once at startup to avoid repeated type checking.
    /// </summary>
    private static Dictionary<Type, Func<MemoryDataExportRepository, DatabaseEntity>> BuildEntityFactories()
    {
        return new Dictionary<Type, Func<MemoryDataExportRepository, DatabaseEntity>>
        {
            [typeof(Catalogue)] = repo => Save(new Catalogue(repo, "Mycata")),

            [typeof(ExtendedProperty)] = repo =>
                new ExtendedProperty(repo, Save(new Catalogue(repo, "Mycata")), "TestProp", 0),

            [typeof(CatalogueItem)] = repo =>
            {
                var cata = new Catalogue(repo, "Mycata");
                return new CatalogueItem(repo, cata, "MyCataItem");
            },

            [typeof(ExtractionInformation)] = repo =>
            {
                var col = WhenIHaveA<ColumnInfo>(repo);
                var cata = new Catalogue(repo, "Mycata");
                // Ensure Catalogue is visible before creating dependent CatalogueItem
                cata.SaveAndFlush();
                var ci = new CatalogueItem(repo, cata, "MyCataItem");
                var ei = new ExtractionInformation(repo, ci, col, "MyCataItem");
                return Save(ei);
            },

            [typeof(TableInfo)] = repo =>
                new TableInfo(repo, "My_Table") { DatabaseType = DatabaseType.MicrosoftSQLServer },

            [typeof(ColumnInfo)] = repo =>
            {
                var ti = WhenIHaveA<TableInfo>(repo);
                // Ensure TableInfo is visible before creating dependent ColumnInfo
                ti.SaveAndFlush();
                var col = new ColumnInfo(repo, "My_Col", "varchar(10)", ti);
                return col;
            },

            [typeof(AggregateConfiguration)] = repo => WhenIHaveA(repo, out _, out _),

            [typeof(ExternalDatabaseServer)] = repo =>
                Save(new ExternalDatabaseServer(repo, "My Server", null)),

            [typeof(ANOTable)] = repo => WhenIHaveA(repo, out ExternalDatabaseServer _),

            [typeof(LoadMetadata)] = repo =>
            {
                //creates the table, column, catalogue, catalogue item and extraction information
                var ei = WhenIHaveA<ExtractionInformation>(repo);
                var cata = ei.CatalogueItem.Catalogue;

                var ti = ei.ColumnInfo.TableInfo;
                ti.Server = "localhost";
                ti.Database = "mydb";
                ti.SaveToDatabase();

                var lmd = new LoadMetadata(repo, "MyLoad");
                lmd.SaveToDatabase();
                cata.SaveToDatabase();
                lmd.LinkToCatalogue(cata);
                return lmd;
            },

            [typeof(AggregateTopX)] = repo =>
            {
                var agg = WhenIHaveA<AggregateConfiguration>(repo);
                return new AggregateTopX(repo, agg, 10);
            },

            [typeof(ConnectionStringKeyword)] = repo =>
                new ConnectionStringKeyword(repo, DatabaseType.MicrosoftSQLServer, "MultipleActiveResultSets", "true"),

            [typeof(DashboardLayout)] = repo =>
                new DashboardLayout(repo, "My Layout"),

            [typeof(DashboardControl)] = repo =>
            {
                var layout = WhenIHaveA<DashboardLayout>(repo);
                return Save(new DashboardControl(repo, layout, typeof(int), 0, 0, 100, 100, "")
                    { ControlType = "GoodBadCataloguePieChart" });
            },

            [typeof(DashboardObjectUse)] = repo =>
            {
                var layout = WhenIHaveA<DashboardLayout>(repo);
                var control = Save(new DashboardControl(repo, layout, typeof(int), 0, 0, 100, 100, "")
                    { ControlType = "GoodBadCataloguePieChart" });
                var use = new DashboardObjectUse(repo, control, WhenIHaveA<Catalogue>(repo));
                return use;
            },

            [typeof(ExtractionFilter)] = repo =>
            {
                var ei = WhenIHaveA<ExtractionInformation>(repo);
                return new ExtractionFilter(repo, "My Filter", ei);
            },

            [typeof(ExtractionFilterParameter)] = repo =>
            {
                var filter = WhenIHaveA<ExtractionFilter>(repo);
                filter.WhereSQL = "@myParam = 'T'";
                return new ExtractionFilterParameter(repo, "DECLARE @myParam varchar(10)", filter);
            },

            [typeof(ExtractionFilterParameterSetValue)] = repo =>
            {
                var parameter = WhenIHaveA<ExtractionFilterParameter>(repo);
                var set = new ExtractionFilterParameterSet(repo, parameter.ExtractionFilter, "Parameter Set");
                return new ExtractionFilterParameterSetValue(repo, set, parameter);
            },

            [typeof(ExtractionFilterParameterSet)] = repo =>
                WhenIHaveA<ExtractionFilterParameterSetValue>(repo).ExtractionFilterParameterSet,

            [typeof(Favourite)] = repo =>
                new Favourite(repo, WhenIHaveA<Catalogue>(repo)),

            [typeof(ObjectExport)] = repo =>
                WhenIHaveA(repo, out ShareManager _),

            [typeof(ObjectImport)] = repo =>
            {
                var export = WhenIHaveA(repo, out ShareManager sm);
                return sm.GetImportAs(export.SharingUID, WhenIHaveA<Catalogue>(repo));
            },

            [typeof(WindowLayout)] = repo =>
                new WindowLayout(repo, "My window arrangement", "<html><body>ignore this</body></html>"),

            [typeof(RemoteRDMP)] = repo =>
                new RemoteRDMP(repo),

            [typeof(CohortIdentificationConfiguration)] = repo =>
                new CohortIdentificationConfiguration(repo, "My cic"),

            [typeof(JoinableCohortAggregateConfiguration)] = repo =>
            {
                var config = WhenIHaveCohortAggregateConfiguration(repo, "PatientIndexTable");
                var cic = WhenIHaveA<CohortIdentificationConfiguration>(repo);
                cic.EnsureNamingConvention(config);
                return new JoinableCohortAggregateConfiguration(repo, cic, config);
            },

            [typeof(JoinableCohortAggregateConfigurationUse)] = repo =>
            {
                var joinable = WhenIHaveA<JoinableCohortAggregateConfiguration>(repo);
                var config = WhenIHaveCohortAggregateConfiguration(repo, "Aggregate");
                return joinable.AddUser(config);
            },

            [typeof(AggregateContinuousDateAxis)] = repo =>
            {
                var config = WhenIHaveA(repo, out var dateEi, out _);
                //remove the other Ei
                config.AggregateDimensions[0].DeleteInDatabase();
                //add the date one
                var dim = new AggregateDimension(repo, dateEi, config);
                return new AggregateContinuousDateAxis(repo, dim);
            },

            [typeof(AggregateDimension)] = repo =>
                WhenIHaveA<AggregateConfiguration>(repo).AggregateDimensions[0],

            [typeof(AggregateFilterContainer)] = repo =>
            {
                var config = WhenIHaveA<AggregateConfiguration>(repo);
                var container = new AggregateFilterContainer(repo, FilterContainerOperation.AND);
                config.RootFilterContainer_ID = container.ID;
                config.SaveToDatabase();
                return container;
            },

            [typeof(AggregateFilter)] = repo =>
            {
                var container = WhenIHaveA<AggregateFilterContainer>(repo);
                return new AggregateFilter(repo, "My Filter", container);
            },

            [typeof(AggregateFilterParameter)] = repo =>
            {
                var filter = WhenIHaveA<AggregateFilter>(repo);
                filter.WhereSQL = "@MyP = 'mnnn apples'";
                filter.SaveToDatabase();
                return (AggregateFilterParameter)filter.GetFilterFactory().CreateNewParameter(filter, "DECLARE @MyP as varchar(10)");
            },

            [typeof(LoadProgress)] = repo =>
                new LoadProgress(repo, WhenIHaveA<LoadMetadata>(repo)),

            [typeof(CacheProgress)] = repo =>
                new CacheProgress(repo, WhenIHaveA<LoadProgress>(repo)),

            [typeof(CacheFetchFailure)] = repo =>
                new CacheFetchFailure(repo, WhenIHaveA<CacheProgress>(repo),
                    DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)), DateTime.Now, new Exception("It didn't work")),

            [typeof(CohortAggregateContainer)] = repo =>
            {
                var cic = WhenIHaveA<CohortIdentificationConfiguration>(repo);
                cic.CreateRootContainerIfNotExists();
                return cic.RootCohortAggregateContainer;
            },

            [typeof(AnyTableSqlParameter)] = repo =>
            {
                var cic = WhenIHaveA<CohortIdentificationConfiguration>(repo);
                return new AnyTableSqlParameter(repo, cic, "DECLARE @myGlobal as varchar(10)");
            },

            [typeof(DataAccessCredentials)] = repo =>
                new DataAccessCredentials(repo, "My credentials"),

            [typeof(GovernancePeriod)] = repo =>
                new GovernancePeriod(repo),

            [typeof(GovernanceDocument)] = repo =>
            {
                var fi = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "myfile.txt"));
                return new GovernanceDocument(repo, WhenIHaveA<GovernancePeriod>(repo), fi);
            },

            [typeof(PermissionWindow)] = repo =>
                new PermissionWindow(repo),

            [typeof(JoinInfo)] = repo =>
            {
                WhenIHaveTwoTables(repo, out var col1, out var col2, out _);
                return new JoinInfo(repo, col1, col2, ExtractionJoinType.Left, null);
            },

            [typeof(Lookup)] = repo =>
            {
                WhenIHaveTwoTables(repo, out var col1, out var col2, out var col3);
                return new Lookup(repo, col3, col1, col2, ExtractionJoinType.Left, null);
            },

            [typeof(LookupCompositeJoinInfo)] = repo =>
            {
                var lookup = WhenIHaveA<Lookup>(repo);
                // Ensure TableInfo objects are visible before creating dependent ColumnInfo objects
                lookup.ForeignKey.TableInfo.SaveAndFlush();
                lookup.PrimaryKey.TableInfo.SaveAndFlush();

                var otherJoinFk = new ColumnInfo(repo, "otherJoinKeyForeign", "int", lookup.ForeignKey.TableInfo);
                var otherJoinPk = new ColumnInfo(repo, "otherJoinKeyPrimary", "int", lookup.PrimaryKey.TableInfo);
                return new LookupCompositeJoinInfo(repo, lookup, otherJoinFk, otherJoinPk);
            },

            [typeof(Pipeline)] = repo =>
                new Pipeline(repo, "My Pipeline"),

            [typeof(PipelineComponent)] = repo =>
                new PipelineComponent(repo, WhenIHaveA<Pipeline>(repo), typeof(ColumnForbidder), 0, "My Component"),

            [typeof(PipelineComponentArgument)] = repo =>
            {
                var comp = WhenIHaveA<PipelineComponent>(repo);
                return (PipelineComponentArgument)comp.CreateArgumentsForClassIfNotExists<ColumnForbidder>().First();
            },

            [typeof(PreLoadDiscardedColumn)] = repo =>
                new PreLoadDiscardedColumn(repo, WhenIHaveA<TableInfo>(repo), "MyDiscardedColumn"),

            [typeof(ProcessTask)] = repo =>
                new ProcessTask(repo, WhenIHaveA<LoadMetadata>(repo), LoadStage.AdjustRaw),

            [typeof(ProcessTaskArgument)] = repo =>
                new ProcessTaskArgument(repo, WhenIHaveA<ProcessTask>(repo)),

            [typeof(StandardRegex)] = repo =>
                new StandardRegex(repo),

            [typeof(SupportingSQLTable)] = repo =>
                new SupportingSQLTable(repo, WhenIHaveA<Catalogue>(repo), "Some Handy Query"),

            [typeof(TicketingSystemConfiguration)] = repo =>
                new TicketingSystemConfiguration(repo, "My Ticketing System"),

            [typeof(TicketingSystemReleaseStatus)] = repo =>
            {
                var ticketingSystem = WhenIHaveA<TicketingSystemConfiguration>(repo);
                ticketingSystem.SaveToDatabase();
                return new TicketingSystemReleaseStatus(repo, "my_status", null, ticketingSystem);
            },

            [typeof(SupportingDocument)] = repo =>
                new SupportingDocument(repo, WhenIHaveA<Catalogue>(repo), "HelpFile.docx"),

            [typeof(Project)] = repo =>
                new Project(repo, "My Project"),

            [typeof(ExtractionConfiguration)] = repo =>
                new ExtractionConfiguration(repo, WhenIHaveA<Project>(repo)),

            [typeof(ExtractableDataSet)] = repo =>
            {
                //To make an extractable dataset we need an extraction identifier (e.g. chi) that will be linked in the cohort
                var ei = WhenIHaveA<ExtractionInformation>(repo);
                ei.IsExtractionIdentifier = true;
                ei.SaveToDatabase();

                //And we need another column too just for sanity sakes (in the same table)
                var ci2 = new CatalogueItem(repo, ei.CatalogueItem.Catalogue, "ci2");
                var col2 = new ColumnInfo(repo, "My_Col2", "varchar(10)", ei.ColumnInfo.TableInfo);
                _ = new ExtractionInformation(repo, ci2, col2, col2.GetFullyQualifiedName());

                return new ExtractableDataSet(repo, ei.CatalogueItem.Catalogue);
            },

            [typeof(CumulativeExtractionResults)] = repo =>
                new CumulativeExtractionResults(repo,
                    WhenIHaveA<ExtractionConfiguration>(repo), WhenIHaveA<ExtractableDataSet>(repo),
                    "SELECT * FROM Anywhere"),

            [typeof(SelectedDataSets)] = repo =>
            {
                var eds = WhenIHaveA<ExtractableDataSet>(repo);
                var config = WhenIHaveA<ExtractionConfiguration>(repo);

                foreach (var ei in eds.Catalogue.GetAllExtractionInformation(ExtractionCategory.Any))
                    _ = new ExtractableColumn(repo, eds, config, ei, ei.Order, ei.SelectSQL);

                return new SelectedDataSets(repo, config, eds, null);
            },

            [typeof(ReleaseLog)] = repo =>
            {
                var file = Path.Combine(TestContext.CurrentContext.TestDirectory, "myDataset.csv");
                File.WriteAllText(file, "omg rows");

                var sds = WhenIHaveA<SelectedDataSets>(repo);
                _ = new CumulativeExtractionResults(repo, sds.ExtractionConfiguration, sds.ExtractableDataSet,
                    "SELECT * FROM ANYWHERE");
                var potential = new FlatFileReleasePotential(new RepositoryProvider(repo), sds);

                return new ReleaseLog(repo,
                    potential,
                    new ReleaseEnvironmentPotential(sds.ExtractionConfiguration),
                    false,
                    new DirectoryInfo(TestContext.CurrentContext.TestDirectory),
                    new FileInfo(file));
            },

            [typeof(ExtractableDataSetPackage)] = repo =>
                new ExtractableDataSetPackage(repo, "My Cool Package"),

            [typeof(SupplementalExtractionResults)] = repo =>
                new SupplementalExtractionResults(repo,
                    WhenIHaveA<CumulativeExtractionResults>(repo), "Select * from Lookup",
                    WhenIHaveA<SupportingSQLTable>(repo)),

            [typeof(SelectedDataSetsForcedJoin)] = repo =>
                new SelectedDataSetsForcedJoin(repo, WhenIHaveA<SelectedDataSets>(repo), WhenIHaveA<TableInfo>(repo)),

            [typeof(ProjectCohortIdentificationConfigurationAssociation)] = repo =>
                new ProjectCohortIdentificationConfigurationAssociation(repo,
                    WhenIHaveA<Project>(repo), WhenIHaveA<CohortIdentificationConfiguration>(repo)),

            [typeof(ExternalCohortTable)] = repo =>
                Save(new ExternalCohortTable(repo, "My cohorts", DatabaseType.MicrosoftSQLServer)
                {
                    Database = "MyCohortsDb",
                    DefinitionTableForeignKeyField = "c_id",
                    PrivateIdentifierField = "priv",
                    ReleaseIdentifierField = "rel",
                    TableName = "Cohorts",
                    DefinitionTableName = "InventoryTable",
                    Server = "localhost\\sqlexpress"
                }),

            [typeof(ExtractableCohort)] = repo =>
                throw new NotSupportedException(
                    "You should inherit from TestsRequiringACohort instead, cohorts have to exist to be constructed"),

            [typeof(GlobalExtractionFilterParameter)] = repo =>
                new GlobalExtractionFilterParameter(repo,
                    WhenIHaveA<ExtractionConfiguration>(repo), "DECLARE @ExtractionGlobal as varchar(100)"),

            [typeof(ExtractableColumn)] = repo =>
            {
                var ei = WhenIHaveA<ExtractionInformation>(repo);
                var eds = new ExtractableDataSet(repo, ei.CatalogueItem.Catalogue);
                var config = WhenIHaveA<ExtractionConfiguration>(repo);
                config.AddDatasetToConfiguration(eds);
                return config.GetAllExtractableColumnsFor(eds).Single();
            },

            [typeof(FilterContainer)] = repo =>
            {
                var sds = WhenIHaveA<SelectedDataSets>(repo);
                var container = new FilterContainer(repo, FilterContainerOperation.AND);
                sds.RootFilterContainer_ID = container.ID;
                sds.SaveToDatabase();
                return container;
            },

            [typeof(DeployedExtractionFilter)] = repo =>
            {
                var container = WhenIHaveA<FilterContainer>(repo);
                return new DeployedExtractionFilter(repo, "Fish = 'haddock'", container);
            },

            [typeof(DeployedExtractionFilterParameter)] = repo =>
            {
                var filter = WhenIHaveA<DeployedExtractionFilter>(repo);
                filter.WhereSQL = "@had = 'enough'";
                return (DeployedExtractionFilterParameter)filter.GetFilterFactory().CreateNewParameter(filter, "DECLARE @had as varchar(100)");
            },

            [typeof(ExtractionProgress)] = repo =>
            {
                var cata = new Catalogue(repo, "MyCata");
                var cataItem = new CatalogueItem(repo, cata, "MyCol");
                var table = new TableInfo(repo, "MyTable");
                // Ensure TableInfo is visible before creating dependent ColumnInfo
                table.SaveAndFlush();
                var col = new ColumnInfo(repo, "mycol", "datetime", table);

                var ei = new ExtractionInformation(repo, cataItem, col, "mycol");
                cata.TimeCoverage_ExtractionInformation_ID = ei.ID;
                cata.SaveToDatabase();

                var eds = new ExtractableDataSet(repo, cata);
                var project = new Project(repo, "My Proj");
                var config = new ExtractionConfiguration(repo, project);
                var sds = new SelectedDataSets(repo, config, eds, null);

                return new ExtractionProgress(repo, sds);
            },

            [typeof(Commit)] = repo =>
                new Commit(repo, Guid.NewGuid(), "Breaking stuff"),

            [typeof(Memento)] = repo =>
            {
                var commit = WhenIHaveA<Commit>(repo);
                var cata = WhenIHaveA<Catalogue>(repo);
                return new Memento(repo, commit, MementoType.Add, cata, null, "placeholder");
            },

            [typeof(LoadMetadataCatalogueLinkage)] = repo =>
            {
                var cata = WhenIHaveA<Catalogue>(repo);
                var lmd = WhenIHaveA<LoadMetadata>(repo);
                return new LoadMetadataCatalogueLinkage(repo, lmd, cata);
            },

            [typeof(Setting)] = repo =>
                new Setting(repo.CatalogueRepository, "", ""),

            [typeof(RegexRedaction)] = repo =>
                new RegexRedaction(repo.CatalogueRepository, 0, 0, "", "", 0, new Dictionary<ColumnInfo, string>()),

            [typeof(RegexRedactionConfiguration)] = repo =>
                new RegexRedactionConfiguration(repo.CatalogueRepository, "name", new System.Text.RegularExpressions.Regex(".*"), "T"),

            [typeof(RegexRedactionKey)] = repo =>
                new RegexRedactionKey(repo.CatalogueRepository, WhenIHaveA<RegexRedaction>(repo), WhenIHaveA<ColumnInfo>(repo), "PK"),

            [typeof(ExtractableDataSetProject)] = repo =>
                new ExtractableDataSetProject(repo, WhenIHaveA<ExtractableDataSet>(repo), WhenIHaveA<Project>(repo))
        };
    }

    /// <summary>
    /// Creates a minimum viable object of Type T using the factory registry.
    /// This includes the object and any dependencies e.g. a <see cref="ColumnInfo"/> cannot exist without a <see cref="TableInfo"/>.
    /// </summary>
    /// <typeparam name="T">Type of object you want to create</typeparam>
    /// <returns></returns>
    /// <exception cref="TestCaseNotWrittenYetException">If there is not yet an implementation for the given T.</exception>
    public static T WhenIHaveA<T>(MemoryDataExportRepository repository) where T : DatabaseEntity
    {
        if (_entityFactories.Value.TryGetValue(typeof(T), out var factory))
            return (T)factory(repository);

        throw new TestCaseNotWrittenYetException(typeof(T));
    }

    private static void WhenIHaveTwoTables(MemoryDataExportRepository repository, out ColumnInfo col1,
        out ColumnInfo col2, out ColumnInfo col3)
    {
        WhenIHaveTwoTables(repository, out _, out _, out col1, out col2, out col3);
    }

    private static void WhenIHaveTwoTables(MemoryDataExportRepository repository, out TableInfo ti1, out TableInfo ti2,
        out ColumnInfo col1, out ColumnInfo col2, out ColumnInfo col3)
    {
        ti1 = WhenIHaveA<TableInfo>(repository);
        ti1.Name = "ParentTable";
        ti1.Database = "MyDb";
        ti1.SaveAndFlush();
        col1 = new ColumnInfo(repository, "ParentCol", "varchar(10)", ti1);

        ti2 = WhenIHaveA<TableInfo>(repository);
        ti2.Name = "ChildTable";
        ti2.Database = "MyDb";
        // Ensure TableInfo is visible before creating dependent ColumnInfo objects
        ti2.SaveAndFlush();
        col2 = new ColumnInfo(repository, "ChildCol", "varchar(10)", ti2);
        col3 = new ColumnInfo(repository, "Desc", "varchar(10)", ti2);
    }

    private static AggregateConfiguration WhenIHaveCohortAggregateConfiguration(MemoryDataExportRepository repository,
        string name)
    {
        var config = WhenIHaveA<AggregateConfiguration>(repository);
        config.Name = name;
        config.SaveToDatabase();

        var ei = config.AggregateDimensions[0].ExtractionInformation;
        ei.IsExtractionIdentifier = true;
        ei.SaveToDatabase();
        return config;
    }

    /// <inheritdoc cref="WhenIHaveA{T}()"/>
    protected static AggregateConfiguration WhenIHaveA(MemoryDataExportRepository repository,
        out ExtractionInformation dateEi, out ExtractionInformation otherEi)
    {
        var ti = WhenIHaveA<TableInfo>(repository);
        var dateCol = new ColumnInfo(repository, "MyDateCol", "datetime2", ti);
        var otherCol = new ColumnInfo(repository, "MyOtherCol", "varchar(10)", ti);

        var cata = WhenIHaveA<Catalogue>(repository);
        var dateCi = new CatalogueItem(repository, cata, dateCol.Name);
        dateEi = new ExtractionInformation(repository, dateCi, dateCol, dateCol.Name);
        var otherCi = new CatalogueItem(repository, cata, otherCol.Name);
        otherEi = new ExtractionInformation(repository, otherCi, otherCol, otherCol.Name);

        var config = new AggregateConfiguration(repository, cata, "My graph");
        _ = new AggregateDimension(repository, otherEi, config);
        return config;
    }

    /// <inheritdoc cref="WhenIHaveA{T}()"/>
    protected static ANOTable WhenIHaveA(MemoryDataExportRepository repository, out ExternalDatabaseServer server)
    {
        server = new ExternalDatabaseServer(repository, "ANO Server", new ANOStorePatcher());
        return new ANOTable(repository, server, "ANOFish", "F");
    }

    /// <inheritdoc cref="WhenIHaveA{T}()"/>
    protected static ObjectExport WhenIHaveA(MemoryDataExportRepository repository, out ShareManager shareManager)
    {
        shareManager = new ShareManager(new RepositoryProvider(repository));
        return shareManager.GetNewOrExistingExportFor(WhenIHaveA<Catalogue>(repository));
    }

    private static T Save<T>(T s) where T : ISaveable
    {
        s.SaveToDatabase();
        return s;
    }

    //Fields that can be safely ignored when comparing an object created in memory with one created into the database.
    private static readonly string[] IgnorePropertiesWhenDiffing =
        { "ID", "Repository", "CatalogueRepository", "SoftwareVersion" };

    public static Dictionary<PropertyInfo, HashSet<object>> _alreadyChecked = new();

    /// <summary>
    /// Asserts that the two objects are basically the same except for IDs/Repositories.  This includes checking all public properties
    /// that are not in the <see cref="IgnorePropertiesWhenDiffing"/> list.  Date fields will be validated as equal if they are within
    /// 10 seconds of each other (<see cref="AreAboutTheSameTime"/>).
    /// </summary>
    /// <param name="memObj"></param>
    /// <param name="dbObj"></param>
    /// <param name="firstIteration"></param>
    public static void AssertAreEqual(IMapsDirectlyToDatabaseTable memObj, IMapsDirectlyToDatabaseTable dbObj,
        bool firstIteration = true)
    {
        if (firstIteration)
            _alreadyChecked.Clear();

        foreach (var property in memObj.GetType().GetProperties())
        {
            if (IgnorePropertiesWhenDiffing.Contains(property.Name) || property.Name.EndsWith("_ID"))
                continue;

            if (!_alreadyChecked.ContainsKey(property))
                _alreadyChecked.Add(property, new HashSet<object>());

            //if we have already checked this property
            if (_alreadyChecked[property].Contains(memObj))
                return; //don't check it again

            _alreadyChecked[property].Add(memObj);

            object memValue = null;
            object dbValue = null;
            try
            {
                memValue = property.GetValue(memObj);
            }
            catch (Exception e)
            {
                Assert.Fail($"{memObj.GetType().Name} Property {property.Name} could not be read from Memory:\r\n{e}");
            }

            try
            {
                dbValue = property.GetValue(dbObj);
            }
            catch (Exception e)
            {
                Assert.Fail($"{dbObj.GetType().Name} Property {property.Name} could not be read from Database:\r\n{e}");
            }

            if (memValue is IMapsDirectlyToDatabaseTable table)
            {
                AssertAreEqual(table, (IMapsDirectlyToDatabaseTable)dbValue, false);
                return;
            }

            if (memValue is IEnumerable<IMapsDirectlyToDatabaseTable> tables)
            {
                AssertAreEqual(tables, (IEnumerable<IMapsDirectlyToDatabaseTable>)dbValue, false);
                return;
            }

            if (memValue is DateTime memTime && dbValue is DateTime dbTime)
                if (!AreAboutTheSameTime(memTime, dbTime))
                    Assert.Fail($"Dates differed, {memObj.GetType().Name} Property {property.Name} differed Memory={memTime} and Db={dbTime}");
                else
                    return;

            //treat empty strings as the same as
            memValue = memValue as string == string.Empty ? null : memValue;
            dbValue = dbValue as string == string.Empty ? null : dbValue;

            //all other properties should be legit
            Assert.That(memValue, Is.EqualTo(dbValue), $"{memObj.GetType().Name} Property {property.Name} differed Memory='{memValue}' and Db='{dbValue}'");
        }
    }

    public static void AssertAreEqual(IEnumerable<IMapsDirectlyToDatabaseTable> memObjects,
        IEnumerable<IMapsDirectlyToDatabaseTable> dbObjects, bool firstIteration = true)
    {
        var memObjectsArr = memObjects.OrderBy(o => o.ID).ToArray();
        var dbObjectsArr = dbObjects.OrderBy(o => o.ID).ToArray();

        Assert.That(memObjectsArr.Length == dbObjectsArr.Length);

        for (var i = 0; i < memObjectsArr.Length; i++)
            AssertAreEqual(memObjectsArr[i], dbObjectsArr[i], firstIteration);
    }

    /// <summary>
    /// The number of seconds that have to differ between two DateTime objects in method <see cref="AreAboutTheSameTime"/> before
    /// they are considered not the same time
    /// </summary>
    private const double TimeThresholdInSeconds = 60;

    private static bool AreAboutTheSameTime(DateTime memValue, DateTime dbValue) =>
        Math.Abs(memValue.Subtract(dbValue).TotalSeconds) < TimeThresholdInSeconds;


    /// <summary>
    /// Returns instances of all Types supported by <see cref="WhenIHaveA{T}()"/>
    /// </summary>
    /// <returns></returns>
    protected IEnumerable<DatabaseEntity> WhenIHaveAll()
    {
        var methodWhenIHaveA = GetWhenIHaveAMethod();
        var repo = new object[] { Repository };
        var types = typeof(Catalogue).Assembly.GetTypes()
            .Where(t => !t.Name.StartsWith("Spontaneous") && !SkipTheseTypes.Contains(t.Name) &&
                        typeof(DatabaseEntity).IsAssignableFrom(t) && !typeof(SpontaneousObject).IsAssignableFrom(t) &&
                        !t.IsAbstract && !t.IsInterface);

        foreach (var t in types)
        {
            //ensure that the method supports the Type
            yield return (DatabaseEntity)methodWhenIHaveA.MakeGenericMethod(t).Invoke(this, repo);
        }
    }

    /// <summary>
    /// Returns a properly initialized object of Type <paramref name="t"/> which must be a <see cref="DatabaseEntity"/> that
    /// is supported by <see cref="UnitTests"/>
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public IMapsDirectlyToDatabaseTable WhenIHaveA(Type t)
    {
        var methodWhenIHaveA = GetWhenIHaveAMethod();
        //ensure that the method supports the Type
        var genericWhenIHaveA = methodWhenIHaveA.MakeGenericMethod(t);
        return (DatabaseEntity)genericWhenIHaveA.Invoke(this, null);
    }

    private MethodInfo GetWhenIHaveAMethod()
    {
        return typeof(UnitTests).GetMethod(nameof(WhenIHaveA), 1, new[] { typeof(MemoryDataExportRepository) });
    }
}