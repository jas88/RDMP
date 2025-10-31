// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using Rdmp.Core.DataFlowPipeline.Requirements;
using System.IO;
using System.Linq;
using Rdmp.Dicom.Extraction.FoDicomBased;

namespace Rdmp.Dicom.PipelineComponents.DicomSources.Worklists;

public class FlatFileToLoadDicomFileWorklist : IDicomFileWorklist
{
    private readonly FlatFileToLoad _file;

    private readonly string[] _lines;
    private int _linesCurrent;
    private bool _dataExhausted = false;

    public FlatFileToLoadDicomFileWorklist(FlatFileToLoad file)
    {
        _file = file;

        if(file.File is not { Extension: ".txt" })
            return;

        //input is a textual list of files/zips
        _lines = File.ReadAllLines(file.File.FullName).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        _linesCurrent = 0;

    }

    public bool GetNextFileOrDirectoryToProcess(out DirectoryInfo directory, out AmbiguousFilePath file)
    {
        file = null;
        directory = null;

        if (_dataExhausted)
            return false;

        //input is a single dicom file/zip
        if(_lines == null)
        {
            _dataExhausted = true;

            file = new AmbiguousFilePath(_file.File.FullName);
            return true;
        }

        //input was a text file full of other things to load
        if(_linesCurrent < _lines.Length)
        {
            var line = _lines[_linesCurrent];

            if (File.Exists(line.Trim()))
            {
                _linesCurrent++;
                file = new AmbiguousFilePath(new FileInfo(line.Trim()).FullName);
                return true;
            }

            if (Directory.Exists(line.Trim()))
            {
                _linesCurrent++;
                directory = new DirectoryInfo(line);
                return true;
            }

            if (!AmbiguousFilePath.IsZipReference(line))
                throw new Exception(
                    $"Text file '{_file.File.Name}' contained a line that was neither a File or a Directory:'{line}'");
            _linesCurrent++;
            file = new AmbiguousFilePath(line);
            return true;
        }

        _dataExhausted = true;
        return false;
    }
}