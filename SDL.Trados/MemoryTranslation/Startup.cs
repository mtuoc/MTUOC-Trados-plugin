using Microsoft.Extensions.Logging;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using SDL.Trados.MTUOC.Log;
using NLog;
using SDL.Trados.MTUOC.Services;
using System;

namespace SDL.Trados.MTUOC
{
    [ApplicationInitializer]
    public class Startup : IApplicationInitializer
    {
        private static readonly Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private static IServiceProvider _dIContainer;
        public static IServiceProvider DIContainer
        {
            get { return _dIContainer; }
            set
            {
                if (_dIContainer != null)
                    return;
                _dIContainer = value;
            }
        }

        public void Execute()
        {
            NlogConfig.Init(false);
            _logger.Info("Startup execute");
            DIContainer = ServicesConfig.GetProvider();
        }
    }
}
