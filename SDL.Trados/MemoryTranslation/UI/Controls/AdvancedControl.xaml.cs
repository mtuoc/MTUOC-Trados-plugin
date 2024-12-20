using Microsoft.Extensions.DependencyInjection;
using NLog.LayoutRenderers.Wrappers;
using Sdl.LanguagePlatform.TranslationMemory;
using SDL.Trados.MTUOC.DTO;
using SDL.Trados.MTUOC.Services.Cache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SDL.Trados.MTUOC.UI.Controls
{
    /// <summary>
    /// Lógica de interacción para CacheControl.xaml
    /// </summary>
    public partial class AdvancedControl : UserControl
    {
        private readonly ICacheService _cacheService;
        public AdvancedControl()
        {
            InitializeComponent();
            root.DataContext = this;
            _cacheService = Startup.DIContainer?.GetService<ICacheService>();
            translateType.ItemsSource = TranslationTypes;
        }

        public static readonly DependencyProperty TimeFileExpiredProperty = DependencyProperty.Register(
            nameof(TimeFileExpired), typeof(int), typeof(AdvancedControl));
        public int TimeFileExpired
        {
            get => (int)GetValue(TimeFileExpiredProperty);
            set => SetValue(TimeFileExpiredProperty, value);
        }

        public static readonly DependencyProperty ScoreProperty = DependencyProperty.Register(
            nameof(Score), typeof(int), typeof(AdvancedControl));
        public int Score
        {
            get => (int)GetValue(ScoreProperty);
            set => SetValue(ScoreProperty, value);
        }

        public ObservableCollection<TranslationTypeDto> TranslationTypes = GetTranlationTypes();

        private static ObservableCollection<TranslationTypeDto> GetTranlationTypes()
        {
            var aux = new ObservableCollection<TranslationTypeDto>();
            foreach(var item in Enum.GetValues(typeof(TranslationUnitOrigin)))
                aux.Add(new TranslationTypeDto { Value = (TranslationUnitOrigin)item });
            return aux;
        }

        private void PreviewNumericTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex _regex = new Regex("[^0-9.-]+");
            e.Handled = _regex.IsMatch(e.Text);
        }

        private void ButtonDelte_Click(object sender, RoutedEventArgs e)
        {
            _cacheService.ClearCaches(DateTime.Now.AddDays(1));
        }
    }
}
