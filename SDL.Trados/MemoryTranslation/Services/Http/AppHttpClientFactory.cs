using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace SDL.Trados.MTUOC.Services.Http
{
    public class AppHttpClientFactory
    {
        private readonly object _lockObject = new object();
        private readonly int _timeOffsetSeconds = 30;
        private IList<KeyValuePair<DateTime, HttpClient>> _clients = new List<KeyValuePair<DateTime, HttpClient>>();

        public int StackElements
        {
            get { return _clients.Count(); }
        }

        public HttpClient CreateClient(bool forceNewClient = false)
        {
            lock (_lockObject)
            {
                
                if (forceNewClient)
                {
                    var service = new HttpClient();
                    _clients.Add(new KeyValuePair<DateTime, HttpClient>(DateTime.Now, service));
                    return service;
                }

                var client = _clients.Where(x => x.Key.AddSeconds(_timeOffsetSeconds) > DateTime.Now).FirstOrDefault();
                if (client.Value != null)
                    _clients.Remove(client);

                client = new KeyValuePair<DateTime, HttpClient>(DateTime.Now, client.Value ?? new HttpClient());
                _clients.Add(client);
                return client.Value;
            }
        }
    }
}
