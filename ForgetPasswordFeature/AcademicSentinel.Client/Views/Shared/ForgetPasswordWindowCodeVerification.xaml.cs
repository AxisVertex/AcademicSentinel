using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AcademicSentinel.Client.Services;

namespace AcademicSentinel.Client.Views.Shared
{
    /// <summary>
    /// Interaction logic for ForgetPasswordWindowCodeVerification.xaml
    /// </summary>
    public partial class ForgetPasswordWindowCodeVerification : Window
    {
        private readonly string _email;
        private readonly AuthService _authService;

        public ForgetPasswordWindowCodeVerification(string email)
        {
            InitializeComponent();
            _authService = new AuthService();
            _email = email;
            TxtCodeHint.Text = $"Sent to {_email}";
            TxtCode1.Focus();
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

        private async void BtnVerifyCode_Click(object sender, RoutedEventArgs e)
        {
            string enteredCode = string.Concat(TxtCode1.Text, TxtCode2.Text, TxtCode3.Text, TxtCode4.Text, TxtCode5.Text, TxtCode6.Text);
            if (enteredCode.Length != 6)
            {
                MessageBox.Show("Please enter the 6-digit code.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resetToken = await _authService.VerifyPasswordResetCodeAsync(_email, enteredCode);
            if (string.IsNullOrWhiteSpace(resetToken))
            {
                MessageBox.Show("Invalid or expired code. Please try again.", "Verification Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            new ForgetPasswordWindowChangePass(_email, resetToken).Show();
            Close();
        }

        private async void LinkResendCode_Click(object sender, MouseButtonEventArgs e)
        {
            bool sent = await _authService.RequestPasswordResetCodeAsync(_email);
            if (sent)
            {
                MessageBox.Show("A new code has been sent to your email.", "Resend Code", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearCodeFields();
                TxtCode1.Focus();
            }
            else
            {
                MessageBox.Show("Failed to resend code.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox current && current.Text.Length == 1)
            {
                if (!char.IsDigit(current.Text[0]))
                {
                    current.Text = string.Empty;
                    return;
                }

                MoveToNextCodeBox(current);
            }
        }

        private void MoveToNextCodeBox(TextBox current)
        {
            TextBox[] codeBoxes = { TxtCode1, TxtCode2, TxtCode3, TxtCode4, TxtCode5, TxtCode6 };
            int currentIndex = Array.IndexOf(codeBoxes, current);
            if (currentIndex >= 0 && currentIndex < codeBoxes.Length - 1)
            {
                codeBoxes[currentIndex + 1].Focus();
            }
        }

        private void ClearCodeFields()
        {
            TxtCode1.Clear();
            TxtCode2.Clear();
            TxtCode3.Clear();
            TxtCode4.Clear();
            TxtCode5.Clear();
            TxtCode6.Clear();
        }
    }
}
