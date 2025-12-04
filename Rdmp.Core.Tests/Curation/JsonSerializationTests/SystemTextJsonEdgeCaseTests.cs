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
using SystemTextJsonExtensions = Rdmp.Core.Curation.Data.Serialization.SystemTextJson.JsonSerializerExtensions;

namespace Rdmp.Core.Tests.Curation.JsonSerializationTests;

/// <summary>
/// Edge case and error handling tests for System.Text.Json serialization
/// </summary>
[TestFixture]
public class SystemTextJsonEdgeCaseTests : DatabaseTests
{
    #region Error Handling Tests

    [Test]
    public void DatabaseEntityJsonConverter_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{\"PersistenceString\": }"; // Malformed JSON

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            SystemTextJsonExtensions.DeserializeObject<Catalogue>(invalidJson, RepositoryLocator));
    }

    [Test]
    public void DatabaseEntityJsonConverter_MissingPersistenceString_ThrowsJsonException()
    {
        // Arrange
        var json = "{\"WrongProperty\":\"value\"}";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            SystemTextJsonExtensions.DeserializeObject<Catalogue>(json, RepositoryLocator));
    }

    [Test]
    public void DatabaseEntityJsonConverter_InvalidPersistenceString_ThrowsException()
    {
        // Arrange
        var json = "{\"PersistenceString\":\"InvalidFormat\"}";

        // Act & Assert
        Assert.Throws<Exception>(() =>
            SystemTextJsonExtensions.DeserializeObject<Catalogue>(json, RepositoryLocator));
    }

    [Test]
    public void DictionaryAsArrayConverter_InvalidArrayFormat_ThrowsJsonException()
    {
        // Arrange - Single value instead of pair
        var invalidJson = "[[\"key\"]]";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            SystemTextJsonExtensions.DeserializeObject<Dictionary<string, string>>(invalidJson, RepositoryLocator));
    }

    [Test]
    public void DictionaryAsArrayConverter_NotAnArray_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{\"key\":\"value\"}";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            SystemTextJsonExtensions.DeserializeObject<Dictionary<string, string>>(invalidJson, RepositoryLocator));
    }

    [Test]
    public void PickAnyConstructorJsonConverter_NoCompatibleConstructor_ThrowsException()
    {
        // Arrange
        var json = "{\"Value\":\"test\"}";

        // Act & Assert
        // PickAnyConstructorJsonConverter throws JsonException when no compatible constructor is found
        var ex = Assert.Throws<JsonException>(() =>
            SystemTextJsonExtensions.DeserializeObject<ClassWithNoCompatibleConstructor>(json, RepositoryLocator));

        Assert.That(ex.Message, Does.Contain("constructor"));
    }

    #endregion

    #region Special Values Tests

    [Test]
    public void DatabaseEntityJsonConverter_EmptyString_HandledGracefully()
    {
        // Act & Assert
        Assert.Throws<JsonException>(() =>
            SystemTextJsonExtensions.DeserializeObject<Catalogue>("", RepositoryLocator));
    }

    [Test]
    public void DictionaryAsArrayConverter_NullValue_SerializesCorrectly()
    {
        // Arrange
        var dict = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", null }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(dict, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, string>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(2));
            Assert.That(deserialized["key1"], Is.EqualTo("value1"));
            Assert.That(deserialized["key2"], Is.Null);
        });
    }

    [Test]
    public void DictionaryAsArrayConverter_SpecialCharactersInKeys_HandledCorrectly()
    {
        // Arrange
        var dict = new Dictionary<string, string>
        {
            { "key with spaces", "value1" },
            { "key\"with\"quotes", "value2" },
            { "key\nwith\nnewlines", "value3" }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(dict, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, string>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(3));
            Assert.That(deserialized["key with spaces"], Is.EqualTo("value1"));
            Assert.That(deserialized["key\"with\"quotes"], Is.EqualTo("value2"));
            Assert.That(deserialized["key\nwith\nnewlines"], Is.EqualTo("value3"));
        });
    }

    #endregion

    #region Large Data Tests

    [Test]
    public void DictionaryAsArrayConverter_LargeDictionary_HandlesCorrectly()
    {
        // Arrange
        var largeDict = new Dictionary<string, int>();
        for (int i = 0; i < 1000; i++)
        {
            largeDict[$"key{i}"] = i;
        }

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(largeDict, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, int>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(1000));
            Assert.That(deserialized["key0"], Is.EqualTo(0));
            Assert.That(deserialized["key999"], Is.EqualTo(999));
        });
    }

    [Test]
    public void Integration_DeeplyNestedObject_HandlesCorrectly()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "NestedTest");
        var level1 = new NestedLevel(RepositoryLocator) { Catalogue = catalogue, Level = 1 };
        var level2 = new NestedLevel(RepositoryLocator) { Catalogue = catalogue, Level = 2, Child = level1 };
        var level3 = new NestedLevel(RepositoryLocator) { Catalogue = catalogue, Level = 3, Child = level2 };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(level3, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<NestedLevel>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized.Level, Is.EqualTo(3));
            Assert.That(deserialized.Child, Is.Not.Null);
            Assert.That(deserialized.Child.Level, Is.EqualTo(2));
            Assert.That(deserialized.Child.Child, Is.Not.Null);
            Assert.That(deserialized.Child.Child.Level, Is.EqualTo(1));
            Assert.That(deserialized.Catalogue.ID, Is.EqualTo(catalogue.ID));
        });

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    #endregion

    #region Type Safety Tests

    [Test]
    public void DictionaryAsArrayConverter_TypeMismatch_ThrowsException()
    {
        // Arrange - Try to deserialize int array as string dictionary
        var json = "[[\"key\",123]]";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            SystemTextJsonExtensions.DeserializeObject<Dictionary<string, string>>(json, RepositoryLocator));
    }

    [Test]
    public void DatabaseEntityJsonConverter_WrongType_ThrowsException()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "Test");
        var json = SystemTextJsonExtensions.SerializeObject(catalogue, RepositoryLocator);

        // Act & Assert
        // Trying to deserialize a Catalogue as a different type should fail
        // System.Text.Json throws InvalidCastException when it can't cast to the expected type
        Assert.Throws<InvalidCastException>(() =>
            SystemTextJsonExtensions.DeserializeObject<CatalogueItem>(json, RepositoryLocator));

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    #endregion

    #region Unicode and Special Character Tests

    [Test]
    public void Integration_UnicodeCharacters_HandledCorrectly()
    {
        // Arrange
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "Test")
        {
            Description = "日本語 Français Español 中文 العربية 🎉"
        };

        var wrapper = new TestWrapper(RepositoryLocator)
        {
            Catalogue = catalogue,
            Title = "Unicode Test: Ǽ ǿ Ǿ"
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(wrapper, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<TestWrapper>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized.Title, Is.EqualTo("Unicode Test: Ǽ ǿ Ǿ"));
            Assert.That(deserialized.Catalogue.Description, Is.EqualTo("日本語 Français Español 中文 العربية 🎉"));
        });

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    #endregion

    #region Test Helper Classes

    public class ClassWithNoCompatibleConstructor
    {
        public string Value { get; set; }

        // Constructor that requires a parameter not available in RepositoryLocator
        public ClassWithNoCompatibleConstructor(string someSpecificParameter)
        {
            Value = someSpecificParameter;
        }
    }

    public class NestedLevel
    {
        public int Level { get; set; }
        public Catalogue Catalogue { get; set; }
        public NestedLevel Child { get; set; }

        private readonly IRDMPPlatformRepositoryServiceLocator _locator;

        public NestedLevel(IRDMPPlatformRepositoryServiceLocator locator)
        {
            _locator = locator;
        }
    }

    public class TestWrapper
    {
        public string Title { get; set; }
        public Catalogue Catalogue { get; set; }

        private readonly IRDMPPlatformRepositoryServiceLocator _locator;

        public TestWrapper(IRDMPPlatformRepositoryServiceLocator locator)
        {
            _locator = locator;
        }
    }

    #endregion
}
