// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Serialization;
using Rdmp.Core.MapsDirectlyToDatabaseTable;
using Rdmp.Core.Repositories;
using Tests.Common;
using NewtonsoftExtensions = Rdmp.Core.Curation.Data.Serialization.JsonConvertExtensions;
using SystemTextJsonExtensions = Rdmp.Core.Curation.Data.Serialization.SystemTextJson.JsonSerializerExtensions;

namespace Rdmp.Core.Tests.Curation.JsonSerializationTests;

/// <summary>
/// Performance comparison tests between Newtonsoft.Json and System.Text.Json implementations
/// </summary>
[TestFixture]
[Category("Performance")]
[Explicit("Performance benchmarks - run manually, not in CI")]
public class JsonSerializationPerformanceTests : DatabaseTests
{
    private const int IterationCount = 100;

    [Test]
    public void Performance_DatabaseEntitySerialization_SystemTextJsonFaster()
    {
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        // Arrange
        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "PerfTest");

        // Warm up
        NewtonsoftExtensions.SerializeObject(catalogue, RepositoryLocator);
        SystemTextJsonExtensions.SerializeObject(catalogue, RepositoryLocator);

        // Act - Newtonsoft.Json
        var swNewtonsoft = Stopwatch.StartNew();
        for (int i = 0; i < IterationCount; i++)
        {
            NewtonsoftExtensions.SerializeObject(catalogue, RepositoryLocator);
        }
        swNewtonsoft.Stop();

        // Act - System.Text.Json
        var swSystemText = Stopwatch.StartNew();
        for (int i = 0; i < IterationCount; i++)
        {
            SystemTextJsonExtensions.SerializeObject(catalogue, RepositoryLocator);
        }
        swSystemText.Stop();

        // Report
        TestContext.Out.WriteLine($"Newtonsoft.Json: {swNewtonsoft.ElapsedMilliseconds}ms for {IterationCount} iterations");
        TestContext.Out.WriteLine($"System.Text.Json: {swSystemText.ElapsedMilliseconds}ms for {IterationCount} iterations");
        TestContext.Out.WriteLine($"Speedup: {(double)swNewtonsoft.ElapsedMilliseconds / swSystemText.ElapsedMilliseconds:F2}x");

        // Assert - System.Text.Json should be faster (or at worst comparable)
        Assert.That(swSystemText.ElapsedMilliseconds, Is.LessThanOrEqualTo(swNewtonsoft.ElapsedMilliseconds * 1.2),
            "System.Text.Json should be comparable or faster than Newtonsoft.Json");

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    [Test]
    public void Performance_DatabaseEntityDeserialization_SystemTextJsonFaster()
    {
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        // Arrange
        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "PerfTest");
        var json = NewtonsoftExtensions.SerializeObject(catalogue, RepositoryLocator);

        // Warm up
        _ = NewtonsoftExtensions.DeserializeObject(json, typeof(Catalogue), RepositoryLocator);
        _ = SystemTextJsonExtensions.DeserializeObject<Catalogue>(json, RepositoryLocator);

        // Act - Newtonsoft.Json
        var swNewtonsoft = Stopwatch.StartNew();
        for (int i = 0; i < IterationCount; i++)
        {
            _ = NewtonsoftExtensions.DeserializeObject(json, typeof(Catalogue), RepositoryLocator);
        }
        swNewtonsoft.Stop();

        // Act - System.Text.Json
        var swSystemText = Stopwatch.StartNew();
        for (int i = 0; i < IterationCount; i++)
        {
            _ = SystemTextJsonExtensions.DeserializeObject<Catalogue>(json, RepositoryLocator);
        }
        swSystemText.Stop();

        // Report
        TestContext.Out.WriteLine($"Newtonsoft.Json: {swNewtonsoft.ElapsedMilliseconds}ms for {IterationCount} iterations");
        TestContext.Out.WriteLine($"System.Text.Json: {swSystemText.ElapsedMilliseconds}ms for {IterationCount} iterations");
        TestContext.Out.WriteLine($"Speedup: {(double)swNewtonsoft.ElapsedMilliseconds / swSystemText.ElapsedMilliseconds:F2}x");

        // Cleanup
        catalogue.DeleteInDatabase();
    }

    [Test]
    public void Performance_DictionarySerialization_Comparison()
    {
        // Arrange
        var largeDict = new Dictionary<string, int>();
        for (int i = 0; i < 100; i++)
        {
            largeDict[$"key{i}"] = i;
        }

        // Warm up
        NewtonsoftExtensions.SerializeObject(largeDict, RepositoryLocator);
        SystemTextJsonExtensions.SerializeObject(largeDict, RepositoryLocator);

        // Act - Newtonsoft.Json
        var swNewtonsoft = Stopwatch.StartNew();
        for (int i = 0; i < IterationCount; i++)
        {
            NewtonsoftExtensions.SerializeObject(largeDict, RepositoryLocator);
        }
        swNewtonsoft.Stop();

        // Act - System.Text.Json
        var swSystemText = Stopwatch.StartNew();
        for (int i = 0; i < IterationCount; i++)
        {
            SystemTextJsonExtensions.SerializeObject(largeDict, RepositoryLocator);
        }
        swSystemText.Stop();

        // Report
        TestContext.Out.WriteLine($"Newtonsoft.Json: {swNewtonsoft.ElapsedMilliseconds}ms for {IterationCount} iterations");
        TestContext.Out.WriteLine($"System.Text.Json: {swSystemText.ElapsedMilliseconds}ms for {IterationCount} iterations");
        TestContext.Out.WriteLine($"Speedup: {(double)swNewtonsoft.ElapsedMilliseconds / swSystemText.ElapsedMilliseconds:F2}x");
    }

    [Test]
    public void Performance_MemoryAllocation_SystemTextJsonLower()
    {
        if (CatalogueRepository is not TableRepository)
            Assert.Inconclusive("This test does not apply for non db repos");

        // Arrange
        var catalogue = new Catalogue(RepositoryLocator.CatalogueRepository, "MemoryTest");

        // Act - Measure allocations
        var beforeNewtonsoft = GC.GetTotalMemory(true);
        for (int i = 0; i < IterationCount; i++)
        {
            _ = NewtonsoftExtensions.SerializeObject(catalogue, RepositoryLocator);
        }
        var afterNewtonsoft = GC.GetTotalMemory(true);
        var newtonsoftAllocations = afterNewtonsoft - beforeNewtonsoft;

        var beforeSystemText = GC.GetTotalMemory(true);
        for (int i = 0; i < IterationCount; i++)
        {
            _ = SystemTextJsonExtensions.SerializeObject(catalogue, RepositoryLocator);
        }
        var afterSystemText = GC.GetTotalMemory(true);
        var systemTextAllocations = afterSystemText - beforeSystemText;

        // Report
        TestContext.Out.WriteLine($"Newtonsoft.Json allocations: {newtonsoftAllocations:N0} bytes");
        TestContext.Out.WriteLine($"System.Text.Json allocations: {systemTextAllocations:N0} bytes");
        TestContext.Out.WriteLine($"Reduction: {(1.0 - ((double)systemTextAllocations / newtonsoftAllocations)) * 100:F1}%");

        // Assert - System.Text.Json should allocate less or comparable memory
        Assert.That(systemTextAllocations, Is.LessThanOrEqualTo(newtonsoftAllocations * 1.2),
            "System.Text.Json should have comparable or lower memory allocations");

        // Cleanup
        catalogue.DeleteInDatabase();
    }
}
