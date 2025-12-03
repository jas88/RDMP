// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.MapsDirectlyToDatabaseTable.Attributes;
using Rdmp.Core.Repositories;
using Tests.Common;
using SystemTextJsonExtensions = Rdmp.Core.Curation.Data.Serialization.SystemTextJson.JsonSerializerExtensions;

namespace Rdmp.Core.Tests.Curation.JsonSerializationTests;

/// <summary>
/// Comprehensive tests for DictionaryAsArrayConverter focusing on complex key scenarios
/// </summary>
[TestFixture]
public class DictionaryAsArrayConverterTests : DatabaseTests
{
    #region Basic Functionality

    [Test]
    public void SerializeDeserialize_StringIntDictionary_RoundTrips()
    {
        // Arrange
        var original = new Dictionary<string, int>
        {
            { "alpha", 1 },
            { "beta", 2 },
            { "gamma", 3 }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, int>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(3));
            Assert.That(deserialized["alpha"], Is.EqualTo(1));
            Assert.That(deserialized["beta"], Is.EqualTo(2));
            Assert.That(deserialized["gamma"], Is.EqualTo(3));
        });
    }

    [Test]
    public void SerializeDeserialize_IntStringDictionary_RoundTrips()
    {
        // Arrange
        var original = new Dictionary<int, string>
        {
            { 1, "one" },
            { 2, "two" },
            { 100, "hundred" }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<int, string>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(3));
            Assert.That(deserialized[1], Is.EqualTo("one"));
            Assert.That(deserialized[2], Is.EqualTo("two"));
            Assert.That(deserialized[100], Is.EqualTo("hundred"));
        });
    }

    #endregion

    #region Complex Key Types

    [Test]
    public void SerializeDeserialize_RelationshipAttributeKeys_RoundTrips()
    {
        // Arrange
        var attr1 = new RelationshipAttribute(typeof(string), RelationshipType.SharedObject, "prop1");
        var attr2 = new RelationshipAttribute(typeof(int), RelationshipType.SharedObject, "prop2");

        var original = new Dictionary<RelationshipAttribute, Guid>
        {
            { attr1, Guid.Parse("11111111-1111-1111-1111-111111111111") },
            { attr2, Guid.Parse("22222222-2222-2222-2222-222222222222") }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<RelationshipAttribute, Guid>>(json, RepositoryLocator);

        // Assert
        Assert.That(deserialized, Has.Count.EqualTo(2));
    }

    [Test]
    public void SerializeDeserialize_DateTimeKeys_RoundTrips()
    {
        // Arrange
        var date1 = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var original = new Dictionary<DateTime, string>
        {
            { date1, "New Year" },
            { date2, "End of Year" }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<DateTime, string>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(2));
            Assert.That(deserialized[date1], Is.EqualTo("New Year"));
            Assert.That(deserialized[date2], Is.EqualTo("End of Year"));
        });
    }

    [Test]
    public void SerializeDeserialize_GuidKeys_RoundTrips()
    {
        // Arrange
        var guid1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var guid2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var original = new Dictionary<Guid, string>
        {
            { guid1, "Value A" },
            { guid2, "Value B" }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<Guid, string>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(2));
            Assert.That(deserialized[guid1], Is.EqualTo("Value A"));
            Assert.That(deserialized[guid2], Is.EqualTo("Value B"));
        });
    }

    #endregion

    #region Nested Dictionary Tests

    [Test]
    public void SerializeDeserialize_NestedDictionaries_RoundTrips()
    {
        // Arrange
        var original = new Dictionary<string, Dictionary<string, int>>
        {
            { "group1", new Dictionary<string, int> { { "a", 1 }, { "b", 2 } } },
            { "group2", new Dictionary<string, int> { { "c", 3 }, { "d", 4 } } }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(2));
            Assert.That(deserialized["group1"]["a"], Is.EqualTo(1));
            Assert.That(deserialized["group1"]["b"], Is.EqualTo(2));
            Assert.That(deserialized["group2"]["c"], Is.EqualTo(3));
            Assert.That(deserialized["group2"]["d"], Is.EqualTo(4));
        });
    }

    #endregion

    #region Edge Cases

    [Test]
    public void SerializeDeserialize_EmptyDictionary_ReturnsEmptyDictionary()
    {
        // Arrange
        var original = new Dictionary<string, string>();

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, string>>(json, RepositoryLocator);

        // Assert
        Assert.That(deserialized, Is.Empty);
    }

    [Test]
    public void SerializeDeserialize_SingleEntry_RoundTrips()
    {
        // Arrange
        var original = new Dictionary<string, string>
        {
            { "onlyKey", "onlyValue" }
        };

        // Act
        var json = SystemTextJsonExtensions.SerializeObject(original, RepositoryLocator);
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, string>>(json, RepositoryLocator);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(1));
            Assert.That(deserialized["onlyKey"], Is.EqualTo("onlyValue"));
        });
    }

    [Test]
    public void SerializeDeserialize_DuplicateHandling_LastValueWins()
    {
        // Arrange - Manually create JSON with duplicate keys
        var json = "[[\"key\",\"value1\"],[\"key\",\"value2\"]]";

        // Act
        var deserialized = SystemTextJsonExtensions.DeserializeObject<Dictionary<string, string>>(json, RepositoryLocator);

        // Assert - Dictionary should contain only one entry (last value wins)
        Assert.Multiple(() =>
        {
            Assert.That(deserialized, Has.Count.EqualTo(1));
            Assert.That(deserialized["key"], Is.EqualTo("value2"));
        });
    }

    #endregion

    #region Helper Classes

    public class NestedLevel
    {
        public int Level { get; set; }
        public NestedLevel Child { get; set; }
        public Rdmp.Core.Curation.Data.Catalogue Catalogue { get; set; }

        private readonly IRDMPPlatformRepositoryServiceLocator _locator;

        public NestedLevel(IRDMPPlatformRepositoryServiceLocator locator)
        {
            _locator = locator;
        }
    }

    #endregion
}
