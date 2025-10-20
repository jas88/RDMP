// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace Rdmp.Core.Tests.DataLoad.Engine.Integration.PipelineTests.Sources;

[Category("Unit")]
public abstract class DelimitedFileSourceTestsBase
{
    protected static FlatFileToLoad CreateTestFile(params string[] contents)
    {
        // Use unique filename for each test to support parallel execution
        var uniqueId = Guid.NewGuid().ToString("N");
        var filename = Path.Combine(TestContext.CurrentContext.TestDirectory, $"DelimitedFileSourceTests_{uniqueId}.txt");

        if (File.Exists(filename))
            File.Delete(filename);

        File.WriteAllLines(filename, contents);

        return new FlatFileToLoad(new FileInfo(filename));
    }

    protected static void AssertDivertFileIsExactly(string expectedContents)
    {
        // The divert file will be named based on the source file with _Errors suffix
        // We need to find it by pattern since we don't know the exact GUID
        var directory = TestContext.CurrentContext.TestDirectory;
        var divertFiles = Directory.GetFiles(directory, "DelimitedFileSourceTests_*_Errors.txt");

        if (divertFiles.Length == 0)
            Assert.Fail($"No Divert file was generated in {directory}");

        // Get the most recently modified divert file (in case there are multiple)
        var filename = divertFiles.OrderByDescending(f => new FileInfo(f).LastWriteTime).First();

        var contents = File.ReadAllText(filename);
        Assert.That(contents, Is.EqualTo(expectedContents));
    }


    protected static DataTable RunGetChunk(FlatFileToLoad file, BadDataHandlingStrategy strategy, bool throwOnEmpty)
    {
        return RunGetChunk(file, s =>
        {
            s.BadDataHandlingStrategy = strategy;
            s.ThrowOnEmptyFiles = throwOnEmpty;
        });
    }

    protected static DataTable RunGetChunk(FlatFileToLoad file, Action<DelimitedFlatFileDataFlowSource> adjust = null)
    {
        var source = new DelimitedFlatFileDataFlowSource();
        source.PreInitialize(file, ThrowImmediatelyDataLoadEventListener.Quiet);
        source.Separator = ",";
        source.StronglyTypeInput = true; //makes the source interpret the file types properly
        source.StronglyTypeInputBatchSize = 100;
        source.AttemptToResolveNewLinesInRecords = true; //maximise potential for conflicts
        adjust?.Invoke(source);

        try
        {
            return source.GetChunk(ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());
        }
        finally
        {
            source.Dispose(ThrowImmediatelyDataLoadEventListener.Quiet, null);
        }
    }
}