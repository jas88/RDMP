// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;

namespace Rdmp.Core.Repositories;

/// <summary>
/// Thrown when the Types of an assembly cannot be taken from the <see cref="CompiledTypeRegistry"/>, e.g. because one of
/// the registered Types fails to load or an RDMP assembly was built without the type registry generator.  <see cref="MEF"/>
/// throws it if the registry is required (as it is in the tests), otherwise it logs it and finds Types with reflection
/// instead.
/// </summary>
public sealed class CompiledTypeRegistryException : Exception
{
    public CompiledTypeRegistryException(string message) : base(message)
    {
    }

    public CompiledTypeRegistryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
