// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

namespace SCIStorePlugin
{
    /// <summary>
    /// Application settings for SCI Store Plugin
    /// </summary>
    internal sealed class Settings : System.Configuration.ApplicationSettingsBase
    {
        private static readonly Settings defaultInstance = ((Settings)Synchronized(new Settings()));

        public static Settings Default => defaultInstance;

        public string FriendlyName { get; set; }
        public string SystemCode { get; set; }
        public string SystemLocation { get; set; }
        public string UserName { get; set; }
    }
}