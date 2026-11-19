// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;

namespace Rdmp.Core.Repositories;

/// <summary>
/// Marks an assembly which registers its Types with the <see cref="CompiledTypeRegistry"/>.  Applied by the code which the
/// type registry generator (Rdmp.Core.Generators) adds to every RDMP assembly.  It lets <see cref="MEF"/> tell an RDMP
/// assembly which has not registered yet (because none of its code has run) from one built without the generator.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class CompiledTypeRegistryAttribute : Attribute
{
}
