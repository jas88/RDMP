// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using Rdmp.Core.Caching.Requests;
using System;

namespace Rdmp.Dicom.Cache;

/// <summary>
/// Represents a chunk of DICOM data fetched from a PACS, including the modality, fetch date, and cache layout information
/// </summary>
public class SMIDataChunk : ICacheChunk
{
    public ICacheFetchRequest Request { get; }
    public string Modality { get; set; }
    public DateTime FetchDate { get; set; }
    public SMICacheLayout Layout { get; set; }

    public SMIDataChunk(ICacheFetchRequest request)
    {
        Request = request;
    }
}