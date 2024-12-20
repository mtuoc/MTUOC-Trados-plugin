using SDL.Trados.MTUOC.DTO;
using SDL.Trados.MTUOC.Log;
using SDL.Trados.Test.Fixtures;
using System;
using Xunit;

namespace SDL.Trados.Test
{
    public class HttpTest : IClassFixture<DIFixture>
    {
        private readonly DIFixture _fixture;
        private const string SERVER_IP = "http://mtuoc.uoclabs.uoc.es";
        private const string SERVER_IP_NO_PREFIX = "mtuoc.uoclabs.uoc.es";
        private const int PORT = 8000;

        public HttpTest(DIFixture fixture)
        {
            NlogConfig.Init(true);
            _fixture = fixture;
        }

        [Fact(DisplayName = "Successful Request. String")]
        public void SendString()
        {
            var settings = new SettingsDto { ServerIp = SERVER_IP, Port = PORT };
            var result = _fixture.HttpService.SendMessage(settings, "Hello word", null, null, string.Empty);
            Assert.NotNull(result);
            Assert.True(!string.IsNullOrEmpty(result?.Tgt));
        }

        [Fact(DisplayName = "Successful Request. No Prefix String")]
        public void SendStringNoPrefix()
        {
            var settings = new SettingsDto { ServerIp = SERVER_IP_NO_PREFIX, Port = PORT };
            var result = _fixture.HttpService.SendMessage(settings, "Hello word", null, null, string.Empty);
            Assert.NotNull(result);
            Assert.True(!string.IsNullOrEmpty(result?.Tgt));
        }

        [Fact(DisplayName = "Successful Request. DTO")]
        public void SendDto()
        {
            var settings = new SettingsDto { ServerIp = SERVER_IP, Port = PORT };
            var id = Guid.NewGuid().ToString();
            var request = new HttpRequestDto
            {
                Id = id,
                Src = "Hello peter"
            };
            var result = _fixture.HttpService.SendMessage(settings, request, string.Empty);
            Assert.NotNull(result);
            Assert.True(!string.IsNullOrEmpty(result?.Tgt));
            Assert.Equal(id, result?.Id);
        }
    }
}
