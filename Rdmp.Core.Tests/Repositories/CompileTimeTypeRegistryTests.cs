// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rdmp.Core.Repositories;
using Rdmp.Core.Repositories.Construction;

namespace Rdmp.Core.Tests.Repositories;

/// <summary>
/// Comprehensive tests for MEF type lookup functionality and performance optimization validation
/// </summary>
internal class CompileTimeTypeRegistryTests
{
    private readonly string[] _testTypeNames = {
        "Rdmp.Core.Curation.Data.Catalogue",
        "Rdmp.Core.Repositories.MEF",
        "System.String",
        "NonExistent.Type"
    };

    [SetUp]
    public void Setup()
    {
        // Clear any cached state to ensure test isolation
        FlushCache();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up after each test
        FlushCache();
    }

    private void FlushCache()
    {
        // Access the internal flush mechanism through reflection to reset state
        var assemblyLoadHandler = typeof(MEF).GetField("_types",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (assemblyLoadHandler?.GetValue(null) is Lazy<System.Collections.ObjectModel.ReadOnlyDictionary<string, Type>> lazy &&
            lazy.IsValueCreated)
        {
            // Trigger a rebuild by simulating assembly load
            typeof(MEF).GetMethod("Flush",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { null, null });
        }
    }

    [Test]
    public void GetType_ExactMatch_ReturnsCorrectType()
    {
        // Arrange
        var expectedType = typeof(MEF);
        var typeName = "Rdmp.Core.Repositories.MEF";

        // Act
        var result = MEF.GetType(typeName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedType));
    }

