// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

namespace Rdmp.Core.MapsDirectlyToDatabaseTable;

/// <summary>
/// Extension methods for IRepository to simplify common patterns, particularly around
/// FK constraint race condition prevention with AUTO_UPDATE_STATISTICS_ASYNC.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Saves the object to the database and immediately flushes visibility to ensure it's
    /// available for FK constraint checks. Use this when creating parent objects that will
    /// immediately have child objects created.
    ///
    /// <para>Example: Creating a TableInfo before ColumnInfo objects</para>
    /// </summary>
    /// <param name="saveable">The object to save and flush</param>
    public static void SaveAndFlush(this ISaveable saveable)
    {
        if (saveable is not IMapsDirectlyToDatabaseTable obj)
            return;

        saveable.SaveToDatabase();
        obj.Repository?.FlushVisibility(obj);
    }

    /// <summary>
    /// Flushes visibility for this object to ensure it's available for FK constraint checks.
    /// Use this after creating a parent object that will immediately have child objects created.
    ///
    /// <para>This is a convenience method that calls Repository.FlushVisibility()</para>
    /// </summary>
    /// <param name="obj">The object to flush</param>
    public static void FlushVisibility(this IMapsDirectlyToDatabaseTable obj)
    {
        obj.Repository?.FlushVisibility(obj);
    }

    /// <summary>
    /// Ensures that all parent objects referenced in the constructor parameters are
    /// flushed for visibility before proceeding. This prevents FK constraint race conditions
    /// when using AUTO_UPDATE_STATISTICS_ASYNC.
    ///
    /// <para>Call this in constructors that take parent objects as parameters.</para>
    /// </summary>
    /// <param name="repository">The repository</param>
    /// <param name="parents">Parent objects that should be flushed</param>
    public static void FlushParents(this IRepository repository, params IMapsDirectlyToDatabaseTable[] parents)
    {
        if (parents == null || repository == null)
            return;

        foreach (var parent in parents)
        {
            if (parent != null)
                repository.FlushVisibility(parent);
        }
    }
}
