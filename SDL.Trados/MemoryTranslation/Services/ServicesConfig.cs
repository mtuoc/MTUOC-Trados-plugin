using Microsoft.Extensions.DependencyInjection;
using SDL.Trados.MTUOC.Services.Cache;
using SDL.Trados.MTUOC.Services.Http;
using SDL.Trados.MTUOC.Services.LanguageSupport;
using SDL.Trados.MTUOC.Services.Tags;
using SDL.Trados.MTUOC.Services.WebSocketClient;
using NLog;
using System;

namespace SDL.Trados.MTUOC.Services
{
    public static class ServicesConfig
    {
        private static readonly Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        public static IServiceProvider GetProvider()
        {
            _logger.Info("Creating service collection");
            var serviceCollection = new ServiceCollection()
                .AddHttpClient()
                .AddTransient<IHttpService, HttpService>()
                .AddSingleton<ILanguageSupportService, LanguageSupportService>()
                .AddSingleton<ITagsServiceV2, TagsServiceV2>()
                .AddSingleton<ICacheService, CacheService>()
                .AddSingleton<AppHttpClientFactory>()
                .AddSingleton<WebSocketFactory>();

            _logger.Info("Created service collection");

            return serviceCollection.BuildServiceProvider();
        }
    }
}