    [Test]
    public void GetType_CaseInsensitiveMatch_ReturnsCorrectType()
    {
        // Arrange
        var expectedType = typeof(MEF);
        var typeName = "rdmp.core.repositories.mef";

        // Act
        var result = MEF.GetType(typeName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedType));
    }

    [Test]
    public void GetType_WithTailMatch_ReturnsCorrectType()
    {
        // Arrange
        var typeName = "MEF";

        // Act
        var result = MEF.GetType(typeName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("MEF"));
    }

    [Test]
    public void GetType_WithTailCaseInsensitiveMatch_ReturnsCorrectType()
    {
        // Arrange
        var typeName = "mef";

        // Act
        var result = MEF.GetType(typeName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("MEF"));
    }

    [Test]
    public void GetType_NonExistentType_ReturnsNull()
    {
        // Arrange
        var typeName = "NonExistent.Type.Name";

        // Act
        var result = MEF.GetType(typeName);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetType_ShortNameOfOpenXmlSdkType_IsNotResolved()
    {
        // typeof makes sure both OpenXml SDK assemblies are loaded, so have been offered to the lookup caches
        var openXmlTypes = new[]
        {
            typeof(DocumentFormat.OpenXml.Wordprocessing.Name),
            typeof(DocumentFormat.OpenXml.Wordprocessing.Body),
            typeof(DocumentFormat.OpenXml.Wordprocessing.Paragraph),
            typeof(DocumentFormat.OpenXml.Wordprocessing.Table),
            typeof(DocumentFormat.OpenXml.OpenXmlElement)
        };

        Assert.Multiple(() =>
        {
            foreach (var type in openXmlTypes)
            {
                Assert.That(MEF.GetType(type.Name)?.Assembly, Is.Not.EqualTo(type.Assembly),
                    $"'{type.Name}' was resolved to a DocumentFormat.OpenXml Type");
                Assert.That(MEF.GetType($"Not.A.Real.Namespace.{type.Name}")?.Assembly, Is.Not.EqualTo(type.Assembly),
                    $"A missing Type ending '{type.Name}' was resolved to a DocumentFormat.OpenXml Type");
            }

            // A Type whose short name nothing else has can still be found by its full name.  Finding it that way
            // must not make the short name resolvable afterwards.
            var withoutNamesake = openXmlTypes.Where(static t => MEF.GetType(t.Name) == null).ToArray();
            Assert.That(withoutNamesake, Does.Contain(typeof(DocumentFormat.OpenXml.Wordprocessing.Name)));

            foreach (var type in withoutNamesake)
            {
                Assert.That(MEF.GetType(type.FullName), Is.EqualTo(type));
                Assert.That(MEF.GetType(type.Name), Is.Null,
                    $"'{type.Name}' became resolvable by short name after a full name lookup");
            }

            Assert.That(
                MEF.GetAllTypes().Where(static t =>
                    t.Assembly.FullName?.StartsWith("DocumentFormat.OpenXml", StringComparison.Ordinal) == true),
                Is.Empty, "DocumentFormat.OpenXml Types should not be in the Type lookup caches");
        });
    }

    [Test]
    public void GetType_NullOrEmptyInput_ThrowsArgumentException()
    {
        // Act & Assert
        // ArgumentException.ThrowIfNullOrEmpty throws ArgumentNullException for null (which derives from ArgumentException)
        Assert.Throws<ArgumentNullException>(() => MEF.GetType(null));
        Assert.Throws<ArgumentException>(() => MEF.GetType(string.Empty));
        // Whitespace is not validated and returns null (type not found)
        Assert.That(MEF.GetType("   "), Is.Null);
    }

    [Test]
    public void GetType_WithExpectedBaseClass_ValidType_ReturnsCorrectType()
    {
        // Arrange
        var expectedBaseType = typeof(UseWithObjectConstructorAttribute);
        var typeName = "Rdmp.Core.Repositories.Construction.UseWithObjectConstructorAttribute";

        // Act
        var result = MEF.GetType(typeName, expectedBaseType);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(expectedBaseType.IsAssignableFrom(result), Is.True);
    }

    [Test]
    public void GetType_WithExpectedBaseClass_InvalidType_ThrowsException()
    {
        // Arrange
        var expectedBaseType = typeof(UseWithObjectConstructorAttribute);
        var typeName = "System.String"; // String doesn't inherit from UseWithObjectConstructorAttribute

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => MEF.GetType(typeName, expectedBaseType));
        Assert.That(ex.Message, Does.Contain("did not implement expected base class/interface"));
    }

    [Test]
    public void GetTypes_Generic_ReturnsDerivedTypes()
    {
        // Arrange
        var baseType = typeof(UseWithObjectConstructorAttribute);

        // Act
        var result = MEF.GetTypes<UseWithObjectConstructorAttribute>();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.All(t => baseType.IsAssignableFrom(t)), Is.True);
    }

    [Test]
    public void GetGenericTypes_ReturnsCompatibleGenericImplementations()
    {
        // Arrange
        var genericType = typeof(IEnumerable<>);
        var typeOfT = typeof(string);

        // Act
        var result = MEF.GetGenericTypes(genericType, typeOfT);

        // Assert
        Assert.That(result, Is.Not.Null);
        var expectedType = genericType.MakeGenericType(typeOfT);
        Assert.That(result.All(t => expectedType.IsAssignableFrom(t)), Is.True);
    }

    [Test]
    public void GetAllTypes_ReturnsAllLoadedTypes()
    {
        // Act
        var allTypes = MEF.GetAllTypes().ToList();

        // Assert
        Assert.That(allTypes, Is.Not.Empty);
        Assert.That(allTypes.Distinct().Count(), Is.EqualTo(allTypes.Count)); // No duplicates

        // Should contain some core RDMP types
        Assert.That(allTypes.Any(t => t.FullName?.Contains("Rdmp.Core") == true), Is.True);
    }

    [Test]
    public void ListBadAssemblies_ReturnsBadAssemblyInformation()
    {
        // Act
        var badAssemblies = MEF.ListBadAssemblies();

        // Assert
        Assert.That(badAssemblies, Is.Not.Null);
        // The result may be empty or contain entries depending on current state
    }

    [Test]
    public void GetCSharpNameForType_SimpleType_ReturnsTypeName()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var result = MEF.GetCSharpNameForType(type);

        // Assert
        Assert.That(result, Is.EqualTo("String"));
    }

    [Test]
    public void GetCSharpNameForType_GenericType_ReturnsFormattedName()
    {
        // Arrange
        var type = typeof(List<string>);

        // Act
        var result = MEF.GetCSharpNameForType(type);

        // Assert
        Assert.That(result, Is.EqualTo("List<String>"));
    }

    [Test]
    public void GetCSharpNameForType_MultiGenericType_ThrowsNotSupportedException()
    {
        // Arrange
        var type = typeof(Dictionary<string, int>);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => MEF.GetCSharpNameForType(type));
    }

    [Test]
    public void CreateA_ValidTypeWithParameters_CreatesInstance()
    {
        // Arrange
        var testType = typeof(TestConstructorClass);
        var parameter = "test parameter";

        // Act
        var result = MEF.CreateA<ITestInterface>(testType.FullName, parameter);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ITestInterface>());
        Assert.That(((TestConstructorClass)result).ParameterValue, Is.EqualTo(parameter));
    }

    [Test]
    public void CreateA_NonExistentType_ThrowsException()
    {
        // Arrange
        var nonExistentType = "NonExistent.Type";

        // Act & Assert
        Assert.Throws<Exception>(() => MEF.CreateA<ITestInterface>(nonExistentType));
    }

    [Test]
    public void CreateA_IncompatibleType_ThrowsException()
    {
        // Arrange
        var incompatibleType = typeof(string);

        // Act & Assert
        Assert.Throws<Exception>(() => MEF.CreateA<ITestInterface>(incompatibleType.FullName));
    }

    [Test]
    public void GetType_Performance_MultipleLookups_CompletesWithinTimeLimit()
    {
        // Arrange
        var typeName = typeof(MEF).FullName;
        var lookupCount = 1000;
        var sw = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < lookupCount; i++)
        {
            var result = MEF.GetType(typeName);
            Assert.That(result, Is.Not.Null);
        }
        sw.Stop();

        // Assert - Allow more time for CI environments which may be slower
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(250)); // Should complete quickly due to caching
        Console.WriteLine($"Type lookup performance: {lookupCount} lookups in {sw.ElapsedMilliseconds}ms");
    }

    [Test]
    public void GetType_ConcurrentLookups_ThreadSafe()
    {
        // Arrange
        var typeName = typeof(MEF).FullName;
        var tasks = new List<Task<Type>>();
        var taskCount = 10;
        var lookupsPerTask = 100;

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var results = new List<Type>();
                for (int j = 0; j < lookupsPerTask; j++)
                {
                    results.Add(MEF.GetType(typeName));
                }
                return results.FirstOrDefault();
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        foreach (var task in tasks)
        {
            var result = task.Result;
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(typeof(MEF)));
        }
    }

    [Test]
    public void BackwardCompatibility_ExistingMEFCalls_WorkCorrectly()
    {
        // Test that existing code patterns continue to work

        // Act & Assert - These should all work without throwing
        Assert.DoesNotThrow(() => MEF.GetType("MEF"));
        Assert.DoesNotThrow(() => MEF.GetTypes<UseWithObjectConstructorAttribute>());
        Assert.DoesNotThrow(() => MEF.GetAllTypes());
        Assert.DoesNotThrow(() => MEF.GetCSharpNameForType(typeof(List<string>)));
    }

    [Test]
    public void TryLoadCompiledRegistry_ListsTypes_ReturnsThemLookedUpCaseInsensitively()
    {
        var types = MEF.TryLoadCompiledRegistry(typeof(RegistryListingTypes));

        Assert.That(types, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(types, Has.Count.EqualTo(2));
            Assert.That(types["MEF"], Is.EqualTo(typeof(MEF)));
            Assert.That(types["mef"], Is.EqualTo(typeof(MEF)));
            Assert.That(types["RDMP.CORE.REPOSITORIES.CONSTRUCTION.OBJECTCONSTRUCTOR"], Is.EqualTo(typeof(ObjectConstructor)));
        });
    }

    [Test]
    public void TryLoadCompiledRegistry_GetAllTypesThrows_ReturnsNull() =>
        Assert.That(MEF.TryLoadCompiledRegistry(typeof(RegistryThatFails)), Is.Null);

    [Test]
    public void TryLoadCompiledRegistry_NoGetAllTypes_ReturnsNull() =>
        Assert.That(MEF.TryLoadCompiledRegistry(typeof(RegistryWithoutGetAllTypes)), Is.Null);

    [Test]
    public void TryLoadCompiledRegistry_GetAllTypesListsSomethingElse_ReturnsNull() =>
        Assert.That(MEF.TryLoadCompiledRegistry(typeof(RegistryListingNames)), Is.Null);

    #region Test Helper Classes

    public interface ITestInterface { }

    public class TestConstructorClass : ITestInterface
    {
        public string ParameterValue { get; }

        public TestConstructorClass(string parameter)
        {
            ParameterValue = parameter;
        }
    }

    // Stand ins for the generated CompiledTypeRegistry.  None of them is called Rdmp.Core.Repositories.CompiledTypeRegistry,
    // so MEF's name lookup never finds them.  The tests pass them directly to MEF.TryLoadCompiledRegistry.

    private static class RegistryListingTypes
    {
        public static IEnumerable<KeyValuePair<string, Type>> GetAllTypes() => new Dictionary<string, Type>
        {
            { "MEF", typeof(MEF) },
            { "Rdmp.Core.Repositories.Construction.ObjectConstructor", typeof(ObjectConstructor) }
        };
    }

    private static class RegistryThatFails
    {
        public static IEnumerable<KeyValuePair<string, Type>> GetAllTypes() =>
            throw new InvalidOperationException("The registry could not be initialised");
    }

    private static class RegistryWithoutGetAllTypes
    {
    }

    private static class RegistryListingNames
    {
        public static IEnumerable<string> GetAllTypes() => new[] { "MEF" };
    }

    #endregion
}