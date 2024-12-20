using System;
using System.Collections.Generic;
using System.Linq;

namespace SDL.Trados.MTUOC.Services.WebSocketClient
{
    public class WebSocketFactory
    {
        private readonly object _lockObject = new object();
        private readonly TimeSpan _timeOffset = TimeSpan.FromSeconds(30);
        private IList<KeyValuePair<DateTime, IWebSocketService>> _services = new List<KeyValuePair<DateTime, IWebSocketService>>();

        public int StackElements
        {
            get { return _services.Count(); }
        }

        public IWebSocketService GetService(bool forceNewService = false)
        {
            lock (_lockObject)
            {
                DisposeConnections();

                if (forceNewService)
                {
                    var service = new WebSocketService();
                    _services.Add(new KeyValuePair<DateTime, IWebSocketService>(DateTime.Now, service));
                    return service;
                }

                var availableService = _services.Where(x => x.Value.Semaphore.CurrentCount > 0).FirstOrDefault();
                if (availableService.Value != null)
                    _services.Remove(availableService);

                availableService = new KeyValuePair<DateTime, IWebSocketService>(DateTime.Now, availableService.Value ?? new WebSocketService());
                _services.Add(availableService);
                return availableService.Value;
            }
        }

        private void DisposeConnections()
        {
            var disabledConnections = _services.Where(x => x.Key.Add(_timeOffset) <= DateTime.Now && x.Value?.Semaphore.CurrentCount == 0);
            foreach (var item in disabledConnections)
                item.Value?.Dispose();
            _services = _services.Where(x => !disabledConnections.Contains(x)).ToList();
        }
    }
}
