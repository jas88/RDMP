// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using CatalogueLibrary.Data;
using LoadModules.Extensions.ReleasePlugins.Data;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.ReleasePlugins.Automation
{
    /// <summary>
    /// Audit record tracking the processing status of files downloaded from WebDAV, including file location, result status, error messages, and creation/update timestamps.
    /// </summary>
    public class WebdavAutomationAudit : DatabaseEntity
    {
        #region Database Properties

        private string _fileHref;
        private FileResult _fileResult;
        private string _message;
        private DateTime _created;
        private DateTime _updated;
        #endregion

        public string FileHref
        {
            get { return _fileHref; }
            set { SetField(ref _fileHref, value); }
        }
        public FileResult FileResult
        {
            get { return _fileResult; }
            set { SetField(ref _fileResult, value); }
        }
        public string Message
        {
            get { return _message; }
            set { SetField(ref _message, value); }
        }
        public DateTime Created
        {
            get { return _created; }
            set { SetField(ref _created, value); }
        }
        public DateTime Updated
        {
            get { return _updated; }
            set { SetField(ref _updated, value); }
        }

        public WebdavAutomationAudit(WebDavDataRepository repository, string href, FileResult result, string message)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"FileHref", href},
                {"FileResult", result},
                {"Message", message},
                {"Created", DateTime.UtcNow},
                {"Updated", DateTime.UtcNow}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }

        public WebdavAutomationAudit(WebDavDataRepository repository, DbDataReader r)
            : base(repository, r)
        {
            FileHref = r["FileHref"].ToString();
            FileResult = (FileResult)Enum.Parse(typeof(FileResult), r["FileResult"].ToString());
            Message = r["Message"].ToString();
            Created = Convert.ToDateTime(r["created"]);
            Updated = Convert.ToDateTime(r["updated"]);
        }
    }
}