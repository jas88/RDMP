using System;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Python.Tests.Unit;

[Category("Integration")]
public class PythonNotInstalledTests
{

    [Test]
    [TestCase(PythonVersion.Version2)]
    [TestCase(PythonVersion.Version3)]
    public void PythonIsNotInstalled(PythonVersion version)
    {
        InconclusiveIfPythonIsInstalled(version);

        var provider = new PythonDataProvider
        {
            Version = version
        };

        var ex = Assert.Throws<Exception>(()=>provider.Check(ThrowImmediatelyCheckNotifier.Quiet));

        Assert.That(ex?.Message.Contains("Failed to launch"), Is.True);
    }

    private static void InconclusiveIfPythonIsInstalled(PythonVersion version)
    {
        var provider = new PythonDataProvider
        {
            Version = version
        };
        string result;
        try
        {
            //These tests run if python is not installed so we expect this to throw
            result = provider.GetPythonVersion();
        }
        catch
        {
            return;
        }
        //if it didn't throw then it means python IS installed and we cannot run these tests so Inconclusive
        Assert.Inconclusive($"Could not run tests because Python is already installed on the system, these unit tests only fire if there is no Python.  Python version string is:{result}");
    }
}