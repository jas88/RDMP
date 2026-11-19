// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Rdmp.Core.CommandLine;
using Rdmp.Core.ReusableLibraryCode.Licenses;
using Decision = Rdmp.Core.CommandLine.LicenseAcceptance.Decision;

namespace Rdmp.Core.Tests.CommandLine;

/// <summary>
/// Tests when the rdmp command line asks the user to accept the licenses, runs, or stops
/// </summary>
public class LicenseAcceptanceTests
{
    private const string CurrentHash = "AA-BB-CC";
    private const string OldHash = "11-22-33";

    private string _licenseFolder;
    private string _storedHash;
    private List<string> _savedHashes;
    private StringWriter _output;

    [SetUp]
    public void SetUp()
    {
        _licenseFolder = Path.Join(TestContext.CurrentContext.WorkDirectory,
            $"{nameof(LicenseAcceptanceTests)}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_licenseFolder);
        File.WriteAllText(Path.Join(_licenseFolder, License.RdmpLicense), "rdmp license");
        File.WriteAllText(Path.Join(_licenseFolder, License.ThirdPartyLicenses), "third party licenses");

        _storedHash = null;
        _savedHashes = new List<string>();
        _output = new StringWriter();
    }

    [TearDown]
    public void TearDown()
    {
        _output.Dispose();
        Directory.Delete(_licenseFolder, true);
    }

    private LicenseAcceptance Create(TextReader input, bool inputRedirected, Action<string> save = null) =>
        new(CurrentHash, () => _storedHash, save ?? (hash =>
            {
                _savedHashes.Add(hash);
                _storedHash = hash;
            }),
            input, _output, inputRedirected, _licenseFolder);

    [TestCase(false)]
    [TestCase(true)]
    public void Decide_AcceptedBefore_DoesNotAsk(bool inputRedirected)
    {
        Assert.That(LicenseAcceptance.Decide(CurrentHash, CurrentHash, false, null, inputRedirected),
            Is.EqualTo(Decision.AlreadyAccepted));
    }

    [TestCase(null, false, Decision.Ask)]
    [TestCase(null, true, Decision.CannotAsk)]
    [TestCase(OldHash, false, Decision.Ask)]
    [TestCase(OldHash, true, Decision.CannotAsk)]
    public void Decide_NotAcceptedOrChanged_AsksIfItCan(string acceptedHash, bool inputRedirected, Decision expected)
    {
        Assert.That(LicenseAcceptance.Decide(CurrentHash, acceptedHash, false, null, inputRedirected),
            Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase(OldHash)]
    public void Decide_Option_AcceptsForThisRun(string acceptedHash)
    {
        Assert.That(LicenseAcceptance.Decide(CurrentHash, acceptedHash, true, null, true),
            Is.EqualTo(Decision.AcceptedForThisRun));
    }

    [TestCase("1")]
    [TestCase("true")]
    [TestCase("TRUE")]
    [TestCase("yes")]
    [TestCase(" 1 ")]
    public void Decide_EnvironmentVariable_AcceptsForThisRun(string environmentValue)
    {
        Assert.That(LicenseAcceptance.Decide(CurrentHash, OldHash, false, environmentValue, true),
            Is.EqualTo(Decision.AcceptedForThisRun));
    }

    [TestCase("")]
    [TestCase("0")]
    [TestCase("false")]
    [TestCase("no")]
    [TestCase("y")]
    public void Decide_EnvironmentVariableNotAccepting_CannotAskWhenRedirected(string environmentValue)
    {
        Assert.That(LicenseAcceptance.Decide(CurrentHash, null, false, environmentValue, true),
            Is.EqualTo(Decision.CannotAsk));
    }

    [TestCase("--accept-licenses", true)]
    [TestCase("--ACCEPT-LICENSES", true)]
    [TestCase("accept-licenses", false)]
    [TestCase("-accept-licenses", false)]
    [TestCase("--accept-license", false)]
    [TestCase(null, false)]
    public void IsAcceptOption(string arg, bool expected)
    {
        Assert.That(LicenseAcceptance.IsAcceptOption(arg), Is.EqualTo(expected));
    }

    [Test]
    public void EnsureAccepted_AcceptedBefore_RunsWithoutAsking()
    {
        _storedHash = CurrentHash;

        Assert.Multiple(() =>
        {
            Assert.That(Create(new UnreadableReader(), false).EnsureAccepted(Array.Empty<string>(), null), Is.True);
            Assert.That(_output.ToString(), Is.Empty);
            Assert.That(_savedHashes, Is.Empty);
        });
    }

    [TestCase("y")]
    [TestCase("yes")]
    [TestCase(" YES ")]
    public void EnsureAccepted_UserAccepts_RecordsAcceptance(string answer)
    {
        var accepted = Create(new StringReader($"{answer}{Environment.NewLine}"), false)
            .EnsureAccepted(Array.Empty<string>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(_savedHashes, Is.EqualTo(new[] { CurrentHash }));
            Assert.That(_output.ToString(), Does.Contain(Path.Join(_licenseFolder, License.RdmpLicense)));
            Assert.That(_output.ToString(), Does.Contain(Path.Join(_licenseFolder, License.ThirdPartyLicenses)));
        });

        // So the next run does not ask
        Assert.That(LicenseAcceptance.Decide(CurrentHash, _storedHash, false, null, true),
            Is.EqualTo(Decision.AlreadyAccepted));
    }

    [TestCase("n")]
    [TestCase("no")]
    [TestCase("")]
    [TestCase("maybe")]
    public void EnsureAccepted_UserDeclines_DoesNotRun(string answer)
    {
        var accepted = Create(new StringReader($"{answer}{Environment.NewLine}"), false)
            .EnsureAccepted(Array.Empty<string>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.False);
            Assert.That(_savedHashes, Is.Empty);
            Assert.That(_output.ToString(), Does.Contain("You have not accepted the licences"));
        });
    }

    [Test]
    public void EnsureAccepted_EndOfInput_DoesNotRun()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Create(new StringReader(""), false).EnsureAccepted(Array.Empty<string>(), null), Is.False);
            Assert.That(_savedHashes, Is.Empty);
        });
    }

    [Test]
    public void EnsureAccepted_LicensesChanged_AsksAgain()
    {
        _storedHash = OldHash;

        var accepted = Create(new StringReader($"y{Environment.NewLine}"), false)
            .EnsureAccepted(Array.Empty<string>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(_output.ToString(), Does.Contain("have changed since you accepted them"));
            Assert.That(_savedHashes, Is.EqualTo(new[] { CurrentHash }));
        });
    }

    [TestCase(null)]
    [TestCase(OldHash)]
    public void EnsureAccepted_InputRedirected_StopsWithoutReadingInput(string storedHash)
    {
        _storedHash = storedHash;

        var accepted = Create(new UnreadableReader(), true).EnsureAccepted(Array.Empty<string>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.False);
            Assert.That(_savedHashes, Is.Empty);
            Assert.That(_output.ToString(), Does.Contain("--accept-licenses"));
            Assert.That(_output.ToString(), Does.Contain($"{LicenseAcceptance.AcceptEnvironmentVariable}=1"));
        });
    }

    [Test]
    public void EnsureAccepted_Option_RunsWithoutAskingOrRecording()
    {
        var accepted = Create(new UnreadableReader(), true)
            .EnsureAccepted(new[] { "cmd", "ListSupportedCommands", "--accept-licenses" }, null);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(_savedHashes, Is.Empty);
            Assert.That(_output.ToString(), Is.Empty);
        });
    }

    [Test]
    public void EnsureAccepted_EnvironmentVariable_RunsWithoutAskingOrRecording()
    {
        var accepted = Create(new UnreadableReader(), true).EnsureAccepted(new[] { "cmd" }, "1");

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(_savedHashes, Is.Empty);
            Assert.That(_output.ToString(), Is.Empty);
        });
    }

    [Test]
    public void EnsureAccepted_RecordingFails_StillRuns()
    {
        var accepted = Create(new StringReader($"y{Environment.NewLine}"), false,
                static _ => throw new IOException("disk full"))
            .EnsureAccepted(Array.Empty<string>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(_output.ToString(), Does.Contain("you will be asked again next time"));
        });
    }

    [Test]
    public void EnsureAccepted_ReadingSettingsFails_Asks()
    {
        var acceptance = new LicenseAcceptance(CurrentHash, static () => throw new IOException("unreadable"),
            _savedHashes.Add, new StringReader($"y{Environment.NewLine}"), _output, false, _licenseFolder);

        Assert.Multiple(() =>
        {
            Assert.That(acceptance.EnsureAccepted(Array.Empty<string>(), null), Is.True);
            Assert.That(_savedHashes, Is.EqualTo(new[] { CurrentHash }));
        });
    }

    [Test]
    public void EnsureAccepted_LicenseFilesMissing_ShowsTheEmbeddedLicenses()
    {
        File.Delete(Path.Join(_licenseFolder, License.RdmpLicense));
        File.Delete(Path.Join(_licenseFolder, License.ThirdPartyLicenses));

        Create(new StringReader($"n{Environment.NewLine}"), false).EnsureAccepted(Array.Empty<string>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(_output.ToString(), Does.Contain(new License(License.RdmpLicense).GetLicenseText()));
            Assert.That(_output.ToString(), Does.Contain(new License(License.ThirdPartyLicenses).GetLicenseText()));
        });
    }

    /// <summary>
    /// Input which fails the test if anything reads it
    /// </summary>
    private sealed class UnreadableReader : TextReader
    {
        public override int Peek() => throw new AssertionException("Input was read");
        public override int Read() => throw new AssertionException("Input was read");
        public override string ReadLine() => throw new AssertionException("Input was read");
    }
}
