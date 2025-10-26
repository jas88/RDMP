// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.Fields
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System.Collections.Generic;

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

public class Fields
{
    public Progress progress { get; set; }

    public string summary { get; set; }

    public Timetracking timetracking { get; set; }

    public IssueType issuetype { get; set; }

    public Votes votes { get; set; }

    public Resolution resolution { get; set; }

    public List<object> fixVersions { get; set; }

    public string resolutiondate { get; set; }

    public int? timespent { get; set; }

    public Author reporter { get; set; }

    public int? aggregatetimeoriginalestimate { get; set; }

    public string created { get; set; }

    public string updated { get; set; }

    public string description { get; set; }

    public Priority priority { get; set; }

    public string duedate { get; set; }

    public List<object> issuelinks { get; set; }

    public Watches watches { get; set; }

    public Worklogs worklog { get; set; }

    public List<Subtask> subtasks { get; set; }

    public Status status { get; set; }

    public List<string> labels { get; set; }

    public long? workratio { get; set; }

    public Author assignee { get; set; }

    public List<Attachment> attachment { get; set; }

    public int? aggregatetimeestimate { get; set; }

    public Project project { get; set; }

    public List<object> versions { get; set; }

    public string environment { get; set; }

    public int? timeestimate { get; set; }

    public Aggregateprogress aggregateprogress { get; set; }

    public List<Component> components { get; set; }

    public Comments comment { get; set; }

    public int? timeoriginalestimate { get; set; }

    public int? aggregatetimespent { get; set; }

    public string customfield_12208 { get; set; }

    public string customfield_13400 { get; set; }
}