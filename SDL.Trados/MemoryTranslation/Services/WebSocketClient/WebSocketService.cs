using Microsoft.Extensions.DependencyInjection;
using NLog;
using Polly;
using SDL.Trados.MTUOC.Services.Cache;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SDL.Trados.MTUOC.Services.WebSocketClient
{
    /// <summary>
    /// Cliente WebSocket
    /// </summary>
    /// <remarks>
    /// Cliente para conexiones mediante websocket. Contiene política de reintentos.
    /// </remarks>
    public sealed class WebSocketService : IWebSocketService
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1);
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Constructor
        /// </summary>
        public WebSocketService()
        {
            _cacheService = Startup.DIContainer.GetService<ICacheService>();
        }

        private const int MAX_RESTRIES = 3;
        private const int CONNECTION_TIMEOUT_SECONDS = 3;
        private const int SEND_TIMEOUT_SECONDS = 10;
        private readonly ICacheService _cacheService;
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSend;
        private CancellationTokenSource _cancellationTokenConnection;

        /// <inheritdoc/>
        public WebSocketState ConnectionState => _webSocket?.State ?? WebSocketState.None;

        /// <inheritdoc/>
        public async Task<WebSocketState> ConnectAsync(Uri url)
        {
            url = ProcessUri(url);

            var retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                retryCount: MAX_RESTRIES,
                sleepDurationProvider: attemptCount => TimeSpan.FromSeconds(attemptCount),
                onRetry: (ex, sleepDuration, attemptCount, context) => _logger.Error(ex, $"Retry Error {attemptCount}"));

            return await retryPolicy.ExecuteAsync(() => ConnectWithNoRetries(url));
        }

        /// <inheritdoc/>
        public async Task<string> SendAsync(string message, string project, Uri url = null)
        {
            try
            {
                var cache = _cacheService.GetValue(message, project);
                if (!string.IsNullOrEmpty(cache))
                {
                    _logger.Info($"Websocket Message Cached: {message} -> {cache}");
                    return cache;
                }

                await Semaphore.WaitAsync();
                if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                {
                    if (url == null)
                        throw new Exception("You must indicate the url and port or make a connection before sending the message");

                    await ConnectAsync(url);
                }
                if (_cancellationTokenSend != null)
                    _cancellationTokenSend.Dispose();
                _cancellationTokenSend = new CancellationTokenSource(TimeSpan.FromSeconds(SEND_TIMEOUT_SECONDS));

                var msgBytes = Encoding.UTF8.GetBytes(message);
                var buffer = new ArraySegment<byte>(msgBytes);

                _logger.Info($"Websocket Message Send: {message}");
                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationTokenSend.Token);
                var result = await ReceiveAsync();

                _cacheService.SetValue(message, result, project);
                return result;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Websocket Send Error");
                throw;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private Uri ProcessUri(Uri uri)
        {
            if (!uri.AbsoluteUri.StartsWith("ws://") && !uri.AbsoluteUri.StartsWith("wss://"))
                return new Uri($"ws://{uri.AbsoluteUri}");

            return uri;
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync()
        {
            try
            {
                if (_webSocket is null)
                    return;
                if (_webSocket.State == WebSocketState.Open)
                {
                    _cancellationTokenConnection?.CancelAfter(TimeSpan.FromSeconds(2));
                    _cancellationTokenSend?.CancelAfter(TimeSpan.FromSeconds(2));
                    await _webSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                _webSocket.Dispose();
                _webSocket = null;
                _cancellationTokenConnection?.Dispose();
                _cancellationTokenConnection = null;
                _cancellationTokenSend?.Dispose();
                _cancellationTokenSend = null;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Websocket Disconnect Error");
                throw;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisconnectAsync().Wait();
        }

        private async Task<WebSocketState> ConnectWithNoRetries(Uri url)
        {
            try
            {
                _logger.Info($"Websocket Init Connection Url: {url.AbsoluteUri}");

                if (_webSocket != null)
                {
                    if (_webSocket.State == WebSocketState.Open)
                        return WebSocketState.Open;
                    else
                        _webSocket.Dispose();
                }
                _webSocket = new ClientWebSocket();

                if (_cancellationTokenConnection != null)
                    _cancellationTokenConnection.Dispose();
                _cancellationTokenConnection = new CancellationTokenSource(TimeSpan.FromSeconds(CONNECTION_TIMEOUT_SECONDS));

                await _webSocket.ConnectAsync(url, _cancellationTokenConnection.Token);
                _logger.Info($"Websocket End Connection Url: {url.AbsoluteUri}, State: {_webSocket.State}");
                return _webSocket.State;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Websocket Connection Error");
                throw;
            }
        }

        private async Task<string> ReceiveAsync()
        {
            var buffer = new byte[255];
            var rcvBuffer = new ArraySegment<byte>(buffer);
            var rcvResult = await _webSocket.ReceiveAsync(rcvBuffer, _cancellationTokenSend.Token);
            var msgBytesReceived = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
            var msgReceived = Encoding.UTF8.GetString(msgBytesReceived);

            _logger.Info($"Websocket Message Received: {msgReceived}");
            return msgReceived;
        }
    }
}
