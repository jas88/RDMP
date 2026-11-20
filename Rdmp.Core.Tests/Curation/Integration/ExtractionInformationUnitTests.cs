// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation.Integration;

public class ExtractionInformationUnitTests : UnitTests
{
    [TestCase(null)]
    [TestCase(4)]
    [TestCase(-3)]
    [TestCase(99)]
    public void NewExtractionInformation_OrderShouldBeContiguous(int? explicitOrder)
    {
        //When we have an ExtractionInformation
        var ei = WhenIHaveA<ExtractionInformation>();

        if (explicitOrder.HasValue)
        {
            ei.Order = explicitOrder.Value;
            ei.SaveToDatabase();
        }

        Assert.That(ei.Order, Is.EqualTo(explicitOrder ?? 1));

        // Newly created ones should have the right Order to not collide
        var cata = ei.CatalogueItem.Catalogue;
        var cataItem = new CatalogueItem(RepositoryLocator.CatalogueRepository, cata, "ci");
        var ei2 = new ExtractionInformation(RepositoryLocator.CatalogueRepository, cataItem, ei.ColumnInfo, "fff");

        Assert.That(ei2.Order, Is.EqualTo((explicitOrder ?? 1) + 1));
    }

    [Test]
    public void NewExtractionInformation_OrderShouldBeContiguous_ManyCalls()
    {
        //When we have an ExtractionInformation
        var ei1 = WhenIHaveA<ExtractionInformation>();

        // Newly created ones should have the right Order to not collide
        var cata = ei1.CatalogueItem.Catalogue;
        var cataItem2 = new CatalogueItem(RepositoryLocator.CatalogueRepository, cata, "ci");
        var ei2 = new ExtractionInformation(RepositoryLocator.CatalogueRepository, cataItem2, ei1.ColumnInfo, "fff");

        Assert.That(ei2.Order, Is.EqualTo(2));

        var cataItem3 = new CatalogueItem(RepositoryLocator.CatalogueRepository, cata, "ci");
        var ei3 = new ExtractionInformation(RepositoryLocator.CatalogueRepository, cataItem3, ei1.ColumnInfo, "fff");

        Assert.That(ei3.Order, Is.EqualTo(3));

        var cataItem4 = new CatalogueItem(RepositoryLocator.CatalogueRepository, cata, "ci");
        var ei4 = new ExtractionInformation(RepositoryLocator.CatalogueRepository, cataItem4, ei1.ColumnInfo, "fff");

        Assert.That(ei4.Order, Is.EqualTo(4));
    }

    [Test]
    public void CopiesOfTheSameExtractionInformation_AreEqualAndHashAlike()
    {
        // Typed as DatabaseEntity, which provides ExtractionInformation's Equals and GetHashCode.  Called on an
        // ExtractionInformation, Equals would make code scanning (cs/class-missing-equals) report the class for not
        // declaring Equals itself.
        DatabaseEntity ei = WhenIHaveA<ExtractionInformation>();

        // a second object for the same record, built the way YamlRepository loads one
        DatabaseEntity copy = new ExtractionInformation { ID = ei.ID, Repository = ei.Repository };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(copy, Is.Not.SameAs(ei));
            Assert.That(copy.Equals(ei), Is.True);
            Assert.That(ei.Equals(copy), Is.True);
            Assert.That(copy.GetHashCode(), Is.EqualTo(ei.GetHashCode()));
            Assert.That(new HashSet<object> { ei }.Contains(copy), Is.True);
        }
    }

    [Test]
    public void ExtractionInformation_HashesLikeEveryOtherDatabaseEntity()
    {
        // Equals and GetHashCode both come from DatabaseEntity, so the two cannot disagree
        var ei = WhenIHaveA<ExtractionInformation>();

        Assert.That(ei.GetHashCode(), Is.EqualTo(ei.Repository.GetHashCode(ei)));
    }

    [Test]
    public void ExtractionInformation_IsNotEqualToOtherTypesWithTheSameID()
    {
        // tree and search dictionaries hold an ExtractionInformation beside the CatalogueItem and ColumnInfo it is
        // built from, whose IDs are often the same.  All three are typed as DatabaseEntity, which provides their
        // Equals: typed as themselves, code scanning would report the comparisons as being between unrelated types.
        DatabaseEntity ei = WhenIHaveA<ExtractionInformation>();
        DatabaseEntity catalogueItem = new CatalogueItem { ID = ei.ID, Repository = ei.Repository };
        DatabaseEntity columnInfo = new ColumnInfo { ID = ei.ID, Repository = ei.Repository };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ei.Equals(catalogueItem), Is.False);
            Assert.That(ei.Equals(columnInfo), Is.False);
            Assert.That(catalogueItem.Equals(ei), Is.False);
            Assert.That(columnInfo.Equals(ei), Is.False);
            Assert.That(new HashSet<object> { catalogueItem, columnInfo }.Contains(ei), Is.False);
        }
    }
}