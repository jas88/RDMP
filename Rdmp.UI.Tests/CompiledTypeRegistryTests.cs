// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using Rdmp.Core.Repositories;
using Rdmp.UI.Collections;
using ResearchDataManagementPlatform.WindowManagement;
using Tests.Common;

namespace Rdmp.UI.Tests;

/// <summary>
/// Tests that MEF finds the Types of the user interface assemblies through the compiled type registry (see Rdmp.Core.Tests
/// for the comparison of the registry with reflection)
/// </summary>
public class CompiledTypeRegistryTests
{
    [Test]
    public void MEF_UsesTheCompiledTypeRegistry()
    {
        Assert.That(MEF.UsesCompiledTypeRegistry, "MEF fell back on reflection, see MEF.ListBadAssemblies()");
        Assert.That(MEF.GetType(typeof(RDMPCollectionCommonFunctionality).FullName),
            Is.EqualTo(typeof(RDMPCollectionCommonFunctionality)));
    }

    [Test]
    public void RdmpUI_RegistersTheTypesReflectionFinds() =>
        CompiledTypeRegistryChecks.AssertRegistersTheTypesReflectionFinds(typeof(RDMPCollectionCommonFunctionality).Assembly);

    [Test]
    public void ResearchDataManagementPlatform_RegistersTheTypesReflectionFinds() =>
        CompiledTypeRegistryChecks.AssertRegistersTheTypesReflectionFinds(typeof(WindowManager).Assembly);

    [Test]
    public void RdmpUITests_RegistersTheTypesReflectionFinds() =>
        CompiledTypeRegistryChecks.AssertRegistersTheTypesReflectionFinds(typeof(CompiledTypeRegistryTests).Assembly);
}
