// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.MapsDirectlyToDatabaseTable;

namespace Rdmp.Core.Tests.MapsDirectlyToDatabaseTable;

public class PropertyAccessorCacheTests
{
    private class TestEntity
    {
        public int IntProperty { get; set; }
        public string StringProperty { get; set; }
        public DateTime DateProperty { get; set; }
        public TestEnum EnumProperty { get; set; }
        public string ReadOnlyProperty { get; } = "ReadOnly";
        public decimal? NullableProperty { get; set; }
    }

    private enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    [Test]
    public void GetAccessor_ValidProperty_ReturnsAccessor()
    {
        var accessor = PropertyAccessorCache.GetAccessor(typeof(TestEntity), nameof(TestEntity.IntProperty));

        Assert.That(accessor, Is.Not.Null);
        Assert.That(accessor.PropertyInfo.Name, Is.EqualTo(nameof(TestEntity.IntProperty)));
        Assert.That(accessor.PropertyType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void GetAccessor_InvalidProperty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            PropertyAccessorCache.GetAccessor(typeof(TestEntity), "NonExistentProperty"));
    }

    [Test]
    public void GetAccessor_CachesAccessor_ReturnsSameInstance()
    {
        var accessor1 = PropertyAccessorCache.GetAccessor(typeof(TestEntity), nameof(TestEntity.IntProperty));
        var accessor2 = PropertyAccessorCache.GetAccessor(typeof(TestEntity), nameof(TestEntity.IntProperty));

        Assert.That(accessor1, Is.SameAs(accessor2), "Accessor should be cached");
    }

