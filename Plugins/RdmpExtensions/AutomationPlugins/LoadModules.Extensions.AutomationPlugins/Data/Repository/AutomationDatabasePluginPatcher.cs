// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System.Linq;
using System.Reflection;
using CatalogueLibrary.Data;
using CatalogueLibrary.ExternalDatabaseServerPatching;
using CatalogueLibrary.Repositories;

namespace LoadModules.Extensions.AutomationPlugins.Data.Repository
{
    public class AutomationDatabasePluginPatcher:IPluginPatcher
    {
        private CatalogueRepository _repository;

        public AutomationDatabasePluginPatcher(CatalogueRepository repository)
        {
            _repository = repository;
        }

        public IExternalDatabaseServer[] FindDatabases(out Assembly hostAssembly, out Assembly dbAssembly)
        {
            hostAssembly = GetType().Assembly;
            dbAssembly = typeof (Database.Class1).Assembly;

            var dbAssemblyName = dbAssembly.GetName().Name;

            return 
                _repository.GetAllObjects<ExternalDatabaseServer>()
                .Where(s => s.CreatedByAssembly != null && s.CreatedByAssembly.Equals(dbAssemblyName))
                .ToArray();
        }

        public Assembly GetHostAssembly()
        {
            throw new System.NotImplementedException();
        }

        public Assembly GetDbAssembly()
        {
            throw new System.NotImplementedException();
        }
    }
}
