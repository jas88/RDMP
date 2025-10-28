// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Net.Http;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System.Threading;
using System.Net;

namespace Rdmp.Dicom.Tests.Integration;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface |
                AttributeTargets.Assembly, AllowMultiple = true)]
public class RequiresSemEHR : Attribute, IApplyToContext
{
    static readonly HttpClient HttpClient = new();
    public const string SemEhrTestUrl = "https://localhost:8485";

    public void ApplyToContext(TestExecutionContext context)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        try
        {
            var response = HttpClient.GetAsync(SemEhrTestUrl, cts.Token).Result;

            //Check the status code is 200 success
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.Ignore($"SemEHR did not respond correctly on {SemEhrTestUrl}: {response.StatusCode}");
            }
        }
        catch (Exception)
        {
            Assert.Ignore($"SemEHR not running on {SemEhrTestUrl}");
        }
    }
}