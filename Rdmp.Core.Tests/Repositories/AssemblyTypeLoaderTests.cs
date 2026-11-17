// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;

namespace Rdmp.Core.Tests.Repositories;

/// <summary>
/// Tests for <see cref="AssemblyTypeLoader"/> with assemblies whose Types fail to load.  They are never loaded into the
/// runtime, so MEF never sees them, and failures are recorded in a dictionary of each test's own.
/// </summary>
internal class AssemblyTypeLoaderTests
{
    /// <summary>
    /// An assembly whose GetTypes throws the given exception
    /// </summary>
    private sealed class FailingAssembly : Assembly
    {
        private readonly Exception _error;

        public FailingAssembly(Exception error) => _error = error;

        public override string FullName => "FailingAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public override Type[] GetTypes() => throw _error;
    }

    [Test]
    public void GetTypesSafely_SomeTypesFailToLoad_ReturnsTheOthers()
    {
        // The Types which could not be loaded are null
        var partlyLoaded = new ReflectionTypeLoadException(new[] { typeof(Catalogue), null, typeof(MEF), null },
            new Exception[] { new TypeLoadException("Missing dependency"), new TypeLoadException("Missing dependency") });
        var failures = new Dictionary<string, Exception>();

        var types = AssemblyTypeLoader.GetTypesSafely(new FailingAssembly(partlyLoaded), failures);

        Assert.Multiple(() =>
        {
            Assert.That(types, Is.EqualTo(new[] { typeof(Catalogue), typeof(MEF) }));
            Assert.That(failures, Is.Empty);
        });
    }

    [Test]
    public void GetTypesSafely_TypesCannotBeListed_ReturnsNoneAndRecordsTheFailure()
    {
        var error = new FileNotFoundException("Could not load a dependency");
        var assembly = new FailingAssembly(error);
        var failures = new Dictionary<string, Exception>();

        var types = AssemblyTypeLoader.GetTypesSafely(assembly, failures);

        Assert.Multiple(() =>
        {
            Assert.That(types, Is.Empty);
            Assert.That(failures.Keys, Is.EqualTo(new[] { assembly.FullName }));
            Assert.That(failures[assembly.FullName], Is.SameAs(error));
        });
    }

    [Test]
    public void GetTypesSafely_TypesCannotBeListedAndNoFailuresDictionary_ReturnsNone()
    {
        var assembly = new FailingAssembly(new FileNotFoundException("Could not load a dependency"));

        Assert.That(AssemblyTypeLoader.GetTypesSafely(assembly), Is.Empty);
    }
}
