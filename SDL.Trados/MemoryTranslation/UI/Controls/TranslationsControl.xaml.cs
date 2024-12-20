using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sdl.LanguagePlatform.Core;
using SDL.Trados.MTUOC.DTO;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SDL.Trados.MTUOC.UI.Controls
{
    /// <summary>
    /// Lógica de interacción para TranslationsControl.xaml
    /// </summary>
    public partial class TranslationsControl : UserControl
    {
        public TranslationsControl()
        {
            InitializeComponent();
            root.DataContext = this;
            translations.ItemsSource = Translations;
        }

        public ObservableCollection<LanguagePair> Translations { get; set; } = new ObservableCollection<LanguagePair>();
        public ObservableCollection<LanguageDto> Items { get; } = GetItems();

        public static ObservableCollection<LanguageDto> GetItems()
        {
            using (var file = new StreamReader(new MemoryStream(PluginResources.countries), Encoding.Default))
            using (var reader = new JsonTextReader(file))
            {
                var jObject = (JObject)JToken.ReadFrom(reader);
                var translations = jObject.Properties().Select(x => new LanguageDto { Code = x.Name, Name = x.Value.ToString() });
                return new ObservableCollection<LanguageDto>(translations);
            }
        }

        private void AddTranslation_Click(object sender, RoutedEventArgs e)
        {
            var sourceLanguaje = (source.SelectedItem as LanguageDto)?.Code;
            var targetLanguaje = (target.SelectedItem as LanguageDto)?.Code;

            if (!ValidateNewTranslation(sourceLanguaje, targetLanguaje))
                return;

            Translations.Add(new LanguagePair(sourceLanguaje, targetLanguaje));
        }

        private bool ValidateNewTranslation(string sourceLanguaje, string targetLanguaje)
        {
            if (sourceLanguaje.Equals(targetLanguaje))
            {
                MessageBox.Show(PluginResources.ErrorDuplicateLanguage, PluginResources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (Translations.Any(x => x.SourceCultureName == sourceLanguaje && x.TargetCultureName == targetLanguaje))
            {
                MessageBox.Show(PluginResources.ErrorTranslationDuplicate, PluginResources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void RemoveTranslation_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (LanguagePair)translations.SelectedItem;
            var result = MessageBox.Show(
                $"{PluginResources.ConfirmRemoveTranslations}\n\n{selectedItem.SourceCulture}/{selectedItem.TargetCultureName}",
                PluginResources.Info, MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes)
                return;

            Translations.Remove(selectedItem);
        }
    }
}
