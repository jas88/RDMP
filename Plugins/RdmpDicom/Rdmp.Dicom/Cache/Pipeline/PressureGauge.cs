// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core.ReusableLibraryCode.Progress;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rdmp.Dicom.Cache.Pipeline;

/// <summary>
/// Tracks unspecified events and performs a delegate action when the frequency exceeds the <see cref="ThresholdBeatsPerMinute"/>
/// </summary>
public class PressureGauge
{
    /// <summary>
    /// Number of events that are allowable per minute
    /// </summary>
    public long ThresholdBeatsPerMinute { get; set; }

    List<DateTime> collection = new();
    object oLock = new();

    /// <summary>
    /// Marks that an event happened at the current date time
    /// </summary>
    /// <param name="listener"></param>
    /// <param name="pressureTooHigh">Delegate to invoke if <see cref="ThresholdBeatsPerMinute"/> is exceeded</param>
    public void Tick(IDataLoadEventListener listener, Action pressureTooHigh)
    {
        Tick(DateTime.Now, listener, pressureTooHigh);
    }

    /// <summary>
    /// Marks that an event happened at <paramref name="eventDate"/>
    /// </summary>
    /// <param name="eventDate">Time of event.  Must be greater than any previous event dates</param>
    /// <param name="listener"></param>
    /// <param name="pressureTooHigh">Delegate to invoke if <see cref="ThresholdBeatsPerMinute"/> is exceeded</param>
    public void Tick(DateTime eventDate, IDataLoadEventListener listener, Action pressureTooHigh)
    {
        bool exceeded;
        lock (oLock)
        {
            // filter collection to only recent events
            collection = collection.Where(c => eventDate.Subtract(c) < TimeSpan.FromMinutes(1)).ToList();

            collection.Add(eventDate);

            exceeded = collection.Count > ThresholdBeatsPerMinute;
        }

        if (!exceeded) return;
        // Important to use log level Information here and not Error in case the listener breaks flow control e.g. ThrowImmediately listener
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "ThresholdBeatsPerMinute exceeded, invoking delegate"));
        pressureTooHigh();
    }
}