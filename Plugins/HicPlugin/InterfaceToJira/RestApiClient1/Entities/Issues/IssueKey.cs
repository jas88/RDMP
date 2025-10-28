// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues.IssueKey
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient1.Entities.Issues;

public class IssueKey
{
    public string ProjectKey { get; set; }

    public int IssueId { get; set; }

    public IssueKey()
    {
    }

    public IssueKey(string projectKey, int issueId)
    {
        ProjectKey = projectKey;
        IssueId = issueId;
    }

    public static IssueKey Parse(string issueKeyString)
    {
        var strArray = issueKeyString?.Split('-') ?? throw new ArgumentNullException(nameof(issueKeyString),"IssueKeyString is null!");
        if (strArray.Length != 2)
            throw new ArgumentException("The string entered is not a JIRA key!");
        return int.TryParse(strArray[1], out var result) ? new IssueKey(strArray[0], result) : throw new ArgumentException("The string entered could not be parsed, issue id is non-integer!");
    }

    public override string ToString() => $"{ProjectKey}-{IssueId}";
}