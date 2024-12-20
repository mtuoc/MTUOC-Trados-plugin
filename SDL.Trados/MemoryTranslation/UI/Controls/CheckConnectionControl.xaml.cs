using Microsoft.Extensions.DependencyInjection;
using SDL.Trados.MTUOC.DTO;
using SDL.Trados.MTUOC.Services.Http;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SDL.Trados.MTUOC.UI.Controls
{
    /// <summary>
    /// Lógica de interacción para CheckConnectionControl.xaml
    /// </summary>
    public partial class CheckConnectionControl : UserControl
    {
        private readonly IHttpService _httpService;
        public CheckConnectionControl()
        {
            InitializeComponent();
            _httpService = Startup.DIContainer?.GetService<IHttpService>();
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            nameof(Settings), typeof(SettingsDto), typeof(CheckConnectionControl));

        public SettingsDto Settings
        {
            get => (SettingsDto)GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        public delegate void StartCheckConnectionHandler(object sender, string message);
        public event StartCheckConnectionHandler OnStartCheckConnection;

        public delegate void EndCheckConnectionHandler(object sender, string message);
        public event EndCheckConnectionHandler OnEndCheckConnection;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            OnStartCheckConnection?.Invoke(sender, messageTextBox.Text);
            try
            {
                var result = string.Empty;
                var settings = Settings;
                var text = messageTextBox.Text;
                await Task.Run(() =>
                    result = _httpService.SendMessage(settings, text, null, null, string.Empty, true)?.Tgt);
                Dispatcher.Invoke(() => messageTextBoxResult.Text = result);
            }
            catch
            {
                messageTextBoxResult.Text = string.Empty;
            }
            OnEndCheckConnection?.Invoke(sender, messageTextBoxResult.Text);
        }
    }
}
