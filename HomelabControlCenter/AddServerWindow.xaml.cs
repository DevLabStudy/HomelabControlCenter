using System.Windows;

namespace HomelabControlCenter
{
    public partial class AddServerWindow : Window
    {
        public string ServerName => TxtServerName.Text.Trim();
        public string ServerIp => TxtServerIp.Text.Trim();
        public string ServerUser => TxtServerUser.Text.Trim();
        public string ServerPassword => TxtServerPassword.Password;
        public bool RememberPassword => ChkRememberPassword.IsChecked == true;

        public AddServerWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ServerName) || string.IsNullOrWhiteSpace(ServerIp) || string.IsNullOrWhiteSpace(ServerUser))
            {
                MessageBox.Show("Uzupełnij nazwę, adres IP oraz użytkownika SSH.", "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}