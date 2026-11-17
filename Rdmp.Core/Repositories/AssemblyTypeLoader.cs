// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rdmp.Core.Repositories;

/// <summary>
/// Lists the Types in an assembly without losing them all when some cannot be loaded (e.g. because a dependency is
/// missing).  Used by <see cref="MEF"/> and <see cref="Rdmp.Core.Validation.Validator"/> to scan the loaded assemblies.
/// It is kept separate from <see cref="MEF"/> so that the Validator can use it without initialising <see cref="MEF"/>.
/// </summary>
internal static class AssemblyTypeLoader
{
    /// <summary>
    /// Gets types from an assembly safely, handling ReflectionTypeLoadException to preserve partially loaded types
    /// rather than discarding everything.  If the types cannot be listed at all, none are returned and the exception
    /// is recorded in <paramref name="failures"/> (if given) against the assembly's full name, unless the assembly is
    /// already recorded there.
    /// </summary>
    /// <param name="assembly">The assembly to list the types of</param>
    /// <param name="failures">Where to record assemblies whose types could not be listed (locked while being written
    /// to), or null to not record them</param>
    internal static IEnumerable<Type> GetTypesSafely(Assembly assembly, Dictionary<string, Exception> failures = null)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return the types that did load successfully (filtering out nulls)
            return ex.Types.Where(t => t != null);
        }
        catch (Exception e)
        {
            if (failures != null)
                lock (failures)
                {
                    failures.TryAdd(assembly.FullName, e);
                }

            return Array.Empty<Type>();
        }
    }
}
