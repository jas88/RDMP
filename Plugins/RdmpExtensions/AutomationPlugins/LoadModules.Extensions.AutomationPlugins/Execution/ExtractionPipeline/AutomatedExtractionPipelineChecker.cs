// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.ReusableLibraryCode.Checks;
using System;
using System.Linq;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;

public class AutomatedExtractionPipelineChecker:ICheckable
{
    private readonly Pipeline _automateExtractionPipeline;

    public AutomatedExtractionPipelineChecker(Pipeline automateExtractionPipeline)
    {
        _automateExtractionPipeline = automateExtractionPipeline;
    }

    public void Check(ICheckNotifier notifier)
    {
        try
        {
            if (_automateExtractionPipeline == null)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("No Pipeline specified", CheckResult.Fail));
                return;
            }

            if (_automateExtractionPipeline.PipelineComponents.Any(c => c.Class == typeof (SuccessfullyExtractedResultsDocumenter).FullName))
                notifier.OnCheckPerformed(new CheckEventArgs("Found SuccessfullyExtractedResultsDocumenter plugin component",CheckResult.Success));
            else
                notifier.OnCheckPerformed(new CheckEventArgs(
                    $"Automated Extraction can only take place through Pipelines that include a {nameof(SuccessfullyExtractedResultsDocumenter)} plugin component", CheckResult.Fail));

            var source = _automateExtractionPipeline.Source;

            if (source == null)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("No Source Pipeline Component Declared",CheckResult.Fail));
                return;
            }

            if (source.Class == typeof (BaselineHackerExecuteDatasetExtractionSource).FullName)
                notifier.OnCheckPerformed(new CheckEventArgs($"Found Compatible Source {source.Class}",
                    CheckResult.Success));
            else
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        $"Source Component {source.Class} of Pipeline {_automateExtractionPipeline} is not a {typeof(BaselineHackerExecuteDatasetExtractionSource).FullName} (Deltas will never be created)", CheckResult.Warning));
        }
        catch (Exception e)
        {
            notifier.OnCheckPerformed(new CheckEventArgs("Checking process failed", CheckResult.Fail, e));
        }
    }
}