    [Test]
    public void GetValue_IntProperty_ReturnsCorrectValue()
    {
        var entity = new TestEntity { IntProperty = 42 };
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.IntProperty));

        var value = accessor.GetValue(entity);

        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void SetValue_IntProperty_SetsCorrectValue()
    {
        var entity = new TestEntity();
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.IntProperty));

        accessor.SetValue(entity, 100);

        Assert.That(entity.IntProperty, Is.EqualTo(100));
    }

    [Test]
    public void GetValue_StringProperty_ReturnsCorrectValue()
    {
        var entity = new TestEntity { StringProperty = "test value" };
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.StringProperty));

        var value = accessor.GetValue(entity);

        Assert.That(value, Is.EqualTo("test value"));
    }

    [Test]
    public void SetValue_StringProperty_SetsCorrectValue()
    {
        var entity = new TestEntity();
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.StringProperty));

        accessor.SetValue(entity, "new value");

        Assert.That(entity.StringProperty, Is.EqualTo("new value"));
    }

    [Test]
    public void GetValue_DateTimeProperty_ReturnsCorrectValue()
    {
        var testDate = new DateTime(2025, 10, 21);
        var entity = new TestEntity { DateProperty = testDate };
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.DateProperty));

        var value = accessor.GetValue(entity);

        Assert.That(value, Is.EqualTo(testDate));
    }

    [Test]
    public void GetValue_EnumProperty_ReturnsCorrectValue()
    {
        var entity = new TestEntity { EnumProperty = TestEnum.Value2 };
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.EnumProperty));

        var value = accessor.GetValue(entity);

        Assert.That(value, Is.EqualTo(TestEnum.Value2));
    }

    [Test]
    public void SetValue_EnumProperty_SetsCorrectValue()
    {
        var entity = new TestEntity();
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.EnumProperty));

        accessor.SetValue(entity, TestEnum.Value3);

        Assert.That(entity.EnumProperty, Is.EqualTo(TestEnum.Value3));
    }

    [Test]
    public void GetValue_NullableProperty_ReturnsNull()
    {
        var entity = new TestEntity { NullableProperty = null };
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.NullableProperty));

        var value = accessor.GetValue(entity);

        Assert.That(value, Is.Null);
    }

    [Test]
    public void GetValue_NullableProperty_ReturnsValue()
    {
        var entity = new TestEntity { NullableProperty = 123.45m };
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.NullableProperty));

        var value = accessor.GetValue(entity);

        Assert.That(value, Is.EqualTo(123.45m));
    }

    [Test]
    public void SetValue_NullableProperty_SetsNull()
    {
        var entity = new TestEntity { NullableProperty = 999m };
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.NullableProperty));

        accessor.SetValue(entity, null);

        Assert.That(entity.NullableProperty, Is.Null);
    }

    [Test]
    public void SetValue_ReadOnlyProperty_ThrowsInvalidOperationException()
    {
        var entity = new TestEntity();
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.ReadOnlyProperty));

        Assert.Throws<InvalidOperationException>(() =>
            accessor.SetValue(entity, "new value"));
    }

    [Test]
    public void GetValue_NullObject_ThrowsArgumentNullException()
    {
        var accessor = PropertyAccessorCache.GetAccessor(typeof(TestEntity), nameof(TestEntity.IntProperty));

        Assert.Throws<ArgumentNullException>(() =>
            accessor.GetValue(null));
    }

    [Test]
    public void SetValue_NullObject_ThrowsArgumentNullException()
    {
        var accessor = PropertyAccessorCache.GetAccessor(typeof(TestEntity), nameof(TestEntity.IntProperty));

        Assert.Throws<ArgumentNullException>(() =>
            accessor.SetValue(null, 42));
    }

    [Test]
    public void GetAccessor_FromObject_NullObject_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PropertyAccessorCache.GetAccessor((object)null, "property"));
    }

    [Test]
    public void CompiledAccessor_PerformsReasonably()
    {
        // Simple sanity check that compiled accessor works in reasonable time
        var entity = new TestEntity { IntProperty = 42 };
        const int iterations = 100000; // Enough to measure, not too long for CI

        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.IntProperty));

        // Warm up JIT
        for (var i = 0; i < 1000; i++)
            accessor.GetValue(entity);

        // Measure accessor performance
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            var val = accessor.GetValue(entity);
            Assert.That(val, Is.EqualTo(42));
        }
        sw.Stop();

        // Just verify it completes in reasonable time (should be <100ms, allow 500ms for slow CI)
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500),
            $"Accessor should complete {iterations} iterations quickly, took {sw.ElapsedMilliseconds}ms");

        TestContext.Out.WriteLine($"Compiled accessor: {iterations} iterations in {sw.ElapsedMilliseconds}ms");
    }

    [Test]
    [Explicit("Performance benchmark - run manually for detailed timing comparison")]
    [Category("Performance")]
    public void PerformanceBenchmark_CompiledAccessor_VsReflection()
    {
        // Detailed performance comparison - only run manually, not in CI
        // CI environments are too noisy for reliable performance assertions
        var entity = new TestEntity { IntProperty = 42 };
        const int iterations = 5000000; // Higher for accurate measurement
        const int warmupIterations = 100000;

        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.IntProperty));
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.IntProperty));

        // Extended warm up to ensure full JIT optimization
        for (var i = 0; i < warmupIterations; i++)
        {
            accessor.GetValue(entity);
            prop.GetValue(entity);
        }

        // Force GC before measurements
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Measure compiled accessor (5 runs, discard first)
        var accessorTimes = new List<long>();
        for (var run = 0; run < 5; run++)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                accessor.GetValue(entity);
            sw.Stop();
            if (run > 0) // Discard first run
                accessorTimes.Add(sw.ElapsedMilliseconds);
        }

        // Measure reflection (5 runs, discard first)
        var reflectionTimes = new List<long>();
        for (var run = 0; run < 5; run++)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                prop.GetValue(entity);
          sw.Stop();
            if (run > 0) // Discard first run
                reflectionTimes.Add(sw.ElapsedMilliseconds);
        }

        var accessorAvg = accessorTimes.Average();
        var reflectionAvg = reflectionTimes.Average();
        var speedup = reflectionAvg / accessorAvg;
        TestContext.Out.WriteLine($"Compiled accessor (avg): {accessorAvg:F2}ms");
        TestContext.Out.WriteLine($"Reflection (avg): {reflectionAvg:F2}ms");
        TestContext.Out.WriteLine($"Speedup: {speedup:F2}x");
        }

        TestContext.Out.WriteLine("=== Performance Benchmark Results ===");
        TestContext.Out.WriteLine($"Iterations per run: {iterations:N0}");
        TestContext.Out.WriteLine($"Compiled accessor (avg): {accessorAvg:F2}ms");
        TestContext.Out.WriteLine($"Reflection (avg): {reflectionAvg:F2}ms");
        TestContext.Out.WriteLine($"Speedup: {speedup:F2}x");
        TestContext.Out.WriteLine($"Accessor times: {string.Join(", ", accessorTimes)}ms");
        TestContext.Out.WriteLine($"Reflection times: {string.Join(", ", reflectionTimes)}ms");

        // No assertion - this is informational only
        // Performance varies too much across environments for reliable assertions
    }

    [Test]
    public void GetAccessor_MultipleCalls_ReturnsSameCachedInstance()
    {
        // Verify that the cache is working by ensuring multiple calls return the same instance
        var accessor1 = PropertyAccessorCache.GetAccessor(typeof(TestEntity), nameof(TestEntity.IntProperty));
        var accessor2 = PropertyAccessorCache.GetAccessor(typeof(TestEntity), nameof(TestEntity.IntProperty));
        var accessor3 = PropertyAccessorCache.GetAccessor(typeof(TestEntity), nameof(TestEntity.IntProperty));

        Assert.That(accessor1, Is.SameAs(accessor2), "Multiple calls should return same cached instance");
        Assert.That(accessor2, Is.SameAs(accessor3), "Multiple calls should return same cached instance");
    }
}
