using System;
using System.IO;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Python.Tests.Unit;

public class TestsThatWorkRegardless
{

    [Test]
    public void PythonVersionNotSetYet()
    {
        var provider = new PythonDataProvider();
        var ex = Assert.Throws<Exception>(()=>provider.Check(ThrowImmediatelyCheckNotifier.Quiet));
        Assert.That(ex?.Message, Is.EqualTo("Version of Python required for script has not been selected"));

    }


    [Test]
    public void PythonScript_OverrideExecutablePath_FileDoesntExist()
    {
        var MyPythonScript = @"s = raw_input ('==>')";

        var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");

        File.Delete(py);
        File.WriteAllText(py, MyPythonScript);

        var provider = new PythonDataProvider
        {
            Version = PythonVersion.Version2,
            FullPathToPythonScriptToRun = py,
            MaximumNumberOfSecondsToLetScriptRunFor = 5,
            OverridePythonExecutablePath = new FileInfo(@"C:\fishmongers\python")
        };
        //call with accept all
        var ex = Assert.Throws<Exception>(()=>provider.Check(new AcceptAllCheckNotifier()));

        Assert.That(ex?.Message, Does.Contain(@"The specified OverridePythonExecutablePath:C:\fishmongers\python does not exist"));

    }

}