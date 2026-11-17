// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using Rdmp.Core.ReusableLibraryCode.Licenses;
using Rdmp.Core.ReusableLibraryCode.Settings;

namespace Rdmp.Core.CommandLine;

/// <summary>
/// Makes sure that the user of the rdmp command line has accepted the license of RDMP (LICENSE) and the third party licenses
/// (LIBRARYLICENSES) before it does anything, as the Windows client does.  Accepting when asked is recorded in
/// <see cref="UserSettings.LicenseAccepted"/>, which the Windows client shares, as the hash of LIBRARYLICENSES, so a changed
/// file asks again.  Scripts and containers can instead accept the licenses for a single run with the --accept-licenses option
/// or the RDMP_ACCEPT_LICENSES environment variable, which are not recorded.  If the licenses have not been accepted and
/// input is redirected, the command line stops at once rather than wait for an answer.
/// </summary>
public sealed class LicenseAcceptance
{
    /// <summary>
    /// The long name of the option which accepts the licenses for a single run
    /// </summary>
    public const string AcceptOptionName = "accept-licenses";

    /// <summary>
    /// The environment variable which accepts the licenses for a single run when set to 1, true or yes
    /// </summary>
    public const string AcceptEnvironmentVariable = "RDMP_ACCEPT_LICENSES";

    /// <summary>
    /// The help text of the <see cref="AcceptOptionName"/> option
    /// </summary>
    public const string AcceptOptionHelpText =
        "Accept the RDMP licence (LICENSE) and the third party licences (LIBRARYLICENSES) for this run of the rdmp command line without being asked, e.g. in scripts.  Setting the environment variable " +
        AcceptEnvironmentVariable + "=1 does the same";

    /// <summary>
    /// The exit code of the rdmp command line when the licenses have not been accepted
    /// </summary>
    public const int NotAcceptedExitCode = 8;

    /// <summary>
    /// What to do about the licenses before the command line runs
    /// </summary>
    public enum Decision
    {
        /// <summary>
        /// The user has accepted these licenses before
        /// </summary>
        AlreadyAccepted,

        /// <summary>
        /// The option or the environment variable accepts the licenses for this run
        /// </summary>
        AcceptedForThisRun,

        /// <summary>
        /// The user must be asked to accept the licenses
        /// </summary>
        Ask,

        /// <summary>
        /// The user must be asked but input is redirected, so the command line cannot run
        /// </summary>
        CannotAsk
    }

    private readonly string _currentHash;
    private readonly Func<string> _getAcceptedHash;
    private readonly Action<string> _setAcceptedHash;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly bool _inputRedirected;
    private readonly string _licenseFolder;

    /// <summary>
    /// Checks acceptance of the licenses with the given state, input and output
    /// </summary>
    /// <param name="currentHash">The hash of the third party licenses which this program includes</param>
    /// <param name="getAcceptedHash">Returns the hash of the third party licenses which the user last accepted, or null</param>
    /// <param name="setAcceptedHash">Records that the user accepted the third party licenses with the given hash</param>
    /// <param name="input">Where the user's answer is read from</param>
    /// <param name="output">Where the question and any errors are written</param>
    /// <param name="inputRedirected">True if <paramref name="input"/> is not someone at a terminal, so the user cannot be asked</param>
    /// <param name="licenseFolder">The folder containing the license files that the user is pointed to</param>
    public LicenseAcceptance(string currentHash, Func<string> getAcceptedHash, Action<string> setAcceptedHash,
        TextReader input, TextWriter output, bool inputRedirected, string licenseFolder)
    {
        _currentHash = currentHash;
        _getAcceptedHash = getAcceptedHash;
        _setAcceptedHash = setAcceptedHash;
        _input = input;
        _output = output;
        _inputRedirected = inputRedirected;
        _licenseFolder = licenseFolder;
    }

    /// <summary>
    /// Checks acceptance of the licenses included in this program for the user of the console, recording it in
    /// <see cref="UserSettings.LicenseAccepted"/> and pointing the user to the license files next to the program
    /// </summary>
    /// <returns>An instance for the console</returns>
    public static LicenseAcceptance ForConsole() =>
        new(new License(License.ThirdPartyLicenses).GetHashOfLicense(),
            static () => UserSettings.LicenseAccepted,
            static hash => UserSettings.LicenseAccepted = hash,
            Console.In, Console.Error, Console.IsInputRedirected, AppContext.BaseDirectory);

