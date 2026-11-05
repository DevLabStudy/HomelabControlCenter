using System.Windows;
using System.Windows.Input;

namespace HomelabControlCenter
{
    public partial class CustomInputDialog : Window
    {
        public string ResponseText => TxtInput.Text;

        public CustomInputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtPrompt.Text = prompt;
            TxtInput.Text = defaultValue;
            TxtInput.Focus();
            TxtInput.SelectAll();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                Close();
            }
        }

        public static string Prompt(Window owner, string title, string prompt, string defaultValue = "")
        {
            var dialog = new CustomInputDialog(title, prompt, defaultValue)
            {
                Owner = owner
            };

            return dialog.ShowDialog() == true ? dialog.ResponseText : string.Empty;
        }
    }
}