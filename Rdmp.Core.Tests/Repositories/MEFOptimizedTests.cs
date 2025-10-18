// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Repositories.Construction;
using Rdmp.Core.Startup;
using Tests.Common;

namespace Rdmp.Core.Tests.Repositories;

/// <summary>
/// Integration tests for MEF optimization system, testing real-world scenarios and fallback mechanisms
/// </summary>
internal class MEFOptimizedTests : DatabaseTests
{
    private ICatalogueRepository _catalogueRepository;
    private IRDMPPlatformRepositoryServiceLocator _repositoryLocator;

    [SetUp]
    public void IntegrationSetup()
    {
        _catalogueRepository = GetCatalogueRepository();
        _repositoryLocator = GetRepositoryLocator();
    }

    [Test]
    public void Integration_MEFWithCatalogueRepository_CreatesCatalogueCorrectly()
    {
        // Arrange
        var catalogueName = "Test Catalogue " + Guid.NewGuid();

        // Act
        var catalogue = new Catalogue(_catalogueRepository, catalogueName);

        // Assert
        Assert.That(catalogue, Is.Not.Null);
        Assert.That(catalogue.Name, Is.EqualTo(catalogueName));
        Assert.That(catalogue.ID, Is.GreaterThan(0));
    }

    [Test]
    public void Integration_MEFTypeDiscovery_FindsRDMPTypes()
    {
        // Arrange & Act
        var allTypes = MEF.GetAllTypes().ToList();
        var rdmpTypes = allTypes.Where(t => t.FullName?.StartsWith("Rdmp.Core") == true).ToList();

        // Assert
        Assert.That(rdmpTypes, Is.Not.Empty);
        Assert.That(rdmpTypes.Any(t => t == typeof(Catalogue)), Is.True);
        Assert.That(rdmpTypes.Any(t => t == typeof(MEF)), Is.True);
    }

    [Test]
    public void Integration_MEFPluginScenarios_HandlesPluginTypes()
    {
        // Arrange - Test with a type that represents a plugin-like scenario
        var pluginTypeNames = new[]
        {
            "Rdmp.Core.Curation.Data.Catalogue",
            "Rdmp.Core.Repositories.Construction.ObjectConstructor",
            "Rdmp.Core.Repositories.MEF"
        };

        // Act & Assert
        foreach (var typeName in pluginTypeNames)
        {
            var type = MEF.GetType(typeName);
            Assert.That(type, Is.Not.Null, $"Type {typeName} should be found");
            Assert.That(type.FullName, Is.EqualTo(typeName));
        }
    }

