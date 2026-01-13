// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;

namespace SCIStorePlugin.DataProvider;

/// <summary>
/// Defines a date range criteria with FromDate and ToDate, providing an EachDay iterator method for processing each day in the range sequentially.
/// </summary>
public class DateCriteria
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public void EachDay(Action<DateTime, DateTime> action)
    {
        var currentDate = FromDate;
        var oneDay = new TimeSpan(1, 0, 0, 0);
        while (currentDate <= ToDate)
        {
            var nextDate = currentDate.Add(oneDay);
            if (nextDate > DateTime.Now)
                nextDate = DateTime.Now;

            if (nextDate > ToDate)
            {
                if (currentDate < ToDate)
                    action(currentDate, ToDate);
                break;
            }

            action(currentDate, nextDate);

            currentDate = nextDate;
        }
    }
}