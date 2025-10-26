// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿// Decompiled with JetBrains decompiler
// Type: HIC.Demography.CHIJobValidationResult
// Assembly: HIC.Demography, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 82227946-33C8-4895-ACC9-8D968B5A9DFA
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\hic.demography\1.0.0\lib\net45\HIC.Demography.dll

namespace HIC.Demography;

public class CHIJobValidationResult
{
    public CHIJobValidationResult(ValidationCategory result) => Result = result;

    public CHIJobValidationResult(ValidationCategory result, string reason)
    {
        Result = result;
        Reason = reason;
    }

    public string? Reason { get; set; }

    public ValidationCategory Result { get; set; }
}