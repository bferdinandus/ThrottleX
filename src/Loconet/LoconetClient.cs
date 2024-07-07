using Loconet.Msg;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Loconet
{
    /// <summary>
    /// Implements a thread that keeps retrying to establish a TCP connection to a given host/port.
    /// Receives ASCII lines to interface implementation
    /// https://loconetovertcp.sourceforge.net/Protocol/LoconetOverTcp.html
    /// </summary>
    public class LoconetClient : IDisposable
    {
        private readonly string _host;
        private readonly ushort _port;
        private readonly MessageLookup _messageLookup = new ();
        private readonly CancellationTokenSource _cancellationSource = new ();
        private readonly CancellationToken _cancellation;
        private readonly byte[] _buffer = new byte[1000];
        private readonly Thread _receiveThread;
        private readonly AutoResetEvent _sentEvent = new(false);
        private readonly AutoResetEvent _replyEvent = new(false);
        private enum LoconetState { Start, Connect, Init, Operation, Wait, Shutdown };

        private LoconetState _state = LoconetState.Start;
        private TcpClient? _client;
        private bool _sentError;
        private bool _nextReceiveIsReply = false;
        private ReceivableLoconetMessage? _reply;

        public readonly ILogger Logger;

        /// <summary>
        /// Attach handler to do stuff on LoconetOverTcp connection establishment
        /// </summary>
        public event Action? OnConnectionEstablished;

        /// <summary>
        /// Attach handler for received LocoNet messages
        /// </summary>
        public event Action<ReceivableLoconetMessage>? OnMessageReceived;

        public LoconetClient(string host, ushort port, ILogger logger)
        {
            _host = host;
            _port = port;
            PeerName = $"{_host}:{_port}";
            Logger = logger;

            _cancellation = _cancellationSource.Token;
            _receiveThread = new Thread(ReceiveThreadMain);
        }

        public string PeerName { get; }

        public bool IsOperational => _state == LoconetState.Operation;

        public string? ServerVersionInfo { get; private set; } = null;

        public override string ToString()
        {
            return $"{PeerName} state {_state}";
        }

        /// <summary>
        /// Result of a send request.
        /// Inspired by https://sourceforge.net/p/loconetovertcp/svn/HEAD/tree/cpp-shared/trunk/AbstractLoconetApi.h#l23
        /// </summary>
        public enum LoconetSendResult
        {
            Invalid = 0, // make sure the default value is not SentOk
            Success = 1, // SENT OK or even an awaited reply was received
            ReplyTimeout = -10,
            SocketError = -5,
            SentError = -3,
            SentTimeout = -2,
            UnexpectedReply = -1,
        }

        public LoconetSendResult BlockingSend<TFormat>(TFormat msg)
            where TFormat : FormatBase, ILoconetMessageFormat
        {
            var msgHex = msg.Encode<TFormat>().ToHex();
            return BlockingSend(msgHex);
        }

        private LoconetSendResult BlockingSend(string msgHex)
        {
            Logger.LogTrace($"Sending msg {msgHex}");
            var line = Encoding.ASCII.GetBytes($"SEND {msgHex}\r\n");

            lock (_sentEvent) // serialize send requests, which utilize _sentEvent
            {
                _sentEvent.Reset();  // from now on a SENT triggers _sentEvent

                try
                {
                    _client!.Client.Send(line);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, $"Failed to send, returning SocketeError");
                    return LoconetSendResult.SocketError;
                }

                var sent = _sentEvent.WaitOne(TimeSpan.FromSeconds(3));  // waiting for SENT

                if (!sent)
                {
                    Logger.LogWarning("Timed out waiting for SENT, returning SentTimeout");
                    return LoconetSendResult.SentTimeout;
                }

                if (_sentError)
                {
                    Logger.LogWarning("returning SentError");
                    return LoconetSendResult.SentError;
                }

                return LoconetSendResult.Success;
            }
        }

        public LoconetSendResult SendAndWaitReply<TSentFormat, TExpectedReply>(TSentFormat request, out TExpectedReply? reply)
            where TSentFormat : FormatBase, ILoconetMessageFormat
            where TExpectedReply : ReceivableLoconetMessage
        {
            reply = null;
            lock (_replyEvent) // serialize waiting for reply, which use _replyEvent
            {
                _replyEvent.Reset();

                var sent = BlockingSend(request);
                if (sent != LoconetSendResult.Success)
                    return sent;

                var replied = _replyEvent.WaitOne(TimeSpan.FromSeconds(3));  // waiting for the reply

                if (!replied)
                    return LoconetSendResult.ReplyTimeout;

                if (!typeof(TExpectedReply).IsAssignableFrom(_reply!.GetType()))
                    return LoconetSendResult.UnexpectedReply;

                reply = (TExpectedReply) _reply;
                return LoconetSendResult.Success;
            }
        }

        private void ReceiveThreadMain()
        {
            try 
            { 
                TimeSpan RetryWait = TimeSpan.FromSeconds(1);

                while (!_cancellation.IsCancellationRequested)
                {
                    try
                    {
                        _state = LoconetState.Connect;
                        Logger.LogInformation($"Connecting to {PeerName}");
                        _client = new TcpClient(_host, _port);

                        _state = LoconetState.Init;
                        Logger.LogInformation("Initializing");
                        OnConnectionEstablished?.Invoke();

                        _state = LoconetState.Operation;
                        Logger.LogInformation("In normal operation");
                        while (!_cancellation.IsCancellationRequested)
                        {
                            var s = ReceiveLineIntoBuffer();
                            Logger.LogTrace($"Received '{s}' from {PeerName}");
                            ProcessLine(s);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return; // nominal way to exit thread for shutdown
                    }
                    catch (EofException) 
                    {
                        Logger.LogTrace($"EOF in receive thread, waiting {RetryWait.TotalSeconds}s and retrying to connect... ({PeerName})");
                    }
                    catch (Exception ex)
                    {
                        if (_cancellation.IsCancellationRequested)
                        {
                            Logger.LogDebug($"Ignoring {ex.GetType().Name} while shutting down");
                            return; // normal way to exit thread for shutdown if we were blocked reading from socket
                        }
                        Logger.LogError(ex, $"Caught exception in Loconet receive thread, waiting {RetryWait.TotalSeconds}s and retrying to connect... ({PeerName})");
                    }
                    _client = null;
                    _state = LoconetState.Wait;
                    _cancellation.WaitHandle.WaitOne(RetryWait);
                }
            }
            finally
            {
                _state = LoconetState.Shutdown;
            }
        }

        public class EofException : Exception
        {
        }

        private string ReceiveLineIntoBuffer()
        {
            var bufferFill = 0;
            var stream = _client!.GetStream();

            while (!_cancellation.IsCancellationRequested)
            {
                var c = stream.ReadByte();
                switch (c)
                {
                    case -1: // eof
                        Logger.LogInformation($"Received EOF ({PeerName})");
                        throw new EofException(); // nominal way to end receiving loop

                    case 13: // cr
                    case 10: // lf
                        if (bufferFill == 0)
                            continue; // no data received yet, than this is an empty line or the rest of a bigger newline sequence
                        return Encoding.ASCII.GetString(_buffer, 0, bufferFill); // otherwise this ends a line that we return

                    default:
                        _buffer[bufferFill++] = (byte)c;  // this throws out of range exception in case of buffer overflow
                        break;
                }
            }
            Logger.LogInformation($"Cancelled ({PeerName})");
            throw new OperationCanceledException(); // nominal way to end thread for shutdown
        }

        private void ProcessLine(string line)
        {
            try
            {
                var space = line.IndexOf(' ');
                if (space == -1)
                    throw new InvalidOperationException($"Did not find space in '{line}'");

                var token = line.Substring(0, space);
                var param = line.Substring(space + 1);

                switch (token)
                {
                    case "VERSION": OnVersion(param); return;
                    case "RECEIVE": OnReceive(param); return;
                    case "SENT": OnSent(param); return;
                    case "TIMESTAMP": OnTimestamp(param); return;
                    case "BREAK": OnBreak(param); return;
                    case "ERROR": OnError(param); return;
                    default:
                        Logger.LogWarning($"Ignoring unknown token '{token}'.");
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Ignoring exception for processing line from server");
            }
        }

        protected virtual void OnVersion(string param)
        {
            ServerVersionInfo = param;
        }

        /// <summary>
        /// Transform the hex string into a byte array and pass to message handler.
        /// This reuses _buffer for a second purpose.
        /// </summary>
        /// <param name="param"></param>
        protected virtual void OnReceive(string param)
        {
            var i = 0;
            bool first = true;
            foreach (var c in param)
            {
                if (c == ' ')
                    continue;

                var bin = c.HexChar2Value();
                
                if (first)
                    _buffer[i] = (byte)(bin << 4);
                else
                    _buffer[i++] |= (byte)bin;

                first = !first;
            }
            var msg = new byte[i];
            Array.Copy(_buffer, msg, i);

            ReceivableLoconetMessage? parsed = _messageLookup.ParseMessage(msg);

            if (parsed == null)
                parsed = new LoconetMessage(msg);

            // Are we waiting for a reply and this is not an OPC_BUSY?
            if (_nextReceiveIsReply && msg[0]!=0x81)
            {
                _nextReceiveIsReply = false;
                _reply = parsed;
                _replyEvent.Set();
            }

            OnMessageReceived?.Invoke(parsed!);
        }

        protected virtual void OnSent(string param)
        {
            _sentError = !param.StartsWith("OK");
            _sentEvent.Set();
            _nextReceiveIsReply = true; // no matter if anybody is waiting for a reply...
        }

        protected virtual void OnTimestamp(string param)
        {
        }

        protected virtual void OnBreak(string param)
        {
            Logger.LogError($"Received break: {param} ms");
        }

        protected virtual void OnError(string param)
        {
            Logger.LogError($"Received error: '{param}'");
        }

        public void Start()
        {
            Logger.LogInformation($"Starting Loconet client for {PeerName}");
            _receiveThread.Start();
        }

        public void Dispose()
        {
            Logger.LogInformation($"Stopping Loconet client for {PeerName}");
            _cancellationSource.Cancel();
            _client?.Dispose();
            _receiveThread.Join();
            _cancellationSource.Dispose();
            Logger.LogDebug("stopped.");
        }
    }
}
