// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Aggregation;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.MapsDirectlyToDatabaseTable;

namespace Rdmp.Core.Tests.CommandExecution;

/// <summary>
/// Tests for <see cref="ExecuteCommandCreateNewFilter" />
/// </summary>
internal class ExecuteCommandCreateNewFilterCliTests : CommandCliTests
{
    [Test]
    public void OfferCatalogueFilters_ForDatasetWhoseCatalogueWasDeleted_OffersNoFilters()
    {
        var sds = WhenIHaveA<SelectedDataSets>();

        // ExtractableDataSet.Catalogue is null once its Catalogue has been deleted
        sds.ExtractableDataSet.Catalogue_ID = int.MaxValue;
        Assert.That(sds.GetCatalogue(), Is.Null);

        var cmd = new ExecuteCommandCreateNewFilter(GetMockActivator(), sds) { OfferCatalogueFilters = true };

        Assert.DoesNotThrow(cmd.Execute);
        Assert.That(cmd.ReasonCommandImpossible, Does.Contain("There are no Filters declared"));
    }

    [Test]
    public void OfferCatalogueFilters_ForAggregate_OffersCatalogueFilters()
    {
        var aggregate = WhenIHaveA<AggregateConfiguration>();
        var catalogueFilter = AddCatalogueFilter(aggregate);

        Assert.That(GetFiltersOffered(aggregate), Is.EquivalentTo(new[] { catalogueFilter }));
    }

    [Test]
    public void OfferCatalogueFilters_ForConfigurationWithNoCohort_OffersCatalogueFilters()
    {
        var sds = WhenIHaveA<SelectedDataSets>();
        var catalogueFilter = AddCatalogueFilter(sds);

        // no cohort has been chosen for the extraction yet
        Assert.That(sds.ExtractionConfiguration.Cohort, Is.Null);

        Assert.That(GetFiltersOffered(sds), Is.EquivalentTo(new[] { catalogueFilter }));
    }

    [Test]
    public void OfferCatalogueFilters_ForCohortIdentificationConfigurationWithNoRootContainer_OffersCatalogueFilters()
    {
        var sds = WhenIHaveA<SelectedDataSets>();
        var catalogueFilter = AddCatalogueFilter(sds);

        var cic = new CohortIdentificationConfiguration(Repository, "My cic");
        Assert.That(cic.RootCohortAggregateContainer, Is.Null);
        SetCohortFrom(sds, cic);

        Assert.That(GetFiltersOffered(sds), Is.EquivalentTo(new[] { catalogueFilter }));
    }

    [Test]
    public void OfferCatalogueFilters_ForCohortSetWithNoFilterContainer_OffersFiltersOfTheOtherCohortSets()
    {
        var sds = WhenIHaveA<SelectedDataSets>();
        var catalogue = sds.GetCatalogue();
        var catalogueFilter = AddCatalogueFilter(sds);

        var cic = new CohortIdentificationConfiguration(Repository, "My cic");
        cic.CreateRootContainerIfNotExists();
        SetCohortFrom(sds, cic);

        // a cohort set that has never had a filter has no root filter container
        var withoutFilters = new AggregateConfiguration(Repository, catalogue, "Everyone");
        cic.RootCohortAggregateContainer.AddChild(withoutFilters, 0);
        Assert.That(withoutFilters.RootFilterContainer, Is.Null);

        var withFilters = new AggregateConfiguration(Repository, catalogue, "Some people");
        var container = new AggregateFilterContainer(Repository, FilterContainerOperation.AND);
        withFilters.RootFilterContainer_ID = container.ID;
        withFilters.SaveToDatabase();
        var cohortSetFilter = new AggregateFilter(Repository, "Cohort set filter", container);
        cic.RootCohortAggregateContainer.AddChild(withFilters, 1);

        Assert.That(GetFiltersOffered(sds), Is.EquivalentTo(new IFilter[] { catalogueFilter, cohortSetFilter }));
    }

