// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using NUnit.Framework;
using Rdmp.Dicom.Extraction.FoDicomBased;
using Rdmp.Dicom.Extraction.FoDicomBased.DirectoryDecisions;
using Rdmp.Core.ReusableLibraryCode.Progress;
using System.IO;
using System.Linq;

namespace Rdmp.Dicom.Tests.Integration;

public class FoDicomAnonymiserStandaloneTests
{
    [Test]
    public void TestAnonymiseAFile()
    {
        var anon = new FoDicomAnonymiser();

        var inPath = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "in"));
        var outPath = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "out"));

        if (inPath.Exists)
            inPath.Delete(true);
        inPath.Create();

        if (outPath.Exists)
            outPath.Delete(true);
        outPath.Create();

        // put a dicom file in the in dir
        var testFile = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestData/IM-0001-0013.dcm"));
        testFile.CopyTo(Path.Combine(inPath.FullName, "blah.dcm"), true);

        anon.Initialize(1, outPath, null /*no UID mapping*/);

        var putter = new PutInRoot();

        anon.ProcessFile(
            new AmbiguousFilePath(inPath.FullName, "blah.dcm").GetDataset().Single().Item2,
            ThrowImmediatelyDataLoadEventListener.Quiet,
            "fffff",
            putter, null);
    }

}