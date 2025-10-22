using System;
using System.Data;
using LoadModules.Extensions.Interactive.DeAnonymise;
using NUnit.Framework;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace LoadModules.Extensions.Interactive.Tests;

public class DeAnonymiseAgainstCohortTests:TestsRequiringACohort,IDeAnonymiseAgainstCohortConfigurationFulfiller
{
    private DeAnonymiseAgainstCohort _deAnonymiseAgainstCohort;
    public IExtractableCohort ChosenCohort { get; set; }
    public string OverrideReleaseIdentifier { get; set; }

    [SetUp]
    public void setup()
    {
        _deAnonymiseAgainstCohort = new DeAnonymiseAgainstCohort();

        ChosenCohort = _extractableCohort;
        _deAnonymiseAgainstCohort.ConfigurationGetter = this;//we force it to use this one (otherwise it would launch a Windows Form)

    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Normal_ReleaseDeAnonToPrivateKeys(bool doRedundantOverride)
    {
        using var dt = new DataTable();
        dt.BeginLoadData();
        dt.Columns.Add("ReleaseID");
        dt.Columns.Add("Animal");

        foreach (var (_, value) in _cohortKeysGenerated)
            dt.Rows.Add(value, "fish");

        if (doRedundantOverride)
            OverrideReleaseIdentifier = "ReleaseID";

        dt.EndLoadData();
        using var clone = dt.Copy();  // Grab a copy of the pre-pipeline data to compare
        var result = _deAnonymiseAgainstCohort.ProcessPipelineData(dt, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

        Assert.That(result.Columns.Contains("PrivateID"), Is.True);

        for(var i=0;i<result.Rows.Count;i++)
        {
            Assert.That(
                clone.Rows[i]["ReleaseID"], Is.EqualTo(_cohortKeysGenerated[(string)result.Rows[i]["PrivateID"]]));
        }

        OverrideReleaseIdentifier = null;
    }

    [Test]
    public void Freaky_ColumnNameOverriding()
    {
        using var dt = new DataTable();
        dt.Columns.Add("HappyFunTimes");
        dt.Columns.Add("Animal");

        foreach (var (_, value) in _cohortKeysGenerated)
            dt.Rows.Add(value, "fish");


        OverrideReleaseIdentifier = "HappyFunTimes";
        try
        {
            using var clone = dt.Copy();  // Grab a copy of the pre-pipeline data to compare
            using var result = _deAnonymiseAgainstCohort.ProcessPipelineData(dt, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken());

            Assert.That(result.Columns.Contains("PrivateID"), Is.True);

            for (var i = 0; i < result.Rows.Count; i++)
            {
                Assert.That(
                    clone.Rows[i]["HappyFunTimes"], Is.EqualTo(_cohortKeysGenerated[(string)result.Rows[i]["PrivateID"]]));
            }
        }
        finally
        {
            OverrideReleaseIdentifier = null;
        }
    }

    [Test]
    public void Throws_ColumnMissing()
    {
        using var dt = new DataTable();
        dt.Columns.Add("Animal");

        foreach (var (key, value) in _cohortKeysGenerated)
            dt.Rows.Add("fish");

        var ex = Assert.Throws<ArgumentException>(() => _deAnonymiseAgainstCohort.ProcessPipelineData(dt, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken()));

        Assert.That(ex?.Message.StartsWith("Column 'ReleaseID' does not belong to table"), Is.True, $"Exception text was '{ex?.Message}'");
    }

    [Test]
    public void Throws_ColumnMissingWithOverride()
    {
        using var dt = new DataTable();
        dt.Columns.Add("Animal");

        foreach (var (key, value) in _cohortKeysGenerated)
            dt.Rows.Add("fish");

        OverrideReleaseIdentifier = "HappyFace";

        var ex = Assert.Throws<ArgumentException>(() => _deAnonymiseAgainstCohort.ProcessPipelineData(dt, ThrowImmediatelyDataLoadEventListener.Quiet, new GracefulCancellationToken()));
        Assert.That(ex?.Message, Is.EqualTo("Cannot DeAnonymise cohort because you specified OverrideReleaseIdentifier of 'HappyFace' but the DataTable toProcess did not contain a column of that name"));
    }

}