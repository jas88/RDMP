// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.CommandLine.Interactive;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.ReusableLibraryCode.Checks;
using Tests.Common;

namespace Rdmp.Core.Tests.CommandExecution;

internal class ExecuteCommandSendExtractionResolutionTeamsNotificationTests : UnitTests
{
    private const string Webhook = "https://teams.example.invalid/webhook";

    /// <summary>
    /// Stands in for the network.  It answers after a pause and only then reads the request, so a command which does
    /// not wait for the request, or which disposes the request content early, is caught.
    /// </summary>
    private sealed class PausingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly Exception _failure;

        public PausingHandler(HttpStatusCode status = HttpStatusCode.OK, Exception failure = null)
        {
            _status = status;
            _failure = failure;
        }

        public bool Answered { get; private set; }
        public HttpMethod Method { get; private set; }
        public Uri RequestUri { get; private set; }
        public string ContentType { get; private set; }
        public string Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);

            Method = request.Method;
            RequestUri = request.RequestUri;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (_failure != null)
                throw _failure;

            Answered = true;
            return new HttpResponseMessage(_status);
        }
    }

    private ExtractionConfiguration _configuration;
    private ToMemoryCheckNotifier _globalErrors;

    [SetUp]
    public void CreateConfiguration()
    {
        _configuration = WhenIHaveA<ExtractionConfiguration>();
        _configuration.Name = "My Extraction";
        _globalErrors = new ToMemoryCheckNotifier();
    }

    private ExecuteCommandSendExtractionResolutionTeamsNotification CreateCommand(HttpMessageInvoker client,
        bool success = true, string url = Webhook, bool withActivator = true) =>
        new(withActivator ? new ConsoleInputManager(RepositoryLocator, _globalErrors) : null, _configuration, success)
        {
            Client = client,
            Url = url
        };

    [TestCase(true, "completed successfully")]
    [TestCase(false, "failed")]
    public void Execute_WaitsUntilTheCardHasBeenSent(bool success, string expectedOutcome)
    {
        var handler = new PausingHandler();
        using var client = new HttpClient(handler);

        CreateCommand(client, success).Execute();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Answered, "Execute returned before the request had been answered");
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUri, Is.EqualTo(new Uri(Webhook)));
            Assert.That(handler.ContentType, Is.EqualTo("application/json"));
            Assert.That(handler.Body, Does.Contain($"Extraction My Extraction: {expectedOutcome}."));
            Assert.That(_globalErrors.Messages, Is.Empty);
        });
    }

    [Test]
    public void Execute_ErrorResponse_IsReportedNotThrown()
    {
        using var client = new HttpClient(new PausingHandler(HttpStatusCode.BadRequest));

        Assert.That(CreateCommand(client).Execute, Throws.Nothing);

        AssertOneWarningAbout<HttpRequestException>();
    }

    [Test]
    public void Execute_NoConnection_IsReportedNotThrown()
    {
        using var client = new HttpClient(new PausingHandler(failure: new HttpRequestException("No such host is known")));

        Assert.That(CreateCommand(client).Execute, Throws.Nothing);

        AssertOneWarningAbout<HttpRequestException>();
    }

    [Test]
    public void Execute_NoAnswerInTime_IsReportedNotThrown()
    {
        // far shorter than the handler's pause
        using var client = new HttpClient(new PausingHandler()) { Timeout = TimeSpan.FromMilliseconds(20) };

        Assert.That(CreateCommand(client).Execute, Throws.Nothing);

        AssertOneWarningAbout<OperationCanceledException>();
    }

    [TestCase("not a url")]
    [TestCase(null)]
    public void Execute_UnusableUrl_IsReportedNotThrown(string url)
    {
        var handler = new PausingHandler();
        using var client = new HttpClient(handler);

        Assert.That(CreateCommand(client, url: url).Execute, Throws.Nothing);

        Assert.That(handler.Answered, Is.False, "Nothing should have been sent");
        AssertOneWarningAbout<InvalidOperationException>();
    }

    [Test]
    public void Execute_WithNowhereToReportTo_ThrowsTheFailure()
    {
        using var client = new HttpClient(new PausingHandler(HttpStatusCode.InternalServerError));

        Assert.That(CreateCommand(client, withActivator: false).Execute, Throws.InstanceOf<HttpRequestException>());
    }

    private void AssertOneWarningAbout<TException>() where TException : Exception
    {
        Assert.That(_globalErrors.Messages, Has.Count.EqualTo(1));

        var report = _globalErrors.Messages.Single();
        Assert.Multiple(() =>
        {
            Assert.That(report.Result, Is.EqualTo(CheckResult.Warning));
            Assert.That(report.Message, Does.Contain("My Extraction"));
            Assert.That(report.Ex, Is.InstanceOf<TException>());
        });
    }
}
