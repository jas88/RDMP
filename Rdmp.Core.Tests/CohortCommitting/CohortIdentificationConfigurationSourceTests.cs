// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using NUnit.Framework;
using Rdmp.Core.CohortCommitting.Pipeline.Sources;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Cohort;
using Rdmp.Core.Databases;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common;

namespace Rdmp.Core.Tests.CohortCommitting;

internal class CohortIdentificationConfigurationSourceTests : UnitTests
{
    [Test]
    public void TryGetPreview_AfterAbort_ReportsTheCancellation()
    {
        var cic = WhenIHaveA<CohortIdentificationConfiguration>();
        cic.CreateRootContainerIfNotExists();

        // with a cache server the source runs the configuration with a CohortCompilerRunner, which returns no task when cancelled
        var cache = new ExternalDatabaseServer(Repository.CatalogueRepository, "Cache", new QueryCachingPatcher());
        cic.QueryCachingServer_ID = cache.ID;
        cic.SaveToDatabase();

        var source = new CohortIdentificationConfigurationSource();
        source.PreInitialize(cic, ThrowImmediatelyDataLoadEventListener.Quiet);
        source.Abort(ThrowImmediatelyDataLoadEventListener.Quiet);

        var ex = Assert.Throws<Exception>(() => source.TryGetPreview());
        Assert.That(ex?.Message, Does.Contain("was the execution cancelled"));
    }
}
