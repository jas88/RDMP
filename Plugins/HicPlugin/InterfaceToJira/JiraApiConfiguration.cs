// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JiraApiConfiguration
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System.Configuration;

namespace HIC.Common.InterfaceToJira;

public class JiraApiConfiguration : ConfigurationSection
{
    [ConfigurationProperty("ApiUrl", DefaultValue = "https://jira-hic.cmdn.dundee.ac.uk/rest/api/latest/", IsRequired = false)]
    public string ApiUrl
    {
        get => (string) this[nameof (ApiUrl)];
        set => this[nameof (ApiUrl)] = value;
    }

    [ConfigurationProperty("ServerUrl", DefaultValue = "https://jira-hic.cmdn.dundee.ac.uk", IsRequired = false)]
    public string ServerUrl
    {
        get => (string) this[nameof (ServerUrl)];
        set => this[nameof (ServerUrl)] = value;
    }

    [ConfigurationProperty("User", DefaultValue = "api", IsRequired = true)]
    public string User
    {
        get => (string) this[nameof (User)];
        set => this[nameof (User)] = value;
    }

    [ConfigurationProperty("Password", DefaultValue = "rest", IsRequired = true)]
    public string Password
    {
        get => (string) this[nameof (Password)];
        set => this[nameof (Password)] = value;
    }

    [ConfigurationProperty("UserDisplayName", DefaultValue = "User API", IsRequired = false)]
    public string UserDisplayName
    {
        get => (string) this[nameof (UserDisplayName)];
        set => this[nameof (UserDisplayName)] = value;
    }

    [ConfigurationProperty("UserEmail", DefaultValue = "l.tramma@dundee.ac.uk", IsRequired = false)]
    public string UserEmail
    {
        get => (string) this[nameof (UserEmail)];
        set => this[nameof (UserEmail)] = value;
    }

    [ConfigurationProperty("HelpdeskProjectKey", DefaultValue = "SUPPORT", IsRequired = false)]
    public string HelpdeskProjectKey
    {
        get => (string) this[nameof (HelpdeskProjectKey)];
        set => this[nameof (HelpdeskProjectKey)] = value;
    }

    [ConfigurationProperty("IssueTypeKeyCustomerRequest", DefaultValue = "10200", IsRequired = false)]
    public string IssueTypeKeyCustomerRequest
    {
        get => (string) this[nameof (IssueTypeKeyCustomerRequest)];
        set => this[nameof (IssueTypeKeyCustomerRequest)] = value;
    }

    [ConfigurationProperty("PriorityKeyMajor", DefaultValue = "2", IsRequired = false)]
    public string PriorityKeyMajor
    {
        get => (string) this[nameof (PriorityKeyMajor)];
        set => this[nameof (PriorityKeyMajor)] = value;
    }
}