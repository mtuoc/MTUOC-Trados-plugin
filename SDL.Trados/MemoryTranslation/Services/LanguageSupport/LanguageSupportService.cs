using Sdl.LanguagePlatform.Core;
using SDL.Trados.MTUOC.DTO;
using System.Globalization;

namespace SDL.Trados.MTUOC.Services.LanguageSupport
{
    public sealed class LanguageSupportService : ILanguageSupportService
    {
        public LanguageSupportService()
        {
        }

        public bool SuppostDirection(string source, string target, SettingsDto settings)
        {
            return SuppostDirection(new LanguagePair(source, target), settings);
        }

        public bool SuppostDirection(CultureInfo source, CultureInfo target, SettingsDto settings)
        {
            return SuppostDirection(new LanguagePair(source, target), settings);
        }

        public bool SuppostDirection(LanguagePair languageDirection, SettingsDto settings)
        {
            return true;
        }
    }
}
