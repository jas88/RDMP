// Copyright (c) The University of Dundee 2018-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Linq;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.Curation.Data.ImportExport;
using Rdmp.Core.Curation.Data.Serialization;
using Rdmp.Core.Sharing.Dependency.Gathering;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation.ImportTests;

/// <summary>
/// The part of <see cref="GatherAndShareTests"/> that needs a database: it relies on the default logging server that
/// <see cref="DatabaseTests"/> configures and on the cascade deletes performed by the Catalogue database.
/// </summary>
public class GatherAndShareDatabaseTests : DatabaseTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void GatherAndShare_Catalogue_Test(bool goViaJson)
    {
        //Setup some objects under Catalogue that we can share
        var cata = new Catalogue(CatalogueRepository, "Cata")
        {
            Periodicity = Catalogue.CataloguePeriodicity.BiMonthly
        };
        cata.SaveToDatabase();

        var catalogueItem1 = new CatalogueItem(CatalogueRepository, cata, "Ci1");
        var catalogueItem2 = new CatalogueItem(CatalogueRepository, cata, "Ci2");

        var tableInfo = new TableInfo(CatalogueRepository, "Myt");
        var colInfo = new ColumnInfo(CatalogueRepository, "[Mt].[C1]", "varchar(10)", tableInfo);

        catalogueItem1.ColumnInfo_ID = colInfo.ID;
        catalogueItem1.SaveToDatabase();

        var ei = new ExtractionInformation(CatalogueRepository, catalogueItem1, colInfo, "UPPER(C1) as Fish");

        //the logging server has a system default so should have been populated
        Assert.That(cata.LiveLoggingServer_ID, Is.Not.Null);

        //Catalogue sharing should be allowed
        var g = new Gatherer(RepositoryLocator);
        Assert.That(g.CanGatherDependencies(cata));

        //gather the objects depending on Catalogue as a tree
        var gObj = Gatherer.GatherDependencies(cata);
        Assert.That(gObj.Children, Has.Count.EqualTo(2)); //both cata items

        var lmd = new LoadMetadata(CatalogueRepository);
        cata.SaveToDatabase();
        var linkage = new LoadMetadataCatalogueLinkage(CatalogueRepository, lmd, cata);
        linkage.SaveToDatabase();
        //get the share definition
        var shareManager = new ShareManager(RepositoryLocator);
        var shareDefinition = gObj.ToShareDefinitionWithChildren(shareManager);


        if (goViaJson)
        {
            var json =
                shareDefinition.Select(s => JsonConvertExtensions.SerializeObject(s, RepositoryLocator)).ToList();
            shareDefinition =
                json.Select(
                        j => JsonConvertExtensions.DeserializeObject(j, typeof(ShareDefinition), RepositoryLocator))
                    .Cast<ShareDefinition>()
                    .ToList();
        }

        //make a local change
        cata.Name = "fishfish";
        cata.SubjectNumbers = "123";
        cata.Periodicity = Catalogue.CataloguePeriodicity.Unknown;
        cata.SaveToDatabase();
        lmd.UnlinkFromCatalogue(cata);
        lmd.DeleteInDatabase();

        //import the saved copy
        shareManager.ImportSharedObject(shareDefinition);

        //revert the memory copy and check it got overwritten with the original saved values
        cata = CatalogueRepository.GetObjectByID<Catalogue>(cata.ID);
        Assert.That(cata.Name, Is.EqualTo("Cata"));

        var exports = CatalogueRepository.GetAllObjects<ObjectExport>();
        Assert.That(exports.Any());

        //now delete and report
        foreach (var d in exports)
            d.DeleteInDatabase();

        //make a local change including Name
        cata.Name = "fishfish";
        cata.SaveToDatabase();

        //test importing the Catalogue properties only
        ShareManager.ImportPropertiesOnly(cata, shareDefinition[0]);

        Assert.Multiple(() =>
        {
            //import the defined properties but not name
            Assert.That(cata.Name, Is.EqualTo("fishfish"));
            Assert.That(cata.Periodicity, Is.EqualTo(Catalogue.CataloguePeriodicity.BiMonthly)); //reset this though
            Assert.That(cata.LoadMetadatas(), Is.Empty);
        });
        cata.SaveToDatabase();

        cata.DeleteInDatabase();

        Assert.Multiple(() =>
        {
            //none of these should now exist thanks to cascade deletes
            Assert.That(cata.Exists(), Is.False);
            Assert.That(catalogueItem1.Exists(), Is.False);
            Assert.That(catalogueItem2.Exists(), Is.False);
        });

        //import the saved copy
        var newObjects = shareManager.ImportSharedObject(shareDefinition).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(((Catalogue)newObjects[0]).Name, Is.EqualTo("Cata"));
            Assert.That(((CatalogueItem)newObjects[1]).Name, Is.EqualTo("Ci1"));
            Assert.That(((CatalogueItem)newObjects[2]).Name, Is.EqualTo("Ci2"));
        });
    }
}
