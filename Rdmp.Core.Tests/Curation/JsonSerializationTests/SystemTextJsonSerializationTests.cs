// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Serialization;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Attributes;
using Rdmp.Core.Repositories;
using Tests.Common;
using NewtonsoftExtensions = Rdmp.Core.Curation.Data.Serialization.JsonConvertExtensions;
using SystemTextJsonExtensions = Rdmp.Core.Curation.Data.Serialization.SystemTextJson.JsonSerializerExtensions;

namespace Rdmp.Core.Tests.Curation.JsonSerializationTests;

/// <summary>
/// Comprehensive unit tests for System.Text.Json serialization converters
/// </summary>
[TestFixture]
public class SystemTextJsonSerializationTests : DatabaseTests
{
    #region DatabaseEntityJsonConverter Tests

    [Test]
    public void DatabaseEntityJsonConverter_SerializeCatalogue_CreatesValidJson()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "TestCatalogue");

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(catalogue, RepositoryLocator, writeIndented: true);

        // Assert
        Assert.That(json, Does.Contain("PersistenceString"));
        Assert.That(json, Does.Contain("Catalogue"));
        Assert.That(json, Does.Contain(catalogue.ID.ToString()));

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    [Test]
    public void DatabaseEntityJsonConverter_DeserializeCatalogue_ReturnsOriginalObject()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "TestCatalogue");
        var json = SystemTextJsonExtensions.SerializeObject(catalogue, RepositoryLocator);

        // Act
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Catalogue>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.ID, Is.EqualTo(catalogue.ID));
            Assert.That(deserialized.Name, Is.EqualTo(catalogue.Name));
        });

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    [Test]
    public void DatabaseEntityJsonConverter_SerializeNull_ReturnsNull()
    {
        // Act
        var json = SystemTextJsonExtensions.SerializeObject(new { Catalogue = (Catalogue)null }, RepositoryLocator);

        // Assert
        Assert.That(json, Does.Contain("null"));
    }

    [Test]
    public void DatabaseEntityJsonConverter_DeserializeNull_ReturnsNull()
    {
        // Arrange
        var json = "{\"Catalogue\":null}";

        // Act
        var result = SystemTextJsonExtensions.DeserializeObject<TestClassWithCatalogue>(json, RepositoryLocator);

        // Assert
        Assert.That(result.Catalogue, Is.Null);
    }

    [Test]
    public void DatabaseEntityJsonConverter_RoundTrip_MaintainsObjectIdentity()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "TestCatalogue") { Description = "Test Description" };
        var wrapper = new TestClassWithCatalogue { Catalogue = catalogue, Title = "MyTitle" };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(wrapper, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<TestClassWithCatalogue>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Catalogue, Is.Not.Null);
            Assert.That(deserialized.Catalogue.ID, Is.EqualTo(catalogue.ID));
            Assert.That(deserialized.Title, Is.EqualTo("MyTitle"));
        });

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    #endregion

    #region PickAnyConstructorJsonConverter Tests

    [Test]
    public void PickAnyConstructorJsonConverter_DeserializeObjectWithNonDefaultConstructor_Success()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "TestCatalogue");
        var obj = new TestClassWithConstructor(RepositoryLocator) { Title = "Test", SelectedCatalogue = catalogue };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(obj, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<TestClassWithConstructor>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Title, Is.EqualTo("Test"));
            Assert.That(deserialized.SelectedCatalogue, Is.Not.Null);
            Assert.That(deserialized.SelectedCatalogue.ID, Is.EqualTo(catalogue.ID));
        });

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    [Test]
    public void PickAnyConstructorJsonConverter_WithCallback_CallsAfterConstruction()
    {
        // Arrange
        var obj = new TestClassWithCallback(RepositoryLocator) { Value = "Original" };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(obj, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<TestClassWithCallback>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.CallbackCalled, Is.True);
            Assert.That(deserialized.Value, Is.EqualTo("Original"));
        });
    }

    #endregion

    #region DictionaryAsArrayConverter Tests

    [Test]
    public void DictionaryAsArrayConverter_SerializeEmptyDictionary_ReturnsEmptyArray()
    {
        // Arrange
        var dict = new Dictionary<string, string>();

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(dict, RepositoryLocator);

        // Assert
        Assert.That(json, Is.EqualTo("[]"));
    }

    [Test]
    public void DictionaryAsArrayConverter_SerializeDictionary_CreatesArrayOfPairs()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "one", 1 },
            { "two", 2 }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(dict, RepositoryLocator);

        // Assert
        Assert.That(json, Does.Contain("one"));
        Assert.That(json, Does.Contain("two"));
        Assert.That(json, Does.Contain("1"));
        Assert.That(json, Does.Contain("2"));
    }

    [Test]
    public void DictionaryAsArrayConverter_RoundTrip_MaintainsDictionaryContent()
    {
        // Arrange
        var original = new Dictionary<string, int>
        {
            { "one", 1 },
            { "two", 2 },
            { "three", 3 }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, int>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized, Has.Count.EqualTo(3));
            Assert.That(deserialized["one"], Is.EqualTo(1));
            Assert.That(deserialized["two"], Is.EqualTo(2));
            Assert.That(deserialized["three"], Is.EqualTo(3));
        });
    }

    [Test]
    public void DictionaryAsArrayConverter_ComplexKeys_Success()
    {
        // Arrange
        var dict = new Dictionary<RelationshipAttribute, Guid>
        {
            { new RelationshipAttribute(typeof(string), RelationshipType.SharedObject, "property1"), Guid.NewGuid() },
            { new RelationshipAttribute(typeof(int), RelationshipType.SharedObject, "property2"), Guid.NewGuid() }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(dict, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<RelationshipAttribute, Guid>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void DictionaryAsArrayConverter_NullDictionary_ReturnsNull()
    {
        // Arrange
        Dictionary<string, string> dict = null;

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(dict, RepositoryLocator);

        // Assert
        Assert.That(json, Is.EqualTo("null"));
    }

    #endregion

    #region Integration Tests

    [Test]
    public void Integration_ComplexObjectGraph_RoundTrip()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "TestCatalogue");
        var complex = new ComplexTestClass(RepositoryLocator)
        {
            Title = "Complex",
            Catalogue = catalogue,
            Tags = new Dictionary<string, string>
            {
                { "version", "1.0" },
                { "author", "test" }
            }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(complex, RepositoryLocator, writeIndented: true);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<ComplexTestClass>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Title, Is.EqualTo("Complex"));
            Assert.That(deserialized.Catalogue, Is.Not.Null);
            Assert.That(deserialized.Catalogue.ID, Is.EqualTo(catalogue.ID));
            Assert.That(deserialized.Tags, Has.Count.EqualTo(2));
            Assert.That(deserialized.Tags["version"], Is.EqualTo("1.0"));
        });

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    [Test]
    public void Integration_NestedDatabaseEntities_RoundTrip()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue1 = new Catalogue(RepositoryLocator.CatalogueRepository, "Cat1");
        var catalogue2 = new Catalogue(RepositoryLocator.CatalogueRepository, "Cat2");

        var nested = new NestedTestClass(RepositoryLocator)
        {
            PrimaryCatalogue = catalogue1,
            SecondaryCatalogue = catalogue2
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(nested, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<NestedTestClass>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized.PrimaryCatalogue.ID, Is.EqualTo(catalogue1.ID));
            Assert.That(deserialized.SecondaryCatalogue.ID, Is.EqualTo(catalogue2.ID));
        });

        // Cleanup
        catalogue1.DeleteInDatabase();
        catalogue2.DeleteInDatabase();
    }

    #endregion

    #region Backward Compatibility Tests

    [Test]
    public void BackwardCompatibility_NewtonsoftJson_CanReadSystemTextJson()
    {
        // This test verifies that JSON generated by System.Text.Json
        // can be read by Newtonsoft.Json (for backward compatibility)

        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        // Arrange
        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "TestCatalogue");

        // Act - Serialize with System.Text.Json
        var jsonNew = SystemTextJsonExtensions.SerializeObject(catalogue, RepositoryLocator);

        // Act - Deserialize with Newtonsoft.Json
        var deserializedOld = NewtonsoftExtensions.DeserializeObject(
            jsonNew, typeof(Catalogue), RepositoryLocator) as Catalogue;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserializedOld, Is.Not.Null);
            Assert.That(deserializedOld.ID, Is.EqualTo(catalogue.ID));
        });

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    [Test]
    public void BackwardCompatibility_SystemTextJson_CanReadNewtonsoftJson()
    {
        // This test verifies that JSON generated by Newtonsoft.Json
        // can be read by System.Text.Json (for backward compatibility)

        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        // Arrange
        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "TestCatalogue");

        // Act - Serialize with Newtonsoft.Json
        var jsonOld = NewtonsoftExtensions.SerializeObject(catalogue, RepositoryLocator);

        // Act - Deserialize with System.Text.Json
        var deserializedNew = SystemTextJsonExtensions.DeserializeObject<Catalogue>(jsonOld, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserializedNew, Is.Not.Null);
            Assert.That(deserializedNew.ID, Is.EqualTo(catalogue.ID));
        });

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    #endregion

    #region Test Helper Classes

    public class TestClassWithCatalogue
    {
        public Catalogue Catalogue { get; set; }
        public string Title { get; set; }
    }

    public class TestClassWithConstructor
    {
        public string Title { get; set; }
        public Catalogue SelectedCatalogue { get; set; }

        private readonly IRDMPPlatformRepositoryServiceLocator _locator;

        public TestClassWithConstructor(IRDMPPlatformRepositoryServiceLocator locator)
        {
            _locator = locator;
        }
    }

    public class TestClassWithCallback : IPickAnyConstructorFinishedCallback
    {
        public string Value { get; set; }
        public bool CallbackCalled { get; private set; }

        private readonly IRDMPPlatformRepositoryServiceLocator _locator;

        public TestClassWithCallback(IRDMPPlatformRepositoryServiceLocator locator)
        {
            _locator = locator;
        }

        public void AfterConstruction()
        {
            CallbackCalled = true;
        }
    }

    public class ComplexTestClass
    {
        public string Title { get; set; }
        public Catalogue Catalogue { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        private readonly IRDMPPlatformRepositoryServiceLocator _locator;

        public ComplexTestClass(IRDMPPlatformRepositoryServiceLocator locator)
        {
            _locator = locator;
        }
    }

    public class NestedTestClass
    {
        public Catalogue PrimaryCatalogue { get; set; }
        public Catalogue SecondaryCatalogue { get; set; }

        private readonly IRDMPPlatformRepositoryServiceLocator _locator;

        public NestedTestClass(IRDMPPlatformRepositoryServiceLocator locator)
        {
            _locator = locator;
        }
    }

    #endregion
}
