using Microsoft.Extensions.DependencyInjection;
using SDL.Trados.MTUOC;
using SDL.Trados.MTUOC.Services;
using SDL.Trados.MTUOC.Services.Http;
using SDL.Trados.MTUOC.Services.WebSocketClient;
using System;

namespace SDL.Trados.Test.Fixtures
{
    public class DIFixture : IDisposable
    {
        public DIFixture()
        {
            DIContainer = Startup.DIContainer = ServicesConfig.GetProvider();

            WebSocketFactory = DIContainer.GetService<WebSocketFactory>();
            HttpService = DIContainer.GetService<IHttpService>();
        }

        public IServiceProvider DIContainer { get; }
        public IHttpService HttpService { get; }
        public WebSocketFactory WebSocketFactory { get; }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
