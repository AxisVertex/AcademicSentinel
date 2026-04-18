using System;
using System.Windows;
using System.Windows.Controls;

namespace AcademicSentinel.Client.Views.IMC
{
    /// <summary>
    /// Interaction logic for CreateRoomWindow.xaml
    /// </summary>
    public partial class CreateRoomWindow : Window
    {
        private static readonly Random _random = new Random();

        public CreateRoomWindow()
        {
            InitializeComponent();

            // Auto-generate room code on window load
            GenerateRandomRoomCode();
            TxtRoomSubject.Focus();
        }

        /// <summary>
        /// Generates a random alphanumeric room code.
        /// Format: [Letter][Number][Letters][4-digit number] (e.g. "2TSYS2526")
        /// </summary>
        private void GenerateRandomRoomCode()
        {
            // Generate a code pattern similar to "2TSYS2526"
            string prefix = _random.Next(1, 9).ToString();                       // 1 digit
            string letters = GenerateRandomLetters(4);                            // 4 uppercase letters
            string suffix = _random.Next(1000, 9999).ToString();                 // 4 digits

            TxtRoomCode.Text = $"{prefix}{letters}{suffix}";
            UpdateRoomPreview();
        }

        private string GenerateRandomLetters(int count)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            char[] result = new char[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = chars[_random.Next(chars.Length)];
            }
            return new string(result);
        }

        private void BtnGenerateCode_Click(object sender, RoutedEventArgs e)
        {
            GenerateRandomRoomCode();
        }

        private void FormField_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRoomPreview();
        }

        private void UpdateRoomPreview()
        {
            if (TxtRoomPreview == null || TxtRoomCode == null || TxtRoomSubject == null || TxtSection == null)
                return;

            string code = TxtRoomCode.Text.Trim();
            string subject = TxtRoomSubject.Text.Trim();
            string section = TxtSection.Text.Trim();

            if (!string.IsNullOrEmpty(code) || !string.IsNullOrEmpty(subject) || !string.IsNullOrEmpty(section))
            {
                string preview = code;
                if (!string.IsNullOrEmpty(subject))
                    preview += $" - {subject}";
                if (!string.IsNullOrEmpty(section))
                    preview += $" - {section}";
                TxtRoomPreview.Text = preview;
            }
            else
            {
                TxtRoomPreview.Text = "Room Preview: --";
            }
        }

        private void BtnCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRoomSubject.Text))
            {
                MessageBox.Show("Please enter a Room Subject / Course.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtRoomSubject.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtSection.Text))
            {
                MessageBox.Show("Please enter a Section.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtSection.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtRoomCode.Text))
            {
                MessageBox.Show("Please generate a Room Code.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
