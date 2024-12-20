using SDL.Trados.MTUOC.Log;
using SDL.Trados.Test.Fixtures;
using System;
using System.Threading.Tasks;
using Xunit;
using SystemWebSockets = System.Net.WebSockets;

namespace SDL.Trados.MTUOC.Test
{
    public class WebsocketTest : IClassFixture<DIFixture>
    {
        private readonly DIFixture _fixture;
        private readonly Uri _serverUri = new Uri($"ws://mtuoc.uoclabs.uoc.es:{PORT}/{ENDPOINT}");
        private readonly Uri _incorrectFormatUri = new Uri($"mtuoc.uoclabs.uoc.es:{PORT}/{ENDPOINT}");
        private readonly Uri _errorUri = new Uri($"error-ip:{PORT}/{ENDPOINT}");

        private const int PORT = 8000;
        private const string ENDPOINT = "translate";

        public WebsocketTest(DIFixture fixture)
        {
            NlogConfig.Init(true);
            _fixture = fixture;
        }

        [Fact(DisplayName = "Successful Connection", Skip = "Old websocket server not available")]
        public async Task Connection()
        {
            var service = _fixture.WebSocketFactory.GetService();
            var result = await service.ConnectAsync(_serverUri);
            Assert.Equal(SystemWebSockets.WebSocketState.Open, result);
            Assert.Equal(SystemWebSockets.WebSocketState.Open, service.ConnectionState);
        }

        [Fact(DisplayName = "Error Connection. Incorrect Server IP", Skip = "Old websocket server not available")]
        public async Task ConnectionError()
        {
            var startTime = DateTime.Now;
            var service = _fixture.WebSocketFactory.GetService(true);
            await Assert.ThrowsAsync<SystemWebSockets.WebSocketException>(
                () => service.ConnectAsync(_errorUri)
            );
            var endTime = DateTime.Now;
            // Check retries. Each retrie consume [retry number] seconds.
            // N retries = (N+1)*N/2 seconds 
            // Example: 3 retries = (3+1)*3/2 seconds, 5 retries = (5+1)*5/2 seconds
            int retries = 3;
            Assert.True(endTime - startTime > TimeSpan.FromSeconds((retries+1)*retries/2));
        }

        [Fact(DisplayName = "Successful Connection. Incorrect Server IP Format", Skip = "Old websocket server not available")]
        public async Task ConnectionArgumentError()
        {
            var result = await _fixture.WebSocketFactory.GetService(true).ConnectAsync(_incorrectFormatUri);
            Assert.Equal(SystemWebSockets.WebSocketState.Open, result);
        }

        [Fact(DisplayName = "Sending successful message", Skip = "Old websocket server not available")]
        public async Task SendMessage()
        {
            var service = _fixture.WebSocketFactory.GetService();
            var response = await service.SendAsync("Hello!", string.Empty, _serverUri);
            Assert.Equal(SystemWebSockets.WebSocketState.Open, service.ConnectionState);
            Assert.NotEqual(string.Empty, response ?? string.Empty);
        }

        [Fact(DisplayName = "Web Socket Stack. Several Elements", Skip = "Old websocket server not available")]
        public async Task WebSocketFactoryStack()
        {
            await Task.WhenAll(
                _fixture.WebSocketFactory.GetService().SendAsync("Hello!", string.Empty, _serverUri),
                _fixture.WebSocketFactory.GetService().SendAsync("Hello!", string.Empty, _serverUri),
                _fixture.WebSocketFactory.GetService().SendAsync("Hello!", string.Empty, _serverUri)
            );
            Assert.NotEqual(1, _fixture.WebSocketFactory.StackElements);
        }
    }
}
