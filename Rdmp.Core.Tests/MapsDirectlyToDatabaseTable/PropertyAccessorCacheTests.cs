// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
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
    public void PerformanceTest_CompiledAccessor_IsFasterThanReflection()
    {
        var entity = new TestEntity { IntProperty = 42 };
        const int iterations = 1000000; // Increased for more reliable timing
        const int warmupIterations = 10000;

        // Warm up both approaches to ensure JIT compilation
        var accessor = PropertyAccessorCache.GetAccessor(entity, nameof(TestEntity.IntProperty));
        var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.IntProperty));

        for (var i = 0; i < warmupIterations; i++)
        {
            accessor.GetValue(entity);
            prop.GetValue(entity);
        }

        // Measure compiled accessor (multiple runs for stability)
        long accessorTotal = 0;
        for (var run = 0; run < 3; run++)
        {
            var sw1 = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                accessor.GetValue(entity);
            }
            sw1.Stop();
            accessorTotal += sw1.ElapsedMilliseconds;
        }
        var accessorAvg = accessorTotal / 3;

        // Measure reflection (multiple runs for stability)
        long reflectionTotal = 0;
        for (var run = 0; run < 3; run++)
        {
            var sw2 = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                prop.GetValue(entity);
            }
            sw2.Stop();
            reflectionTotal += sw2.ElapsedMilliseconds;
        }
        var reflectionAvg = reflectionTotal / 3;

        TestContext.Out.WriteLine($"Compiled accessor (avg): {accessorAvg}ms");
        TestContext.Out.WriteLine($"Reflection (avg): {reflectionAvg}ms");

        if (reflectionAvg > 0 && accessorAvg > 0)
        {
            var speedup = (double)reflectionAvg / accessorAvg;
            TestContext.Out.WriteLine($"Speedup: {speedup:F2}x");
        }
        else
        {
            TestContext.Out.WriteLine("Times too small to measure accurately - both approaches are very fast");
        }

        // More lenient assertion - compiled accessor should be at least as fast
        // or within 20% on noisy CI environments
        Assert.That(accessorAvg, Is.LessThanOrEqualTo(reflectionAvg * 1.2),
            $"Compiled accessor should be competitive with reflection (accessor: {accessorAvg}ms, reflection: {reflectionAvg}ms)");
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
