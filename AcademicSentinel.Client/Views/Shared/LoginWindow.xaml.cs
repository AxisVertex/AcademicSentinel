using System;
using System.Windows;
using System.Windows.Input;
using System.Net.Http.Json;
using AcademicSentinel.Client.Services;
using AcademicSentinel.Client.Models;
using AcademicSentinel.Client.Views.IMC;
using AcademicSentinel.Client.Views.SAC; // Added SAC reference

namespace AcademicSentinel.Client.Views.Shared
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtLoginEmail.Text.Trim();
            string password = TxtLoginPassword.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter your email and password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnLogin.IsEnabled = false;
            BtnLogin.Content = "Logging in...";

            try
            {
                bool isSuccess = await _authService.LoginAsync(email, password);

                if (isSuccess)
                {
                    // Check the role to decide which dashboard to open!
                    string userRole = SessionManager.CurrentUser?.Role;

                    if (userRole == "Instructor")
                    {
                        new TeacherDashboard().Show();
                    }
                    else if (userRole == "Student")
                    {
                        new StudentDashboard().Show();
                    }

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid email or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetLoginButton();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection Error: {ex.Message}");
                ResetLoginButton();
            }
        }

        private void ResetLoginButton()
        {
            TxtLoginPassword.Clear();
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "Log In";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void LinkCreateAccount_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            this.Close();
        }

        private void RoleToggle_Changed(object sender, RoutedEventArgs e) { }
    }
}