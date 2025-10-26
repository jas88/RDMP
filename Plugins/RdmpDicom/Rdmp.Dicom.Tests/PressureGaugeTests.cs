// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using NUnit.Framework;
using Rdmp.Dicom.Cache.Pipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System;

namespace Rdmp.Dicom.Tests;

class PressureGaugeTests
{
    [Test]
    public void TestGauge_NotReached()
    {
        var someFact = false;

        var g = new PressureGauge
        {
            ThresholdBeatsPerMinute = 4
        };
        g.Tick(new DateTime(2001, 01, 01, 01, 01, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
    }
    [Test]
    public void TestGauge_NotReached_OverTime()
    {
        var someFact = false;

        var g = new PressureGauge
        {
            ThresholdBeatsPerMinute = 1
        };

        // events are 1 minute appart so does not trigger
        g.Tick(new(2001, 01, 01, 01, 01, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
        g.Tick(new(2001, 01, 01, 01, 02, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
        g.Tick(new(2001, 01, 01, 01, 03, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
    }
    [Test]
    public void TestGauge_Reached()
    {
        var someFact = false;

        var g = new PressureGauge
        {
            ThresholdBeatsPerMinute = 4
        };
        g.Tick(new DateTime(2001, 01, 01, 01, 01, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
        g.Tick(new(2001, 01, 01, 01, 01, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
        g.Tick(new(2001, 01, 01, 01, 01, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
        g.Tick(new(2001, 01, 01, 01, 01, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
        g.Tick(new(2001, 01, 01, 01, 01, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact);
    }

    [Test]
    public void TestGauge_Reached_OverTime()
    {
        var someFact = false;

        var g = new PressureGauge
        {
            ThresholdBeatsPerMinute = 1
        };
        g.Tick(new DateTime(2001, 01, 01, 01, 01, 01), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
        g.Tick(new(2001, 01, 01, 01, 01, 30), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact);
    }
    [Test]
    public void TestGauge_Reached_OverTime_Boundary()
    {
        var someFact = false;

        var g = new PressureGauge
        {
            ThresholdBeatsPerMinute = 1
        };
        g.Tick(new DateTime(2001, 01, 01, 01, 01, 30), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact, Is.False);
        g.Tick(new(2001, 01, 01, 01, 02, 29), ThrowImmediatelyDataLoadEventListener.Quiet, () => someFact = true);
        Assert.That(someFact);
    }
}