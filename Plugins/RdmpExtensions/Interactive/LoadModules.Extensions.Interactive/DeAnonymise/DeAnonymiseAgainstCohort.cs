// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Data;
using System.Windows.Forms;
using FAnsi.Implementations.MicrosoftSQL;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.Interactive.DeAnonymise;

/// <summary>
/// Interactive pipeline component that de-anonymises data by allowing the user to select a cohort and reversing the anonymisation process to restore real identifiers from release identifiers.
/// </summary>
public class DeAnonymiseAgainstCohort: IPluginDataFlowComponent<DataTable>, IPipelineRequirement<IBasicActivateItems>
{
    /// <summary>
    /// When null (default) we launch a new DeAnonymiseAgainstCohortUI in order that the user selects which cohort he wants to deanonymise against. If you set this then you can
    /// (for example in unit tests) specify an explicit implementation and dodge the gui.
    /// </summary>
    public IDeAnonymiseAgainstCohortConfigurationFulfiller ConfigurationGetter;
    private IBasicActivateItems _activator;

    public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
    {
        if (toProcess == null)
            return null;

        Check(new FromDataLoadEventListenerToCheckNotifier(listener));

        if (ConfigurationGetter == null)
        {
            ConfigurationGetter = new DeAnonymiseAgainstCohortUI(toProcess,_activator);
            if(((Form)ConfigurationGetter).ShowDialog() != DialogResult.OK)
                throw new Exception("User cancelled cohort picking dialog");

            if (ConfigurationGetter.ChosenCohort == null)
                throw new Exception("User closed dialog without picking a cohort");
        }


        if (ConfigurationGetter.OverrideReleaseIdentifier != null)
        {
            var replacementName = MicrosoftQuerySyntaxHelper.Instance.GetRuntimeName(ConfigurationGetter.ChosenCohort.GetReleaseIdentifier());

            if (!toProcess.Columns.Contains(ConfigurationGetter.OverrideReleaseIdentifier))
                throw new ArgumentException(
                    $"Cannot DeAnonymise cohort because you specified OverrideReleaseIdentifier of '{ConfigurationGetter.OverrideReleaseIdentifier}' but the DataTable toProcess did not contain a column of that name");

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
                $"Renaming DataTable column {ConfigurationGetter.OverrideReleaseIdentifier} to {replacementName}"));
            toProcess.Columns[ConfigurationGetter.OverrideReleaseIdentifier].ColumnName = replacementName;
        }

        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to call ReverseAnonymiseDataTable"));
        ConfigurationGetter.ChosenCohort.ReverseAnonymiseDataTable(toProcess, listener, true);

        return toProcess;
    }


    public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
    {

    }

    public void Abort(IDataLoadEventListener listener)
    {

    }
    public void Check(ICheckNotifier notifier)
    {

    }

    public void PreInitialize(IBasicActivateItems value, IDataLoadEventListener listener)
    {
        _activator = value;
    }
}