// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;
using SCIStorePlugin.Repositories;
using NSubstitute;

namespace SCIStorePluginTests.Unit;

public class LimitedRetryThenContinueStrategyTests
{
    [Test]
    public void Test()
    {
        var mockServer = Substitute.For<IRepositorySupportsDateRangeQueries<CombinedReportData>>();

        var strategy = new LimitedRetryThenContinueStrategy(5,new List<int>(new int[]{3,1}), mockServer);

            
       Assert.That(4,Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1,0,0,0), ThrowImmediatelyDataLoadEventListener.Quiet, 5, new Exception())));
       Assert.That(3, Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), ThrowImmediatelyDataLoadEventListener.Quiet, 4, new Exception())));
       Assert.That(2, Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), ThrowImmediatelyDataLoadEventListener.Quiet, 3, new Exception())));
       Assert.That(1, Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), ThrowImmediatelyDataLoadEventListener.Quiet, 2, new Exception())));
       Assert.That(0, Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), ThrowImmediatelyDataLoadEventListener.Quiet, 1, new Exception())));

    }
}