    private ExtractionFilter AddCatalogueFilter(IRootFilterContainerHost host) =>
        new(Repository, "Catalogue filter",
            host.GetCatalogue().GetAllExtractionInformation(ExtractionCategory.Any).First());

    /// <summary>
    /// Gives the extraction configuration of <paramref name="sds"/> a cohort which <see cref="ExecuteCommandCreateNewFilter"/>
    /// takes to come from <paramref name="cic"/>, because it looks the configuration up by the cohort's
    /// <see cref="ExtractableCohort.OriginID"/>
    /// </summary>
    private void SetCohortFrom(SelectedDataSets sds, CohortIdentificationConfiguration cic)
    {
        var cohort = new ExtractableCohort();
        Repository.InsertAndHydrate(cohort, new Dictionary<string, object>
        {
            { nameof(ExtractableCohort.OriginID), cic.ID },
            { nameof(ExtractableCohort.ExternalCohortTable_ID), WhenIHaveA<ExternalCohortTable>().ID }
        });

        var config = sds.ExtractionConfiguration;
        config.Cohort_ID = cohort.ID;
        config.SaveToDatabase();
    }

    /// <summary>
    /// Runs <see cref="ExecuteCommandCreateNewFilter"/> with <see cref="ExecuteCommandCreateNewFilter.OfferCatalogueFilters"/>
    /// for <paramref name="host"/>, and returns the filters it offers to import. None of them is chosen.
    /// </summary>
    private IMapsDirectlyToDatabaseTable[] GetFiltersOffered(IRootFilterContainerHost host)
    {
        IMapsDirectlyToDatabaseTable[] offered = null;

        var activator = Substitute.For<IBasicActivateItems>();
        activator.RepositoryLocator.Returns(RepositoryLocator);
        activator.SelectMany(Arg.Any<DialogArgs>(), Arg.Any<Type>(), Arg.Any<IMapsDirectlyToDatabaseTable[]>())
            .Returns(call =>
            {
                offered = call.ArgAt<IMapsDirectlyToDatabaseTable[]>(2);
                return null;
            });

        new ExecuteCommandCreateNewFilter(activator, host) { OfferCatalogueFilters = true }.Execute();

        return offered ?? throw new AssertionException("No filters were offered for import");
    }

    [Test]
    public void TestNewFilterForAggregate()
    {
        var ac = WhenIHaveA<AggregateConfiguration>();

        // has no container to start with (no filters)
        Assert.That(ac.RootFilterContainer_ID, Is.Null);
        Run("CreateNewFilter", $"{nameof(AggregateConfiguration)}:{ac.ID}");

        Assert.Multiple(() =>
        {
            Assert.That(ac.RootFilterContainer_ID, Is.Not.Null, "Should now have a container");
            Assert.That(ac.RootFilterContainer.GetFilters(), Has.Length.EqualTo(1), "Expected a single new filter");
        });
    }

    [Test]
    public void TestNewFilterForExtractionConfiguration()
    {
        var sds = WhenIHaveA<SelectedDataSets>();

        // has no container to start with (no filters)
        Assert.That(sds.RootFilterContainer_ID, Is.Null);
        Run("CreateNewFilter", $"{nameof(SelectedDataSets)}:{sds.ID}");

        Assert.Multiple(() =>
        {
            Assert.That(sds.RootFilterContainer_ID, Is.Not.Null, "Should now have a container");
            Assert.That(sds.RootFilterContainer.GetFilters(), Has.Length.EqualTo(1), "Expected a single new filter");
        });
    }

    [Test]
    public void TestNewFilterForCatalogue()
    {
        var ei = WhenIHaveA<ExtractionInformation>();

        // no Catalogue level filters
        Assert.That(ei.ExtractionFilters, Is.Empty);
        Run("CreateNewFilter", $"{nameof(ExtractionInformation)}:{ei.ID}", "My cool filter", "hb='t'");

        var f = ei.ExtractionFilters.Single();
        Assert.Multiple(() =>
        {
            Assert.That(f.Name, Is.EqualTo("My cool filter"));
            Assert.That(f.WhereSQL, Is.EqualTo("hb='t'"));
        });
    }
}