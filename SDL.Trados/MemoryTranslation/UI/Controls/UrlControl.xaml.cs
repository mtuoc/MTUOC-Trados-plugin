using System.Windows;
using System.Windows.Controls;

namespace SDL.Trados.MTUOC.UI.Controls
{
    /// <summary>
    /// Lógica de interacción para ConnectionControl.xaml
    /// </summary>
    public partial class UrlControl : UserControl
    {
        public UrlControl()
        {
            InitializeComponent();
            root.DataContext = this;
        }

        public static readonly DependencyProperty ServerIpProperty = DependencyProperty.Register(
            nameof(ServerIp), typeof(string), typeof(UrlControl));
        public string ServerIp
        {
            get => GetValue(ServerIpProperty)?.ToString();
            set => SetValue(ServerIpProperty, value);
        }

        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            nameof(Port), typeof(int), typeof(UrlControl));
        public int Port
        {
            get => (int)GetValue(PortProperty);
            set => SetValue(PortProperty, value);
        }

        private void TextBoxIp_GotFocus(object sender, RoutedEventArgs e)
        {
            ipTextBox.SelectAll();
        }

        private void TextBoxPort_GotFocus(object sender, RoutedEventArgs e)
        {
            portTextBox.SelectAll();
        }
    }
}