    /// <summary>
    /// Returns true if <paramref name="arg"/> is the option which accepts the licenses for a single run
    /// </summary>
    /// <param name="arg">A command line argument</param>
    /// <returns>True for --accept-licenses in any case</returns>
    public static bool IsAcceptOption(string arg) =>
        string.Equals(arg, $"--{AcceptOptionName}", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if <paramref name="environmentValue"/>, the value of <see cref="AcceptEnvironmentVariable"/>, accepts the licenses
    /// </summary>
    /// <param name="environmentValue">The value of the environment variable, or null if it is not set</param>
    /// <returns>True for 1, true or yes in any case</returns>
    public static bool IsAcceptingValue(string environmentValue) =>
        environmentValue?.Trim().ToLowerInvariant() is "1" or "true" or "yes";

    /// <summary>
    /// Decides what to do about the licenses before the command line runs
    /// </summary>
    /// <param name="currentHash">The hash of the third party licenses which this program includes</param>
    /// <param name="acceptedHash">The hash of the third party licenses which the user last accepted, or null</param>
    /// <param name="acceptOption">True if the command line has the --accept-licenses option</param>
    /// <param name="environmentValue">The value of <see cref="AcceptEnvironmentVariable"/>, or null if it is not set</param>
    /// <param name="inputRedirected">True if input is redirected, so the user cannot be asked</param>
    /// <returns>What to do</returns>
    public static Decision Decide(string currentHash, string acceptedHash, bool acceptOption, string environmentValue,
        bool inputRedirected)
    {
        if (acceptedHash != null && string.Equals(acceptedHash, currentHash, StringComparison.Ordinal))
            return Decision.AlreadyAccepted;

        if (acceptOption || IsAcceptingValue(environmentValue))
            return Decision.AcceptedForThisRun;

        return inputRedirected ? Decision.CannotAsk : Decision.Ask;
    }

    /// <summary>
    /// Makes sure that the licenses have been accepted, asking the user to accept them if need be
    /// </summary>
    /// <param name="args">The command line arguments, which may include the --accept-licenses option</param>
    /// <param name="environmentValue">The value of <see cref="AcceptEnvironmentVariable"/>, or null if it is not set</param>
    /// <returns>True if the command line may run</returns>
    public bool EnsureAccepted(string[] args, string environmentValue)
    {
        var acceptedHash = GetAcceptedHash();
        var changed = acceptedHash != null;

        switch (Decide(_currentHash, acceptedHash, args.Any(IsAcceptOption), environmentValue, _inputRedirected))
        {
            case Decision.AlreadyAccepted:
            case Decision.AcceptedForThisRun:
                return true;
            case Decision.CannotAsk:
                if (changed)
                    _output.WriteLine("The third party licences have changed since you accepted them.");
                _output.WriteLine(
                    $"RDMP cannot run until you accept its licence and the third party licences, which are in {_licenseFolder}, and it cannot ask you to because input is redirected.  " +
                    $"Run rdmp once without redirecting input to accept them, or accept them for this run with --{AcceptOptionName} or by setting the environment variable {AcceptEnvironmentVariable}=1.");
                return false;
            default:
                return Ask(changed);
        }
    }

    private string GetAcceptedHash()
    {
        try
        {
            return _getAcceptedHash();
        }
        catch (Exception)
        {
            // Settings which cannot be read have not recorded an acceptance
            return null;
        }
    }

    private bool Ask(bool changed)
    {
        _output.WriteLine(changed
            ? "The third party licences have changed since you accepted them.  Before you use RDMP you must accept them again, along with its licence:"
            : "Before you use RDMP you must accept its licence and the licences of the third party software it includes, some of which require your agreement:");

        DescribeLicense(License.RdmpLicense, "The RDMP licence (GNU GPL version 3 or later)");
        DescribeLicense(License.ThirdPartyLicenses, "The third party licences");

        _output.Write("Do you accept these licences? Enter y to accept or n to decline: ");
        _output.Flush();

        var answer = _input.ReadLine()?.Trim();

        if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine("You have not accepted the licences, so RDMP will not run.");
            return false;
        }

        try
        {
            _setAcceptedHash(_currentHash);
        }
        catch (Exception e)
        {
            _output.WriteLine($"Could not record that you accepted the licences, so you will be asked again next time: {e.Message}");
        }

        return true;
    }

    private void DescribeLicense(string fileName, string description)
    {
        var path = Path.Combine(_licenseFolder, fileName);

        if (File.Exists(path))
        {
            _output.WriteLine($"  {description}: {path}");
            return;
        }

        // The file was not shipped next to this program, so show the copy embedded in it
        _output.WriteLine($"  {description}:");
        _output.WriteLine(new License(fileName).GetLicenseText());
    }
}
