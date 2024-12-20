using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using SDL.Trados.MTUOC.Log;
using SDL.Trados.MTUOC.Services;
using System;

namespace SDL.Trados.MTUOC
{
    [ApplicationInitializer]
    public class Startup : IApplicationInitializer
    {
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

            DIContainer = ServicesConfig.GetProvider();
        }
    }
}
