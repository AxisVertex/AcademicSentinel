using System;
using System.Windows;
using System.Windows.Input;
using AcademicSentinel.Client.Services;

namespace AcademicSentinel.Client.Views.Shared
{
    public partial class ForgetPasswordWindowChangePass : Window
    {
        private readonly string _email;
        private readonly string _resetToken;
        private readonly AuthService _authService;

        public ForgetPasswordWindowChangePass(string email, string resetToken)
        {
            InitializeComponent();
            _authService = new AuthService();
            _email = email;
            _resetToken = resetToken;
            TxtNewPassword.Focus();
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

        private async void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = TxtNewPassword.Password;
            string confirmPassword = TxtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Please fill in all fields.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters.", "Weak Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                MessageBox.Show("Passwords do not match.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool success = await _authService.ResetPasswordAsync(_email, newPassword, _resetToken);
            if (!success)
            {
                MessageBox.Show("Failed to reset password. Please verify your reset session and try again.", "Reset Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Password reset successful for {_email}. You can now log in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            new LoginWindow().Show();
            Close();
        }

        private void LinkSignUp_Click(object sender, MouseButtonEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }
    }
}
