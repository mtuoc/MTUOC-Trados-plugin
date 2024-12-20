using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog;
using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemoryApi;
using SDL.Trados.MTUOC.DTO;
using SDL.Trados.MTUOC.Services.Cache;
using SDL.Trados.MTUOC.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace SDL.Trados.MTUOC.Studio
{
    [TranslationProviderWinFormsUi(Id = nameof(MTUOCProviderWinFormsUI),
                                   Name = nameof(MTUOCProviderWinFormsUI),
                                   Description = nameof(MTUOCProviderWinFormsUI))]
    public class MTUOCProviderWinFormsUI : ITranslationProviderWinFormsUI
    {
        #region Properties

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Determines whether this supplied translation provider can be edited (i.e. whether any settings can be changed).
        /// </summary>
        /// <remarks>
        /// true if the provider's settings can be changed, and false otherwise.
        /// </remarks>
        public bool SupportsEditing
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// Gets the type description of the factory
        /// </summary>
        public string TypeDescription
        {
            get
            {
                return PluginResources.Plugin_Name;
            }
        }
        /// <summary>
        /// Gets the type name of the factory
        /// </summary>
        public string TypeName
        {
            get
            {
                return PluginResources.Plugin_Name;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Displays a dialog to interactively browse for one or more translation providers.
        /// </summary>
        /// <param name="owner">The window that will own the dialog</param>
        /// <param name="languagePairs">A collection of language pairs. 
        /// If provided, the list of available translation providers will be filtered by these language directions.</param>
        /// <param name="credentialStore">A credential store object that can be used to retrieve credentials required.</param>
        /// <returns>A collection of translation providers selected by the user, 
        /// or null if none were selected or available or the browse was cancelled.</returns>
        public ITranslationProvider[] Browse(IWin32Window owner, LanguagePair[] languagePairs, ITranslationProviderCredentialStore credentialStore)
        {
            var credentials = GetCredentials(credentialStore, owner);
            return GetDialogResult(credentialStore, credentials) ? new ITranslationProvider[] { new MTUOCProvider(credentials) } : null;
        }
       
        /// <summary>
        /// Displays a dialog to interactively change any of the translation provider settings.
        /// </summary>
        /// <param name="owner">The window that will own the dialog</param>
        /// <param name="translationProvider">A translation provider descriptor, representing the translation provider to edit.</param>
        /// <param name="languagePairs">A collection of language pairs. If provided, the list of available translation 
        /// providers will be filtered by these language directions.</param>
        /// <param name="credentialStore">A credential store object that can be used to retrieve credentials required.</param>
        /// <returns>True if changes were made to the translation provider; false otherwise.</returns>
        /// <exception cref="InvalidOperationException">Thrown when calling this method while 
        /// Sdl.LanguagePlatform.TranslationMemoryApi.ITranslationProviderWinFormsUI.SupportsEditing return false.</exception>
        public bool Edit(IWin32Window owner, ITranslationProvider translationProvider, LanguagePair[] languagePairs, ITranslationProviderCredentialStore credentialStore)
        {
            if (!(translationProvider is MTUOCProvider))
                return false;
            
            var savedCredentials = GetCredentials(credentialStore, owner);
            return savedCredentials != null && GetDialogResult(credentialStore, savedCredentials);
        }

        /// <summary>
        /// Gets the credentials from the user and puts these credentials in the credential store.
        /// </summary>
        /// <param name="owner">The window that will own the dialog</param>
        /// <param name="translationProviderUri">translation provider uri</param>
        /// <param name="translationProviderState">translation provider state</param>
        /// <param name="credentialStore">credential store</param>
        /// <returns>true if the user provided credentials or false if the user canceled</returns>
        public bool GetCredentialsFromUser(IWin32Window owner, Uri translationProviderUri, string translationProviderState, ITranslationProviderCredentialStore credentialStore)
        {
            return false;
        }
        
        /// <summary>
        /// Gets display information for the specified translation provider.
        /// </summary>
        /// <remarks>
        /// Note that this method can potentially be called very frequently so it is not
        /// advisable to instantiate the translation provider within its implementation.
        /// </remarks>
        /// <param name="translationProviderUri">A translation provider URI, representing the translation provider.</param>
        /// <param name="translationProviderState">Optional translation provider state information, which can be used to determine
        /// certain aspects of the display information.</param>
        /// <returns>A Sdl.LanguagePlatform.TranslationMemoryApi.TranslationProviderDisplayInfo object,
        /// containing display information that allows an application to represent the translation
        /// provider without having to instantiate it.</returns>
        public TranslationProviderDisplayInfo GetDisplayInfo(Uri translationProviderUri, string translationProviderState)
        {
            var info = new TranslationProviderDisplayInfo
            {
                Name = PluginResources.Plugin_Name,
                TooltipText = PluginResources.Plugin_Name,
                TranslationProviderIcon = PluginResources.LogoMTUOC
            };
            return info;
        }

        /// <summary>
        /// Returns true if this component supports the specified translation provider URI.
        /// </summary>
        /// <param name="translationProviderUri">The uri.</param>
        /// <returns>True if this component supports the specified translation provider URI.</returns>
        public bool SupportsTranslationProviderUri(Uri translationProviderUri)
        {
            if (translationProviderUri == null)
                throw new ArgumentNullException(nameof(translationProviderUri));

            var supportsProvider = string.Equals(translationProviderUri.Scheme, Constants.PROVIDER_SCHEME, StringComparison.OrdinalIgnoreCase);
            return supportsProvider;
        }

        private bool GetDialogResult(ITranslationProviderCredentialStore credentialStore, SettingsDto originalSettings)
        {
            ConnectionPage dialog = GetDialog(originalSettings);
            ElementHost.EnableModelessKeyboardInterop(dialog);
            dialog.ShowDialog();


            _logger.Info(dialog?.DialogResult ?? false ? "DialogResult: Acept" : "DialogResult: Cancel");
            if (dialog.DialogResult.HasValue && dialog.DialogResult.Value)
            {
                try
                {
                    var settings = dialog.GetSettings();
                    _logger.Info(settings != null ? "Dialog Settings: " + JsonConvert.SerializeObject(settings) : "Settings KO");
                    if (originalSettings != null && !originalSettings.FullServerHttpUri.Equals(settings.FullServerHttpUri))
                        ClearCache();
                    SetCredentials(credentialStore, JsonConvert.SerializeObject(settings), true);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
                return true;
            }
            return false;
        }

        private void ClearCache()
        {
            _logger.Info($"Clear cache");
            Startup.DIContainer.GetService<ICacheService>().ClearCaches(DateTime.Now.AddDays(1));
        }

        private ConnectionPage GetDialog(SettingsDto actualSettings)
        {
            return new ConnectionPage(actualSettings ?? new SettingsDto());
        }

        private SettingsDto GetCredentials(ITranslationProviderCredentialStore credentialStore, IWin32Window owner)
        {
            var providerUri = new Uri(Constants.FULL_PROVIDER_SCHEME);
            SettingsDto cred = null;

            if (credentialStore.GetCredential(providerUri) != null)
            {
                var translationCred = new TranslationProviderCredential(credentialStore.GetCredential(providerUri).Credential, credentialStore.GetCredential(providerUri).Persist);
                cred = JsonConvert.DeserializeObject<SettingsDto>(translationCred.Credential);
            }

            return cred;
        }

        private void SetCredentials(ITranslationProviderCredentialStore credentialStore, string json, bool persistKey)
        {
            var uri = new Uri(Constants.FULL_PROVIDER_SCHEME);
            var credentials = new TranslationProviderCredential(json, persistKey);
            credentialStore.RemoveCredential(uri);
            credentialStore.AddCredential(uri, credentials);
        }

        #endregion

    }
}
