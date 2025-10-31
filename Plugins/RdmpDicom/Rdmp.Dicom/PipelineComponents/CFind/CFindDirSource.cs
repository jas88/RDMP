// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Xml;
using FAnsi.Discovery;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Rdmp.Dicom.PipelineComponents.CFind;

/// <summary>
/// Source that reads an 'inventory file' which contains one or more directories. Directories
/// should contain CFind xml formatted results e.g. as produced by dicom toolkit
/// </summary>
public class CFindDirSource : IPluginDataFlowSource<DataTable>, IPipelineRequirement<FlatFileToLoad>
{
    private FlatFileToLoad _file;

    const string DefaultHeaders = "RetrieveAETitle,ModalitiesInStudy,StudyDescription,PatientID,TypeOfPatientID,StudyInstanceUID,StudyDate";


    [DemandsInitialization("Search pattern for locating CFind results in directories found", Mandatory = true, DefaultValue = "*.xml")]
    public string SearchPattern { get; set; } = "*.xml";


    [DemandsInitialization("Comma seperated list of dicom tags to read from the CFind results", Mandatory = true, DefaultValue = DefaultHeaders)]
    public string HeadersToRead { get; set; } = DefaultHeaders;

    int filesRead = 0;

    Stopwatch timer;

    public void Abort(IDataLoadEventListener listener)
    {

    }

    public void Check(ICheckNotifier notifier)
    {
        var dt = GenerateTable();

        notifier.OnCheckPerformed(dt.Columns.Count <= 0
            ? new CheckEventArgs($"Failed to build table.  Check {nameof(HeadersToRead)}", CheckResult.Fail)
            : new CheckEventArgs("Built table successfully", CheckResult.Success));
    }

    public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {

    }

    bool firstTime = true;

    public DataTable GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        if (_file == null)
            throw new Exception("File has not been set");
        if (!_file.File.Exists)
            throw new FileNotFoundException($"File did not exist:'{_file.File.FullName}'");

        // This is an all at once source, next call returns null (i.e. we are done)
        if (!firstTime)
        {
            return null;
        }

        timer = Stopwatch.StartNew();


        var dt = GenerateTable();

        foreach(var f in File.ReadAllLines(_file.File.FullName))
        {
            if (string.IsNullOrWhiteSpace(f))
                continue;

            ProcessDir(f, dt,listener);
        }

        firstTime = false;
        return dt;
    }

    private DataTable GenerateTable()
    {
        var dt = new DataTable();

        if (_file != null)
        {
            dt.TableName = QuerySyntaxHelper.MakeHeaderNameSensible(_file.File.Name);
        }


        foreach (var h in HeadersToRead.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            dt.Columns.Add(h);
        }

        return dt;
    }

    private void ProcessDir(string dir, DataTable dt, IDataLoadEventListener listener)
    {
        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"Starting '{dir}'"));

        if (File.Exists(dir))
        {
            // the inventory entry is a xml file directly :o
            XmlToRows(dir, dt);
            return;
        }

        if (!Directory.Exists(dir))
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, $"'{dir}' was not a Directory or File"));
            return;
        }

        var matches = Directory.GetFiles(dir, SearchPattern, SearchOption.AllDirectories);

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, $"Found {matches.Length} CFind files in {dir}"));

        foreach(var file in matches)
        {
            XmlToRows(file, dt);

            if (filesRead++ % 10000 == 0)
            {
                listener.OnProgress(this, new ProgressEventArgs("Reading files", new ProgressMeasurement(filesRead, ProgressType.Records, matches.Length), timer?.Elapsed ?? TimeSpan.Zero));
            }
        }
    }

    private void XmlToRows(string file, DataTable dt)
    {
        using var fileStream = File.Open(file, FileMode.Open);
        //Load the file and create a navigator object.
        var xDoc = new XmlDocument();
        xDoc.Load(fileStream);

        var datasets = xDoc.GetElementsByTagName("data-set");

        foreach(XmlElement d in datasets)
        {
            var row = dt.NewRow();

            foreach(XmlElement child in d.ChildNodes)
            {
                var name = child.GetAttribute("name");
                if(dt.Columns.Contains(name))
                {
                    row[name] = child.InnerText;
                }
            }

            dt.Rows.Add(row);
        }
    }

    public void PreInitialize(FlatFileToLoad value, IDataLoadEventListener listener)
    {
        _file = value;
    }

    public DataTable TryGetPreview()
    {
        return GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
    }
}