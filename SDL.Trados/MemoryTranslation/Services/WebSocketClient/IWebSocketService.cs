using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SDL.Trados.MTUOC.Services.WebSocketClient
{
    public interface IWebSocketService : IDisposable
    {
        SemaphoreSlim Semaphore { get; }
        /// <summary>
        /// Connection state
        /// </summary>
        WebSocketState ConnectionState { get; }

        /// <summary>
        /// Connect via websocket with the indicated url.
        /// Makes 3 attempts consuming [number of retries] seconds each:
        /// N retries = (N+1)*N/2 seconds 
        /// </summary>
        /// <example>
        /// 3 retries = (3+1)*3/2 seconds
        /// 5 retries = (5+1)*5/2 seconds
        /// </example>
        /// <param name="url">Server URL</param>
        Task<WebSocketState> ConnectAsync(Uri url);

        /// <summary>
        /// Connect if not already done and send a message
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="url">Server URL</param>
        /// <returns>Server response</returns>
        Task<string> SendAsync(string message, string project, Uri url = null);

        /// <summary>
        /// Disconnect
        /// </summary>
        /// <returns></returns>
        Task DisconnectAsync();
    }
}
