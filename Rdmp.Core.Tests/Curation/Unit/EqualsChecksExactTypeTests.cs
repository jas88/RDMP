// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.Providers.Nodes;
using Rdmp.Core.Providers.Nodes.PipelineNodes;
using Tests.Common;

namespace Rdmp.Core.Tests.Curation.Unit;

/// <summary>
/// Tests that a <see cref="SingletonNode"/> is only equal to a node of exactly its own Type, so that a different kind
/// of node with the same caption is a different object.
/// </summary>
public class EqualsChecksExactTypeTests : UnitTests
{
    [Test]
    public void SingletonNodes_OfTheSameTypeAndCaption_AreEqual()
    {
        var allPipelines = new AllPipelinesNode();
        var allPipelinesAgain = new AllPipelinesNode();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(allPipelinesAgain.Equals(allPipelines), Is.True);
            Assert.That(allPipelinesAgain.GetHashCode(), Is.EqualTo(allPipelines.GetHashCode()));
            Assert.That(new ArbitraryFolderNode("Folder", 0).Equals(new ArbitraryFolderNode("Folder", 1)), Is.True);
            Assert.That(new ArbitraryFolderNode("Folder", 0).Equals(new ArbitraryFolderNode("Other", 0)), Is.False);
            Assert.That(allPipelines.Equals(null), Is.False);
            Assert.That(allPipelines.Equals((SingletonNode)null), Is.False);
        }
    }

    [Test]
    public void SingletonNodes_OfDifferentTypes_AreNotEqualEvenWithTheSameCaption()
    {
        var allPipelines = new AllPipelinesNode();
        var folderCalledPipelines = new ArbitraryFolderNode(allPipelines.ToString(), 0);
        var folder = new ArbitraryFolderNode("Datasets", 0);
        var extractionFolder = new ExtractionArbitraryFolderNode("Datasets", 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(folderCalledPipelines.Equals(allPipelines), Is.False);
            Assert.That(allPipelines.Equals(folderCalledPipelines), Is.False);
            Assert.That(extractionFolder.Equals(folder), Is.False);
            Assert.That(folder.Equals(extractionFolder), Is.False);
            Assert.That(new HashSet<object> { allPipelines, folderCalledPipelines, folder, extractionFolder },
                Has.Count.EqualTo(4));
        }
    }
}
