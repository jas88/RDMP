// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.Repositories;
using Rdmp.Dicom.Extraction.FoDicomBased;
using Tests.Common;

namespace Rdmp.Dicom.Tests;

/// <summary>
/// Tests that MEF finds the Types of the Dicom assemblies through the compiled type registry (see Rdmp.Core.Tests for the
/// comparison of the registry with reflection)
/// </summary>
public class CompiledTypeRegistryTests
{
    [Test]
    public void MEF_UsesTheCompiledTypeRegistry()
    {
        Assert.That(MEF.UsesCompiledTypeRegistry, "MEF fell back on reflection, see MEF.ListBadAssemblies()");
        Assert.That(MEF.GetType(typeof(FoDicomAnonymiser).FullName), Is.EqualTo(typeof(FoDicomAnonymiser)));
    }

    [Test]
    public void RdmpDicom_RegistersTheTypesReflectionFinds() =>
        CompiledTypeRegistryChecks.AssertRegistersTheTypesReflectionFinds(typeof(FoDicomAnonymiser).Assembly);

    [Test]
    public void RdmpDicomTests_RegistersTheTypesReflectionFinds() =>
        CompiledTypeRegistryChecks.AssertRegistersTheTypesReflectionFinds(typeof(CompiledTypeRegistryTests).Assembly);
}
