using Sdl.LanguagePlatform.Core;
using SDL.Trados.MTUOC.DTO;
using System.Globalization;

namespace SDL.Trados.MTUOC.Services.LanguageSupport
{
    internal interface ILanguageSupportService
    {
        bool SuppostDirection(string source, string target, SettingsDto settings);
        bool SuppostDirection(CultureInfo source, CultureInfo target, SettingsDto settings);
        bool SuppostDirection(LanguagePair languageDirection, SettingsDto settings);
    }
}
