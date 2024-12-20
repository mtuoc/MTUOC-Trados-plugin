using Newtonsoft.Json;
using Sdl.LanguagePlatform.TranslationMemoryApi;
using SDL.Trados.MTUOC.DTO;
using System;

namespace SDL.Trados.MTUOC.Studio
{
    [TranslationProviderFactory(Id = nameof(MTUOCProviderFactory),
                                Name = nameof(MTUOCProviderFactory),
                                Description = nameof(MTUOCProviderFactory))]
    class MTUOCProviderFactory : ITranslationProviderFactory
    {
        #region ITranslationProviderFactory Members

        public ITranslationProvider CreateTranslationProvider(Uri translationProviderUri, string translationProviderState, ITranslationProviderCredentialStore credentialStore)
        {
            var originalUri = new Uri(Constants.FULL_PROVIDER_SCHEME);
            var credentials = credentialStore.GetCredential(originalUri);
            var settings = JsonConvert.DeserializeObject<SettingsDto>(credentials.Credential);

            return new MTUOCProvider(settings);
        }

        public TranslationProviderInfo GetTranslationProviderInfo(Uri translationProviderUri, string translationProviderState)
        {
            var info = new TranslationProviderInfo
            {
                TranslationMethod = TranslationMethod.MachineTranslation,
                Name = PluginResources.ProviderFactoryInfo
            };
            return info;
        }

        public bool SupportsTranslationProviderUri(Uri translationProviderUri)
        {
            if (translationProviderUri == null)
                throw new ArgumentNullException(nameof(translationProviderUri));

            return string.Equals(translationProviderUri.Scheme, Constants.PROVIDER_SCHEME, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
