using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using SDL.Trados.MTUOC.DTO;
using SDL.Trados.MTUOC.Services.Cache;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SDL.Trados.MTUOC.Services.Http
{
    public class HttpService : IHttpService
    {
        private readonly ICacheService _cacheService;
        private readonly AppHttpClientFactory _appHttpClientFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public HttpService()
        {
            _appHttpClientFactory = Startup.DIContainer.GetService<AppHttpClientFactory>();
            _httpClientFactory = Startup.DIContainer.GetService<IHttpClientFactory>();
            _cacheService = Startup.DIContainer.GetService<ICacheService>();
        }
               
        public HttpResponseDto SendMessage(SettingsDto settings, string message, CultureInfo source, CultureInfo target, string project, bool ignoreCache = false)
        {
            var body = new HttpRequestDto
            {
                Id = Guid.NewGuid().ToString(),
                Src = message,
                SrcLang = source?.Name,
                TgtLang = target?.Name
            };
            return SendMessage(settings, body, project, ignoreCache);
        }

        public HttpResponseDto SendMessage(SettingsDto settings, HttpRequestDto body, string project, bool ignoreCache = false)
        {
            try
            {
                var cache = ignoreCache ? null : GetCacheResult(body, project);
                var response = cache ?? SendRequest(body, settings);

                ValidateResponse(response, body);
                _cacheService.SetValue(body.Src, response.Tgt, project);

                return response;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }

        private HttpResponseDto GetCacheResult(HttpRequestDto body, string project)
        {
            string value = _cacheService.GetValue(body.Src, project);
            return string.IsNullOrEmpty(value) ? null : new HttpResponseDto
            {
                Id = body.Id,
                Src = body.Src,
                Tgt = value
            };
        }

        private HttpResponseDto SendRequest(HttpRequestDto body, SettingsDto settings)
        {
            _logger.Info($"START HTTP. Source: {JsonConvert.SerializeObject(body, GetJsonConvertSettings())}");
            var requestMsg = GetRequestMessage(body, settings);
#if (Version2019 || Version2021 || Version2022)
            var client = _appHttpClientFactory?.CreateClient() ?? new HttpClient();
#else
            var client = _httpClientFactory.CreateClient();
#endif
            var result = Task.Run(async () =>
            {
                var httpResponse = await client.SendAsync(requestMsg);
                if (!httpResponse.IsSuccessStatusCode)
                    throw new Exception("Status code should be 200. Status: " + httpResponse.StatusCode);
                return await httpResponse.Content.ReadAsStringAsync();
            }).Result;

            _logger.Info($"END HTTP. Translation: {result}");
            return JsonConvert.DeserializeObject<HttpResponseDto>(result, GetJsonConvertSettings());
        }

        private void ValidateResponse(HttpResponseDto responseDto, HttpRequestDto requestDto)
        {
            if (responseDto == null)
                throw new Exception("Empty Response");

            if (!requestDto.Id.Equals(responseDto.Id))
                throw new Exception("Ids not equals");
        }

        private static HttpRequestMessage GetRequestMessage(HttpRequestDto body, SettingsDto settings)
        {
            var requestMsg = new HttpRequestMessage(HttpMethod.Post, settings.FullServerHttpUri);
            var serializeOptions = GetJsonConvertSettings();
            var json = JsonConvert.SerializeObject(body, serializeOptions);
            requestMsg.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return requestMsg;
        }

        private static JsonSerializerSettings GetJsonConvertSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }
    }
}
