using SDL.Trados.MTUOC.DTO;
using System.Globalization;

namespace SDL.Trados.MTUOC.Services.Http
{
    public interface IHttpService
    {
        HttpResponseDto SendMessage(SettingsDto settings, string message, CultureInfo source, CultureInfo target, string project, bool ignoreCache = false);
        HttpResponseDto SendMessage(SettingsDto settings, HttpRequestDto dto, string project, bool ignoreCache = false);
    }
}
