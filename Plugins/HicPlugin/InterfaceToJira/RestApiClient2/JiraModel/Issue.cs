// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel.Issue
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

namespace HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;

public class Issue : BasicIssue
{
    public const string FIELD_PROGRESS = "progress";
    public const string FIELD_SUMMARY = "summary";
    public const string FIELD_TIMETRACKING = "timetracking";
    public const string FIELD_ISSUETYPE = "issuetype";
    public const string FIELD_VOTES = "votes";
    public const string FIELD_RESOLUTION = "resolution";
    public const string FIELD_FIXVERSIONS = "fixVersions";
    public const string FIELD_RESOLUTIONDATE = "resolutiondate";
    public const string FIELD_TIMESPENT = "timespent";
    public const string FIELD_REPORTER = "reporter";
    public const string FIELD_AGGREGATEIMEORIHINALESTIMATE = "aggregatetimeoriginalestimate";
    public const string FIELD_CREATED = "created";
    public const string FIELD_UPDATED = "updated";
    public const string FIELD_DESCRIPTION = "description";
    public const string FIELD_PRIORITY = "priority";
    public const string FIELD_DUEDATE = "duedate";
    public const string FIELD_ISSUELINKS = "issuelinks";
    public const string FIELD_WATCHES = "watches";
    public const string FIELD_WORKLOG = "worklog";
    public const string FIELD_SUBTASKS = "subtasks";
    public const string FIELD_STATUS = "status";
    public const string FIELD_LABELS = "labels";
    public const string FIELD_WORKRATIO = "workratio";
    public const string FIELD_ASSIGNEE = "assignee";
    public const string FIELD_ATTACHMENT = "attachment";
    public const string FIELD_AGGREGATETIMEESTIMATE = "aggregatetimeestimate";
    public const string FIELD_PROJECT = "project";
    public const string FIELD_VERSIONS = "versions";
    public const string FIELD_ENVIRONMENT = "environment";
    public const string FIELD_TIMEESTIMATE = "timeestimate";
    public const string FIELD_AGGREGATEPROGESS = "aggregateprogress";
    public const string FIELD_COMPONENTS = "components";
    public const string FIELD_COMMENT = "comment";
    public const string FIELD_TIMEORIGINALESTIMATE = "timeoriginalestimate";
    public const string FIELD_AGGREGATETIMESPENT = "aggregatetimespent";

    public Fields fields { get; set; }
}