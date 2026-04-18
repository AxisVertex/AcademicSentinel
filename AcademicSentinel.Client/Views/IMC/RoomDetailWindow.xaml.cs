using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AcademicSentinel.Client.Constants;
using AcademicSentinel.Client.Services;
using System.Collections.Generic;

namespace AcademicSentinel.Client.Views.IMC
{
    public partial class RoomDetailWindow : Window
    {
        public int CurrentRoomId { get; private set; }
        public ObservableCollection<SessionItem> Sessions { get; set; }

        public RoomDetailWindow(int roomId, string roomTitle)
        {
            InitializeComponent();

            CurrentRoomId = roomId;
            TxtRoomTitle.Text = roomTitle;

            Sessions = new ObservableCollection<SessionItem>();
            SessionsList.ItemsSource = Sessions;

            // Load Sidebar Branding
            LoadTeacherSidebarInfo();

            // Load Database Content
            UpdatePaginationUI();
            FetchRoomStatus();
            FetchPastSessions();
        }

        private void LoadTeacherSidebarInfo()
        {
            if (SessionManager.CurrentUser != null)
            {
                TxtSidebarProfName.Text = !string.IsNullOrWhiteSpace(SessionManager.CurrentUser.FullName)
                    ? SessionManager.CurrentUser.FullName
                    : SessionManager.CurrentUser.Email.Split('@')[0];

                if (!string.IsNullOrEmpty(SessionManager.CurrentUser.ProfileImageUrl))
                {
                    try
                    {
                        // Safety: Trim slashes to prevent "http://localhost:5000//path"
                        string baseUrl = ApiEndpoints.BaseUrl.TrimEnd('/');
                        string imgPath = SessionManager.CurrentUser.ProfileImageUrl.TrimStart('/');
                        string fullUrl = $"{baseUrl}/{imgPath}";

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullUrl);
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Force fresh download
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        SidebarProfileBrush.ImageSource = bitmap;
                        SidebarProfileImage.Visibility = Visibility.Visible;
                        SidebarDefaultIcon.Visibility = Visibility.Collapsed;
                    }
                    catch (Exception ex)
                    {
                        // If this hits, your URL is likely malformed. 
                        System.Diagnostics.Debug.WriteLine($"PFP Load Failed: {ex.Message}");
                    }
                }
            }
        }

        // ======================== UPDATED SIDEBAR NAVIGATION ========================

        // Both Profile and Courses now point to the TeacherDashboard
        private void NavProfile_Click(object sender, RoutedEventArgs e) => NavigateBackToDashboard();
        private void NavCourses_Click(object sender, RoutedEventArgs e) => NavigateBackToDashboard();

        // Back button also returns to the Dashboard
        private void BtnBack_Click(object sender, RoutedEventArgs e) => NavigateBackToDashboard();

        // Helper method to handle the transition
        private void NavigateBackToDashboard()
        {
            // Create and show the Dashboard
            var dashboard = new TeacherDashboard();
            dashboard.Show();

            // Close this Room Detail window to prevent window piling
            this.Close();
        }

        // Help button goes to the Shared Help Guide
        private void NavHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new AcademicSentinel.Client.Views.Shared.HelpGuideWindow();
            helpWindow.Owner = this; // Centers it over the room window

            // ShowDialog opens it as a popup. The user must close it to click the room again.
            helpWindow.ShowDialog();

            // REMOVED: this.Close(); 
        }

        // ======================== DATA LOADING ========================

        private async void FetchPastSessions()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
                    var response = await client.GetAsync($"{ApiEndpoints.Rooms}/{CurrentRoomId}/history");

                    if (response.IsSuccessStatusCode)
                    {
                        var pastSessions = await response.Content.ReadFromJsonAsync<List<PastSessionDto>>();

                        if (pastSessions != null)
                        {
                            Sessions.Clear();
                            foreach (var s in pastSessions)
                            {
                                DateTime startTime = s.StartTime.ToLocalTime();
                                string status = s.Status;

                                string durationText = startTime.ToString("MMM dd, yyyy - hh:mm tt");

                                if (s.EndTime.HasValue)
                                {
                                    DateTime endTime = s.EndTime.Value.ToLocalTime();
                                    int minutes = (int)Math.Round((endTime - startTime).TotalMinutes);
                                    durationText += $" ({minutes} mins)";
                                }

                                Sessions.Add(new SessionItem
                                {
                                    SessionId = $"Session {Math.Max(1, s.SessionNumber)}",
                                    DateDuration = durationText,
                                    StatusText = status,
                                    Status = status,
                                    ExamType = string.IsNullOrWhiteSpace(s.ExamType) ? "Summative" : s.ExamType,
                                    StudentCount = s.ParticipantCount
                                });
                            }
                            UpdatePaginationUI();
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"History Load Error: {ex.Message}"); }
        }

        private async void FetchRoomStatus()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
                    var response = await client.GetAsync($"{ApiEndpoints.Rooms}/{CurrentRoomId}/status");
                    // Data mapping for student counts can go here
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void UpdatePaginationUI()
        {
            if (EmptySessionList != null) EmptySessionList.Visibility = Sessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (TxtPaginationInfo != null) TxtPaginationInfo.Text = $"Showing {Sessions.Count} sessions";
        }

        private void BtnCreateSession_Click(object sender, RoutedEventArgs e)
        {
            var setupWindow = new CreateSessionSetupWindow(CurrentRoomId, TxtRoomTitle.Text);
            setupWindow.Owner = this;
            if (setupWindow.ShowDialog() == true)
            {
                var liveWindow = new LiveSessionMonitoringWindow(
                    CurrentRoomId,
                    TxtRoomTitle.Text,
                    setupWindow.CreatedSessionId,
                    setupWindow.MonitoringDurationSeconds,
                    setupWindow.EndSessionWhenTimerEnds,
                    setupWindow.StartDelaySeconds);
                this.Hide();
                liveWindow.Closed += (_, __) =>
                {
                    this.Show();
                    FetchPastSessions();
                    FetchRoomStatus();
                };
                liveWindow.Show();
            }
        }

        private void BtnStudentList_Click(object sender, RoutedEventArgs e)
        {
            // 1. Open the actual Student List Window
            var studentListWindow = new StudentListWindow(CurrentRoomId, TxtRoomTitle.Text);
            studentListWindow.Owner = this;
            studentListWindow.ShowDialog();

            // 2. Refresh the room status when the list window closes 
            // (in case the teacher added/removed students while in that window)
            FetchRoomStatus();
        }

        private void SessionAction_Click(object sender, RoutedEventArgs e) => new LiveSessionMonitoringWindow(CurrentRoomId, TxtRoomTitle.Text).Show();
        private async void BtnGenerateCode_Click(object sender, RoutedEventArgs e) { /* Code Generation Logic */ }
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) { }
        private void CmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void ViewSession_Click(object sender, RoutedEventArgs e) { }
        private void DeleteSession_Click(object sender, RoutedEventArgs e) { }
    }

    public class SessionItem
    {
        public string SessionId { get; set; } = string.Empty;
        public string DateDuration { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    public class PastSessionDto
    {
        public int Id { get; set; }
        public int SessionNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
    }

    public class GenerateCodeResponse { public string EnrollmentCode { get; set; } = string.Empty; }
}