using Sdl.LanguagePlatform.TranslationMemory;
using SDL.Trados.MTUOC.DTO;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml;

namespace SDL.Trados.MTUOC.UI
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class ConnectionPage : Window
    {
        public ConnectionPage(SettingsDto settings)
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;
            LoadSettings(settings);
            LoadVersion();
        }

        private void LoadVersion()
        {
            version.Content = $"{PluginResources.Version}";
        }

        public SettingsDto GetSettings()
        {
            return new SettingsDto
            {
                Port = connection.Port,
                ServerIp = connection.ServerIp,
                DaysCacheFilesExpiration = advancedControl.TimeFileExpired,
                Score = advancedControl.Score,
                TranslationType = (advancedControl.translateType.SelectedItem as TranslationTypeDto)?.Value ?? TranslationUnitOrigin.MachineTranslation
            };
        }

        private void CheckConnectionControl_OnStartCheckConnection(object sender, string message)
        {
            SetActivityIndicator(true);
            connectionControl.Settings = GetSettings();
        }

        private void CheckConnectionControl_OnEndCheckConnection(object sender, string message)
        {
            SetActivityIndicator(false);
        }

        private void PreviewNumericTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex _regex = new Regex("[^0-9.-]+");
            e.Handled = _regex.IsMatch(e.Text);
        }

        private void LoadSettings(SettingsDto settings)
        {
            foreach (var item in advancedControl.translateType.ItemsSource)
            {
                if (item is TranslationTypeDto dto && dto.Value == settings.TranslationType)
                    advancedControl.translateType.SelectedItem = dto;
            }
            connection.ServerIp = settings.ServerIp;
            connection.Port = settings.Port;
            advancedControl.TimeFileExpired = settings.DaysCacheFilesExpiration;
            advancedControl.Score = settings.Score;
        }

        private void SetActivityIndicator(bool show)
        {
            activityIndicator.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            layout.IsEnabled = !show;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSettings())
                return;
            DialogResult = true;
            Close();
        }

        private bool ValidateSettings()
        {
            return true;
        }

        #region Disable Maximize Button

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;

        private const int WS_MAXIMIZEBOX = 0x10000; //maximize button
        private IntPtr _windowHandle;
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            DisableMinimizeButton();
        }

        protected void DisableMinimizeButton()
        {
            if (_windowHandle == IntPtr.Zero)
                throw new InvalidOperationException("The window has not yet been completely initialized");

            SetWindowLong(_windowHandle, GWL_STYLE, GetWindowLong(_windowHandle, GWL_STYLE) & ~WS_MAXIMIZEBOX);
        }

        #endregion
    }
}
