using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using AcademicSentinel.Client.Services;

namespace AcademicSentinel.Client.Views.Shared
{
    public partial class ForgetPasswordWindow : Window
    {
        private readonly AuthService _authService;

        public ForgetPasswordWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
            TxtEmail.Focus();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        private async void BtnSendCode_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please enter your email.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnSendCode.IsEnabled = false;
            BtnSendCode.Content = "Sending...";

            bool sent = await _authService.RequestPasswordResetCodeAsync(email);
            if (!sent)
            {
                string errorMessage = _authService.LastErrorMessage ?? "Failed to send verification code. Please check your connection or server email settings.";
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnSendCode.IsEnabled = true;
                BtnSendCode.Content = "Enter";
                return;
            }

            new ForgetPasswordWindowCodeVerification(email).Show();
            Close();
        }

        private void LinkSignUp_Click(object sender, MouseButtonEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }
    }
}
