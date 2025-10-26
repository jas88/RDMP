// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Dicom.PACS;

namespace Rdmp.Dicom.Cache.Pipeline.Dicom;

public class DicomRequestSender : IDicomRequestSender
{
    public int SendTimeout { get; private set; }

    private readonly DicomConfiguration _dicomConfiguration;
    private readonly IDataLoadEventListener _listener;
    private readonly bool verbose;
    private readonly Stopwatch _moveRequestTimer = new();
    public delegate void OnCheckExceptionDelegate(Exception ex);
    public delegate void OnCheckTimeoutDelegate();
    public delegate void OnCheckSucessDelegate();

    public OnCheckExceptionDelegate OnRequestException;
    public OnCheckTimeoutDelegate OnRequestTimeout;
    public OnCheckSucessDelegate OnRequestSucess;

    public DicomRequestSender(DicomConfiguration dicomConfiguration, IDataLoadEventListener listener,bool verbose)
    {
        _dicomConfiguration = dicomConfiguration;
        _listener = listener;
        this.verbose = verbose;
    }

    /// <summary>
    /// Check
    /// </summary>
    #region Check
    public void Check()
    {
        var echoRequest = new DicomCEchoRequest();
        SendRequest(echoRequest, new CancellationToken(false));
    }
    #endregion


    /// <summary>
    ///    Throttle requests using W(O) = mO(t) + c where W is the wait period, O is the opertaion duration, m and c are positive constants
    ///    The request is added to the client which is unreleased at the end of this request send.
    /// </summary>
    /// 
    #region ThrottleRequest
    public void ThrottleRequest(DicomRequest dicomRequest, IDicomClient client, CancellationToken cancellationToken)
    {
        client.AddRequestAsync(dicomRequest).Wait(cancellationToken);
        ThrottleRequest(client, cancellationToken);
    }
    #endregion

    /// <summary>
    ///    Throttle requests using W(O) = mO(t) + c where W is the wait period, O is the opertaion duration, m and c are positive constants
    ///    Sends requests added to the client is unreleased at the end of this request send.
    /// </summary>
    /// 
    #region ThrottleRequest
    public void ThrottleRequest(IDicomClient client, CancellationToken cancellationToken)
    {
        var transferTimer = new Stopwatch();
        transferTimer.Start();
        SendRequest(client,cancellationToken);
        transferTimer.Stop();
        // value in mills
        var delay =  _dicomConfiguration.RequestCooldownInMilliseconds;
        if (delay <= 0) return;
        _listener.OnNotify(this, new NotifyEventArgs(
            verbose ? ProgressEventType.Information : ProgressEventType.Trace,
            $"Requests sleeping for {delay / 1000}seconds"));
        Task.Delay(delay, cancellationToken).Wait(cancellationToken);
    }
    #endregion


    /// <summary>
    ///     Blocks until the request is received so calling code doesn't have to deal with asynchrony (see the EventWaitHandle in TrySend).
    ///     Only the timeout is applied no Throtelling
    /// </summary>
    /// <param name="dicomRequest"></param>
    /// <param name="token"></param>

    #region SendRequest
    private void SendRequest(DicomRequest dicomRequest, CancellationToken token)
    {
        var client = DicomClientFactory.Create(_dicomConfiguration.RemoteAetHost,
            _dicomConfiguration.RemoteAetPort, false, _dicomConfiguration.LocalAetTitle,
            _dicomConfiguration.RemoteAetTitle);
        SendRequest(dicomRequest, client,token);
    }
    #endregion


    /// <summary>
    ///     Blocks until the request is received so calling code doesn't have to deal with asynchrony (see the EventWaitHandle in TrySend).
    ///     Only the timeout is applied no Throtelling, the client is unreleased on return
    /// </summary>
    /// <param name="dicomRequest"></param>
    /// <param name="client"></param>
    /// <param name="token"></param>

    #region SendRequest
    public void SendRequest(DicomRequest dicomRequest, IDicomClient client,CancellationToken token)
    {
        client.AddRequestAsync(dicomRequest).Wait(token);
        SendRequest(client,token);
    }
    #endregion

    /// <summary>
    ///     Blocks until the request is received so calling code doesn't have to deal with asynchrony (see the EventWaitHandle in TrySend).
    ///     Only the timeout is applied no Throtelling, the client is unreleased on return
    /// </summary>
    /// <param name="client"></param>
    /// <param name="token"></param>

    #region SendRequest
    public void SendRequest(IDicomClient client,CancellationToken token)
    {
        _listener.OnNotify(this, new NotifyEventArgs(
            verbose ? ProgressEventType.Information : ProgressEventType.Trace,
            $"Sending request to {_dicomConfiguration.RemoteAetTitle} at {_dicomConfiguration.RemoteAetHost}:{_dicomConfiguration.RemoteAetHost}"));
        bool completed;
        try
        {
            completed = client.SendAsync(token)
                .Wait(_dicomConfiguration.TransferTimeOutInMilliseconds + 1000, token);
        }
        catch (Exception ex)
        {
            OnRequestException?.Invoke(ex);
            throw new Exception($"Error when attempting to send DICOM request: {ex.Message}", ex);
        }

        if(completed)
            OnRequestSucess?.Invoke();
        else
            OnRequestTimeout?.Invoke();

    }
    #endregion


}