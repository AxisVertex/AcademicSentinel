using AcademicSentinel.Client.Constants;
using AcademicSentinel.Client.Services;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace AcademicSentinel.Client.Views.IMC
{
    public partial class StudentListWindow : Window
    {
        private int _roomId;
        public ObservableCollection<ParticipantItem> Participants { get; set; }

        public StudentListWindow(int roomId, string roomTitle)
        {
            InitializeComponent();
            _roomId = roomId;
            TxtRoomTitle.Text = roomTitle;

            Participants = new ObservableCollection<ParticipantItem>();
            StudentsList.ItemsSource = Participants;

            FetchStudents();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnActionDropdown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        // REAL DELETE LOGIC
        private async void MenuRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as MenuItem)?.DataContext as ParticipantItem;
            if (selected == null) return;

            var result = MessageBox.Show($"Are you sure you want to remove {selected.StudentName}?",
                "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);

                    // Call the unenroll API we are about to add to the server
                    var response = await client.DeleteAsync($"{ApiEndpoints.Rooms}/{_roomId}/unenroll/{selected.StudentId}");

                    if (response.IsSuccessStatusCode)
                    {
                        FetchStudents(); // Refresh the list after deletion
                    }
                    else
                    {
                        MessageBox.Show("Failed to remove student from server.");
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private async void FetchStudents()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
                var response = await client.GetAsync($"{ApiEndpoints.Rooms}/{_roomId}/participants");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<ParticipantItem>>();
                    Participants.Clear();
                    if (data != null)
                    {
                        foreach (var p in data) Participants.Add(p);
                    }
                    UpdateUIStatus();
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void BtnAddStudent_Click(object sender, RoutedEventArgs e)
        {
            var addDialog = new AddStudentDialog(_roomId);
            addDialog.Owner = this;
            if (addDialog.ShowDialog() == true) FetchStudents();
        }

        private void UpdateUIStatus()
        {
            // Update the count label at the bottom
            TxtPaginationInfo.Text = $"Showing {Participants.Count} Student(s) Enrolled";

            // Toggle the "No Students" message
            EmptyStudentList.Visibility = Participants.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class ParticipantItem
    {
        public int StudentId { get; set; }
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty; // Holds the FullName from DB
        public string EnrollmentSource { get; set; } = string.Empty;
        public string ParticipationStatus { get; set; } = string.Empty;
    }
}