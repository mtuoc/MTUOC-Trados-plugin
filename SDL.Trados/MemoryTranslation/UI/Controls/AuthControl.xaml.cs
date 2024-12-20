using System.Windows;
using System.Windows.Controls;

namespace SDL.Trados.MTUOC.UI.Controls
{
    /// <summary>
    /// Lógica de interacción para AuthControl.xaml
    /// </summary>
    public partial class AuthControl : UserControl
    {
        public AuthControl()
        {
            InitializeComponent();
            root.DataContext = this;
        }

        public static readonly DependencyProperty PwdProperty = DependencyProperty.Register(
            nameof(Pwd), typeof(string), typeof(AuthControl));

        public string Pwd
        {
            get => GetValue(PwdProperty)?.ToString();
            set => SetValue(PwdProperty, value);
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            pwdHide.Visibility = Visibility.Hidden;
            pwdText.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            pwdHide.Visibility = Visibility.Visible;
            pwdText.Visibility = Visibility.Hidden;
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            Pwd = pwdHide.Password;
        }
    }
}
