using Newtonsoft.Json;
using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemory;
using System;
using System.Collections.Generic;

namespace SDL.Trados.MTUOC.DTO
{
    public class SettingsDto
    {
        public int Port { get; set; }
        public string ServerIp { get; set; }
        public int DaysCacheFilesExpiration { get; set; } = 7;
        public int Score { get; set; }
        public List<LanguagePair> Languajes { get; set; }
        public TranslationUnitOrigin TranslationType { get; set; }

        [JsonIgnore, Obsolete("service used by old websocket protocol")]
        public Uri FullServerUri
        {
            get
            {
                var serverIp = ServerIp;
                if (!serverIp.StartsWith("ws://") && !serverIp.StartsWith("wss://"))
                    serverIp = $"ws://{serverIp}";
                return new Uri($"{serverIp}:{Port}/translate");
            }
        }
        [JsonIgnore]
        public Uri FullServerHttpUri
        {
            get
            {
                var serverIp = ServerIp;
                if (!serverIp.StartsWith("http://") && !serverIp.StartsWith("https://"))
                    serverIp = $"http://{serverIp}";
                return new Uri($"{serverIp}:{Port}/translate");
            }
        }
    }
}