    [Test]
    public void Integration_MEFWithObjectConstructor_CreatesInstancesCorrectly()
    {
        // Arrange
        var testType = typeof(TestIntegrationClass);
        var parameter = "Integration Test Parameter";

        // Act
        var instance = MEF.CreateA<ITestIntegrationInterface>(testType.FullName, parameter);

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<TestIntegrationClass>());
        Assert.That(((TestIntegrationClass)instance).TestValue, Is.EqualTo(parameter));
    }

    [Test]
    public void Integration_MEFWithComplexConstructors_HandlesMultipleParameters()
    {
        // Arrange
        var testType = typeof(ComplexConstructorClass);
        var parameters = new object[] { "test", 42, true };

        // Act
        var instance = ObjectConstructor.ConstructIfPossible(testType, parameters);

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<ComplexConstructorClass>());
        var complexInstance = (ComplexConstructorClass)instance;
        Assert.That(complexInstance.StringValue, Is.EqualTo("test"));
        Assert.That(complexInstance.IntValue, Is.EqualTo(42));
        Assert.That(complexInstance.BoolValue, Is.EqualTo(true));
    }

    [Test]
    public void Integration_MEFWithRepository_CreatesRepositoryDependentObjects()
    {
        // Arrange
        var testType = typeof(RepositoryDependentClass);

        // Act
        var instance = ObjectConstructor.Construct(testType, _catalogueRepository);

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<RepositoryDependentClass>());
        var repoDependent = (RepositoryDependentClass)instance;
        Assert.That(repoDependent.Repository, Is.EqualTo(_catalogueRepository));
    }

    [Test]
    public void Integration_FallbackMechanism_UsesBlankConstructor()
    {
        // Arrange
        var testType = typeof(BlankConstructorClass);

        // Act
        var instance = ObjectConstructor.Construct(testType, _catalogueRepository, allowBlank: true);

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(instance, Is.InstanceOf<BlankConstructorClass>());
    }

    [Test]
    public void Integration_FallbackMechanism_DisallowsBlankConstructor_ThrowsException()
    {
        // Arrange
        var testType = typeof(BlankConstructorClass);

        // Act & Assert
        Assert.Throws<ObjectLacksCompatibleConstructorException>(
            () => ObjectConstructor.Construct(testType, _catalogueRepository, allowBlank: false));
    }

    [Test]
    public void Integration_ConcurrentMEFUsage_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task<object>>();
        var taskCount = 10;
        var typeName = typeof(TestIntegrationClass).FullName;

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            var parameter = $"Parameter {i}";
            tasks.Add(Task.Run(() => MEF.CreateA<ITestIntegrationInterface>(typeName, parameter)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        foreach (var task in tasks)
        {
            var result = task.Result;
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<TestIntegrationClass>());
        }
    }

    [Test]
    public void Integration_MEFAssemblyLoad_HandlesDynamicAssemblyLoading()
    {
        // Arrange - This simulates what happens when assemblies are loaded dynamically
        var initialTypeCount = MEF.GetAllTypes().Count();

        // Act - Trigger a flush to simulate assembly loading
        var assemblyLoadHandler = typeof(MEF).GetMethod("Flush",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        assemblyLoadHandler?.Invoke(null, new object[] { null, null });

        var typeCountAfterFlush = MEF.GetAllTypes().Count();

        // Assert
        Assert.That(typeCountAfterFlush, Is.GreaterThanOrEqualTo(initialTypeCount));
    }

    [Test]
    public void Integration_MEFTypeHierarchy_HandlesInheritance()
    {
        // Arrange
        var baseType = typeof(ITestIntegrationInterface);

        // Act
        var derivedTypes = MEF.GetTypes<ITestIntegrationInterface>().ToList();

        // Assert
        Assert.That(derivedTypes, Is.Not.Empty);
        Assert.That(derivedTypes.All(t => baseType.IsAssignableFrom(t)), Is.True);
        Assert.That(derivedTypes.Contains(typeof(TestIntegrationClass)), Is.True);
    }

    [Test]
    public void Integration_MEFGenerics_HandlesGenericTypes()
    {
        // Arrange
        var genericInterface = typeof(IList<>);
        var elementType = typeof(string);

        // Act
        var concreteTypes = MEF.GetGenericTypes(genericInterface, elementType).ToList();

        // Assert
        Assert.That(concreteTypes, Is.Not.Empty);
        var expectedInterface = genericInterface.MakeGenericType(elementType);
        Assert.That(concreteTypes.All(t => expectedInterface.IsAssignableFrom(t)), Is.True);
    }

    [Test]
    public void Integration_RealWorldScenario_CreatesFullObjectGraph()
    {
        // Arrange - Simulate a real-world scenario with multiple objects
        var catalogue = new Catalogue(_catalogueRepository, "Integration Test Catalogue");

        // Act
        var relatedObjects = new List<object>
        {
            catalogue,
            ObjectConstructor.Construct(typeof(TestIntegrationClass), "Catalogue Test"),
            ObjectConstructor.Construct(typeof(RepositoryDependentClass), _catalogueRepository)
        };

        // Assert
        Assert.That(relatedObjects.All(o => o != null), Is.True);
        Assert.That(relatedObjects.OfType<Catalogue>().First().ID, Is.EqualTo(catalogue.ID));
    }

    [Test]
    public void Integration_ErrorHandling_InvalidConstructor_ThrowsAppropriateException()
    {
        // Arrange
        var testType = typeof(ExceptionThrowingConstructorClass);

        // Act & Assert
        Assert.Throws<Exception>(() => ObjectConstructor.Construct(testType, _catalogueRepository));
    }

    [Test]
    public void Integration_Performance_MultipleTypeLookups_PerformsWithinAcceptableTime()
    {
        // Arrange
        var typeNames = new[]
        {
            typeof(Catalogue).FullName,
            typeof(MEF).FullName,
            typeof(ObjectConstructor).FullName
        };
        var iterations = 100;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            foreach (var typeName in typeNames)
            {
                var type = MEF.GetType(typeName);
                Assert.That(type, Is.Not.Null);
            }
        }
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000)); // Should complete quickly
        Console.WriteLine($"Integration performance: {iterations * typeNames.Length} lookups in {stopwatch.ElapsedMilliseconds}ms");
    }

    #region Test Helper Classes

    public interface ITestIntegrationInterface { }

    public class TestIntegrationClass : ITestIntegrationInterface
    {
        public string TestValue { get; }

        public TestIntegrationClass(string testValue)
        {
            TestValue = testValue;
        }
    }

    public class ComplexConstructorClass
    {
        public string StringValue { get; }
        public int IntValue { get; }
        public bool BoolValue { get; }

        public ComplexConstructorClass(string stringValue, int intValue, bool boolValue)
        {
            StringValue = stringValue;
            IntValue = intValue;
            BoolValue = boolValue;
        }
    }

    public class RepositoryDependentClass
    {
        public ICatalogueRepository Repository { get; }

        public RepositoryDependentClass(ICatalogueRepository repository)
        {
            Repository = repository;
        }
    }

    public class BlankConstructorClass
    {
        // Blank constructor for fallback testing
        public BlankConstructorClass()
        {
        }
    }

    public class ExceptionThrowingConstructorClass
    {
        public ExceptionThrowingConstructorClass(ICatalogueRepository repository)
        {
            throw new InvalidOperationException("Simulated constructor failure");
        }
    }

    #endregion
}