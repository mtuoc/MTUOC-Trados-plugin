using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemoryApi;
using SDL.Trados.MTUOC.DTO;
using SDL.Trados.MTUOC.Services.Cache;
using SDL.Trados.MTUOC.Services.LanguageSupport;
using System;

namespace SDL.Trados.MTUOC.Studio
{
    public class MTUOCProvider : ITranslationProvider
    {
        private SettingsDto _settings;
        private readonly TranslationProviderUriBuilder _uriBuilder;
        private readonly ILanguageSupportService _languageSupportService;

        public MTUOCProvider(SettingsDto settings)
        {
            _settings = settings;
            _uriBuilder = new TranslationProviderUriBuilder(Constants.PROVIDER_SCHEME);

            if (_settings == null)
                return;

            var minDate = DateTime.Now.AddDays((-1) * _settings.DaysCacheFilesExpiration);
            Startup.DIContainer.GetService<ICacheService>().ClearCaches(minDate);
            _languageSupportService = Startup.DIContainer.GetService<ILanguageSupportService>();
        }

        public ITranslationProviderLanguageDirection GetLanguageDirection(LanguagePair languageDirection)
        {
            return new MTUOCProviderLanguageDirectionV2(this, languageDirection, _settings);
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public void LoadState(string translationProviderState)
        {
            _settings = JsonConvert.DeserializeObject<SettingsDto>(translationProviderState);
        }

        public string Name
        {
            get { return PluginResources.TranslationProviderName; }
        }

        public void RefreshStatusInfo()
        {
        }

        public string SerializeState()
        {
            return JsonConvert.SerializeObject(_settings);
        }

        public ProviderStatusInfo StatusInfo
        {
            get { return new ProviderStatusInfo(true, PluginResources.TranslationProviderStatusInfo); }
        }

        public bool SupportsConcordanceSearch
        {
            get { return false; }
        }

        public bool SupportsDocumentSearches
        {
            get { return false; }
        }

        public bool SupportsFilters
        {
            get { return false; }
        }

        public bool SupportsFuzzySearch
        {
            get { return false; }
        }

        public bool SupportsLanguageDirection(LanguagePair languageDirection)
        {
            return _languageSupportService.SuppostDirection(languageDirection, _settings);
        }

        public bool SupportsMultipleResults
        {
            get { return false; }
        }

        public bool SupportsPenalties
        {
            get { return true; }
        }

        public bool SupportsPlaceables
        {
            get { return true; }
        }

        public bool SupportsScoring
        {
            get { return true; }
        }

        public bool SupportsSearchForTranslationUnits
        {
            get { return true; }
        }

        public bool SupportsSourceConcordanceSearch
        {
            get { return false; }
        }

        public bool SupportsStructureContext
        {
            get { return false; }
        }

        public bool SupportsTaggedInput
        {
            get { return false; }
        }

        public bool SupportsTargetConcordanceSearch
        {
            get { return false; }
        }

        public bool SupportsTranslation
        {
            get { return true; }
        }

        public bool SupportsUpdate
        {
            get { return true; }
        }

        public bool SupportsWordCounts
        {
            get { return false; }
        }

        public TranslationMethod TranslationMethod
        {
            get { return TranslationMethod.MachineTranslation; }
        }

        public Uri Uri
        {
            get { return _uriBuilder.Uri; }
        }
    }
}

