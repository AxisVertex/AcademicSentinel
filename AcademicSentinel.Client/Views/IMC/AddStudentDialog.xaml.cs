using AcademicSentinel.Client.Constants;
using AcademicSentinel.Client.Services;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json; // FIX: This is needed for PostAsJsonAsync
using System.Windows;

namespace AcademicSentinel.Client.Views.IMC
{
    public partial class AddStudentDialog : Window
    {
        private int _roomId; // FIX: Added the field to store the ID

        // FIX: Constructor now accepts the roomId
        public AddStudentDialog(int roomId)
        {
            InitializeComponent();
            _roomId = roomId;

            // NOTE: Ensure your XAML TextBox has x:Name="TxtEmail"
            TxtEmail.Focus();
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Please enter a student email.");
                return;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);

                    // POST to the manual enrollment endpoint
                    var response = await client.PostAsJsonAsync($"{ApiEndpoints.Rooms}/{_roomId}/enroll-email", email);

                    if (response.IsSuccessStatusCode)
                    {
                        this.DialogResult = true;
                        this.Close();
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show(error, "Assignment Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Network Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}