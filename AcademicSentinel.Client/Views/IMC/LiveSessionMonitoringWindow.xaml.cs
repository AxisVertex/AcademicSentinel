using AcademicSentinel.Client.Constants;
using AcademicSentinel.Client.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading; // NEW: Required for the Live Timer
using System.Collections.Generic;
using MaterialDesignThemes.Wpf;
using AcademicSentinel.Client.Models;

namespace AcademicSentinel.Client.Views.IMC
{
    public partial class LiveSessionMonitoringWindow : Window
    {
        private HubConnection _hubConnection;
        private int _roomId;
        private int _totalAlerts = 0;
        private int _currentSessionId;
        private bool _isMonitoringStarted;
        private int _enrolledCount;
        private readonly int _monitoringDurationSeconds;
        private readonly bool _endSessionWhenTimerEnds;
        private readonly int _startDelaySeconds;
        private bool _isEndingFromTimer;
        private DateTime? _monitoringEffectiveStartTime;
        private int _countdownSecondsRemaining;

        // Timer Variables
        private DispatcherTimer _sessionTimer;
        private DispatcherTimer _participantsRefreshTimer;
        private DateTime _sessionStartTime;
        private bool _isSessionEnded;

        private ICollectionView _studentsView;
        private ICollectionView _logsView;
        private LiveStudentStatus _selectedStudent;
        private List<ParticipantDto> _allParticipants = new List<ParticipantDto>();

        public ObservableCollection<LiveStudentStatus> ActiveStudents { get; set; }
        public ObservableCollection<LogEntry> LogFeed { get; set; }

        public LiveSessionMonitoringWindow(int roomId, string roomTitle, int sessionId = 0, int monitoringDurationSeconds = 3600, bool endSessionWhenTimerEnds = true, int startDelaySeconds = 10)
        {
            InitializeComponent();
            _roomId = roomId;
            _currentSessionId = sessionId;
            _monitoringDurationSeconds = monitoringDurationSeconds;
            _endSessionWhenTimerEnds = endSessionWhenTimerEnds;
            _startDelaySeconds = Math.Max(0, startDelaySeconds);

            TxtRoomHeader.Text = $"Live Session Monitoring - {roomTitle}";
            if (FindName("TxtMonitoringState") is TextBlock monitoringState)
                monitoringState.Text = "Monitoring: Inactive";

            if (SessionManager.CurrentUser != null)
            {
                TxtProfName.Text = !string.IsNullOrWhiteSpace(SessionManager.CurrentUser.FullName)
                    ? SessionManager.CurrentUser.FullName
                    : SessionManager.CurrentUser.Email.Split('@')[0];
                SetTeacherAvatar(SessionManager.CurrentUser);
            }

            ActiveStudents = new ObservableCollection<LiveStudentStatus>();
            LogFeed = new ObservableCollection<LogEntry>();

            _studentsView = CollectionViewSource.GetDefaultView(ActiveStudents);
            _logsView = CollectionViewSource.GetDefaultView(LogFeed);

            // NEW: Auto-Sort Logic! 
            // 1st Priority: Most violations go to the top
            // 2nd Priority: Alphabetical by Email
            _studentsView.SortDescriptions.Add(new SortDescription("ViolationCount", ListSortDirection.Descending));
            _studentsView.SortDescriptions.Add(new SortDescription("Email", ListSortDirection.Ascending));

            StudentsItemsControl.ItemsSource = _studentsView;
            LogFeedItemsControl.ItemsSource = _logsView;

            _ = LoadParticipantsFromServerAsync();

            _participantsRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _participantsRefreshTimer.Tick += async (_, __) => await LoadParticipantsFromServerAsync();
            _participantsRefreshTimer.Start();
        }

        // ======================== SEARCH & FILTER LOGIC ========================

        private void TxtStudentSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = TxtStudentSearch.Text.ToLower();
            _studentsView.Filter = obj => string.IsNullOrEmpty(filter) || (obj as LiveStudentStatus).Email.ToLower().Contains(filter);
            _studentsView.Refresh();
        }

        private void CmbLogFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyAllFilters();
        }

        private void ApplyAllFilters()
        {
            if (_logsView == null) return;

            string category = (CmbLogFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Entries";

            _logsView.Filter = obj =>
            {
                var entry = obj as LogEntry;
                bool matchesUser = _selectedStudent == null ||
                                   entry.StudentEmail == _selectedStudent.Email ||
                                   entry.StudentEmail == "SYSTEM";

                bool matchesCategory = true;
                if (category == "Violations Only") matchesCategory = entry.BadgeText == "VIOLATION";
                else if (category == "Connections Only") matchesCategory = (entry.BadgeText == "JOINED" || entry.BadgeText == "LEFT" || entry.BadgeText == "KICKED");

                return matchesUser && matchesCategory;
            };
            _logsView.Refresh();
        }

        private void BtnShowGlobalLogs_Click(object sender, RoutedEventArgs e)
        {
            if (EnsureSessionNotEnded()) return;
            _selectedStudent = null;
            TxtLogHeader.Text = "Global Log Feed";
            TxtSelectedName.Text = "Select a Student";
            ApplyAllFilters();
        }

        // ======================== SESSION & SIGNALR ========================

        private async void BtnStartMonitoring_Click(object sender, RoutedEventArgs e)
        {
            if (EnsureSessionNotEnded()) return;

            var startStopLabel = FindName("TxtStartStopLabel") as TextBlock;
            var startStopIcon = FindName("StartStopIcon") as PackIcon;
            var monitoringState = FindName("TxtMonitoringState") as TextBlock;

            if (_isMonitoringStarted)
            {
                await StopMonitoringAsync();
                return;
            }

            BtnStartMonitoring.IsEnabled = false;
            if (startStopLabel != null) startStopLabel.Text = "Starting...";
            try
            {
                if (_currentSessionId <= 0)
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
                    var response = await client.PostAsJsonAsync($"{ApiEndpoints.Rooms}/{_roomId}/start-session", new { ExamType = "Summative" });
                    if (!response.IsSuccessStatusCode)
                    {
                        BtnStartMonitoring.IsEnabled = true;
                        if (startStopLabel != null) startStopLabel.Text = "Start Session Monitoring";
                        MessageBox.Show("Failed to start session.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = await response.Content.ReadFromJsonAsync<StartSessionResponse>();
                    _currentSessionId = result?.SessionId ?? 0;
                    if (monitoringState != null) monitoringState.Text = $"Monitoring: Ready (Session #{_currentSessionId})";
                }

                await InitializeSignalR();

                if (_startDelaySeconds > 0)
                {
                    await _hubConnection.InvokeAsync("BeginMonitoringCountdown", _roomId, _startDelaySeconds, _monitoringDurationSeconds > 0 ? _monitoringDurationSeconds : 0);
                    _monitoringEffectiveStartTime = DateTime.Now.AddSeconds(_startDelaySeconds);
                    _countdownSecondsRemaining = _startDelaySeconds;
                    if (monitoringState != null) monitoringState.Text = $"Monitoring: Starting in {_startDelaySeconds}s";
                    LogActivity("SYSTEM", "COUNTDOWN", $"Monitoring starts in {_startDelaySeconds} seconds.", "#FF9800");
                }
                else
                {
                    await _hubConnection.InvokeAsync("SetMonitoringState", _roomId, true);
                    _monitoringEffectiveStartTime = DateTime.Now;
                }

                _isMonitoringStarted = true;
                if (monitoringState != null)
                    monitoringState.Text = _startDelaySeconds > 0
                        ? $"Monitoring: Countdown (Session #{_currentSessionId})"
                        : $"Monitoring: Active (Session #{_currentSessionId})";
                if (startStopLabel != null) startStopLabel.Text = "Stop Monitoring";
                if (startStopIcon != null) startStopIcon.Kind = PackIconKind.Stop;
                BtnStartMonitoring.IsEnabled = true;
                TimerPanel.Visibility = Visibility.Visible; // Show the timer
                StartSessionTimer(); // Start the clock!

                LogActivity("SYSTEM", "STARTED", "Live monitoring active.", "#1B5E20");
            }
            catch (Exception ex)
            {
                if (startStopLabel != null) startStopLabel.Text = "Start Session Monitoring";
                if (startStopIcon != null) startStopIcon.Kind = PackIconKind.Play;
                BtnStartMonitoring.IsEnabled = true;
                MessageBox.Show(ex.Message);
            }
        }

        private async Task StopMonitoringAsync()
        {
            if (EnsureSessionNotEnded()) return;

            if (MessageBox.Show("Stop monitoring for this session?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            if (_currentSessionId > 0)
            {
                if (_hubConnection != null)
                {
                        await _hubConnection.InvokeAsync("SetMonitoringState", _roomId, false);
                }
            }

            _isMonitoringStarted = false;
            if (FindName("TxtMonitoringState") is TextBlock monitoringState) monitoringState.Text = "Monitoring: Inactive";
            if (FindName("TxtStartStopLabel") is TextBlock startStopLabel) startStopLabel.Text = "Start Session Monitoring";
            if (FindName("StartStopIcon") is PackIcon startStopIcon) startStopIcon.Kind = PackIconKind.Play;
            _sessionTimer?.Stop();
            TimerPanel.Visibility = Visibility.Collapsed;
            LogActivity("SYSTEM", "STOPPED", "Monitoring stopped. Session remains active.", "#D32F2F");
        }

        // NEW: Timer Method
        private void StartSessionTimer()
        {
            _sessionStartTime = DateTime.Now;
            _sessionTimer = new DispatcherTimer();
            _sessionTimer.Interval = TimeSpan.FromSeconds(1);
            _sessionTimer.Tick += async (s, e) =>
            {
                var elapsed = DateTime.Now - _sessionStartTime;
                TxtSessionTimer.Text = $"Duration: {elapsed:hh\\:mm\\:ss}";

                var elapsedForAutoStop = _monitoringEffectiveStartTime.HasValue
                    ? TimeSpan.Zero
                    : (_monitoringEffectiveStartTime == null && _startDelaySeconds > 0
                        ? DateTime.Now - (_sessionStartTime + TimeSpan.FromSeconds(_startDelaySeconds))
                        : elapsed);

                if (elapsedForAutoStop < TimeSpan.Zero)
                    elapsedForAutoStop = TimeSpan.Zero;

                if (_monitoringEffectiveStartTime.HasValue && DateTime.Now >= _monitoringEffectiveStartTime.Value)
                {
                    _monitoringEffectiveStartTime = null;
                    _countdownSecondsRemaining = 0;
                    if (FindName("TxtMonitoringState") is TextBlock monitoringState)
                        monitoringState.Text = $"Monitoring: Active (Session #{_currentSessionId})";
                    if (FindName("TxtCountdownDisplay") is TextBlock countdownDisplay)
                        countdownDisplay.Text = "00:00";
                }
                else if (_monitoringEffectiveStartTime.HasValue)
                {
                    _countdownSecondsRemaining = Math.Max(0, (int)Math.Ceiling((_monitoringEffectiveStartTime.Value - DateTime.Now).TotalSeconds));
                    if (FindName("TxtMonitoringState") is TextBlock monitoringState)
                        monitoringState.Text = $"Monitoring: Countdown {_countdownSecondsRemaining}s";
                    if (FindName("TxtCountdownDisplay") is TextBlock countdownDisplay)
                        countdownDisplay.Text = TimeSpan.FromSeconds(_countdownSecondsRemaining).ToString(@"mm\:ss");
                }
                else if (FindName("TxtCountdownDisplay") is TextBlock noCountdownDisplay)
                {
                    noCountdownDisplay.Text = "00:00";
                }

                if (FindName("TxtMonitoringTimerDisplay") is TextBlock timerDisplay)
                {
                    var timerElapsed = _monitoringEffectiveStartTime.HasValue ? TimeSpan.Zero : elapsedForAutoStop;
                    var configured = TimeSpan.FromSeconds(_monitoringDurationSeconds);
                    var timerRemaining = configured - timerElapsed;
                    if (timerRemaining < TimeSpan.Zero) timerRemaining = TimeSpan.Zero;
                    timerDisplay.Text = _monitoringDurationSeconds > 0 ? timerRemaining.ToString(@"hh\:mm\:ss") : "N/A";
                }

                if (_monitoringDurationSeconds > 0 && !_isEndingFromTimer && elapsedForAutoStop >= TimeSpan.FromSeconds(_monitoringDurationSeconds))
                {
                    _sessionTimer.Stop();

                    if (_endSessionWhenTimerEnds)
                    {
                        _isEndingFromTimer = true;
                        await Dispatcher.InvokeAsync(() => BtnEndSession_Click(this, new RoutedEventArgs()));
                    }
                    else
                    {
                        await Dispatcher.InvokeAsync(async () => await StopMonitoringOnlyAsync());
                    }
                }
            };
            _sessionTimer.Start();
        }

        private async Task StopMonitoringOnlyAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.InvokeAsync("SetMonitoringState", _roomId, false);
            }

            _isMonitoringStarted = false;
            if (FindName("TxtMonitoringState") is TextBlock monitoringState) monitoringState.Text = $"Monitoring: Stopped (Session #{_currentSessionId})";
            if (FindName("TxtStartStopLabel") is TextBlock startStopLabel) startStopLabel.Text = "Start Session Monitoring";
            if (FindName("StartStopIcon") is PackIcon startStopIcon) startStopIcon.Kind = PackIconKind.Play;
            if (FindName("TxtCountdownDisplay") is TextBlock countdownDisplay) countdownDisplay.Text = "00:00";
            if (FindName("TxtMonitoringTimerDisplay") is TextBlock timerDisplay) timerDisplay.Text = "00:00:00";
            TimerPanel.Visibility = Visibility.Collapsed;
            LogActivity("SYSTEM", "STOPPED", "Monitoring stopped by timer.", "#D32F2F");
        }

        private bool EnsureSessionNotEnded()
        {
            if (_isSessionEnded)
            {
                MessageBox.Show("Session already ended.", "Session Ended", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            return false;
        }

        private void SetTeacherAvatar(UserResponseDto user)
        {
            if (string.IsNullOrWhiteSpace(user?.ProfileImageUrl))
                return;

            string url = user.ProfileImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? user.ProfileImageUrl
                : $"{ApiEndpoints.BaseUrl}{user.ProfileImageUrl}";

            try
            {
                if (FindName("TeacherAvatarBrush") is ImageBrush avatarBrush)
                    avatarBrush.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(url, UriKind.Absolute));

                if (FindName("TeacherAvatarImage") is Ellipse avatarImage)
                    avatarImage.Visibility = Visibility.Visible;

                if (FindName("TeacherAvatarDefault") is Border avatarDefault)
                    avatarDefault.Visibility = Visibility.Collapsed;
            }
            catch
            {
            }
        }

        private async Task InitializeSignalR()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{ApiEndpoints.BaseUrl}/monitoringHub", o => o.AccessTokenProvider = () => Task.FromResult(SessionManager.JwtToken))
                .WithAutomaticReconnect().Build();

            _hubConnection.On<int>("StudentJoined", (id) => Dispatcher.Invoke(() => {
                _ = LoadParticipantsFromServerAsync();
            }));

            _hubConnection.On<int>("StudentDisconnected", (id) => Dispatcher.Invoke(() =>
            {
                _ = LoadParticipantsFromServerAsync();
            }));

            _hubConnection.On<ViolationAlertPayload>("ViolationDetected", payload => Dispatcher.Invoke(() =>
            {
                if (payload == null) return;

                var targetStudent = ActiveStudents.FirstOrDefault(s => s.StudentId == payload.StudentId);
                if (targetStudent != null)
                {
                    targetStudent.ViolationCount += Math.Max(1, payload.SeverityScore);
                    targetStudent.Status = $"ALERT: {payload.EventType}";
                    targetStudent.StatusColor = "#D32F2F";
                }

                var email = targetStudent?.Email ?? _allParticipants.FirstOrDefault(p => p.StudentId == payload.StudentId)?.StudentEmail ?? $"Student #{payload.StudentId}";
                var message = string.IsNullOrWhiteSpace(payload.Description)
                    ? payload.EventType
                    : $"{payload.EventType}: {payload.Description}";
                LogActivity(email, "VIOLATION", message, "#D32F2F");
                _studentsView.Refresh();
            }));

            _hubConnection.On<int, string, int, DateTime>("ViolationDetected", (studentId, eventType, severityScore, timestamp) => Dispatcher.Invoke(() =>
            {
                var targetStudent = ActiveStudents.FirstOrDefault(s => s.StudentId == studentId);
                if (targetStudent != null)
                {
                    targetStudent.ViolationCount += Math.Max(1, severityScore);
                    targetStudent.Status = $"ALERT: {eventType}";
                    targetStudent.StatusColor = "#D32F2F";
                }

                var email = targetStudent?.Email ?? _allParticipants.FirstOrDefault(p => p.StudentId == studentId)?.StudentEmail ?? $"Student #{studentId}";
                LogActivity(email, "VIOLATION", eventType, "#D32F2F");
                _studentsView.Refresh();
            }));

            _hubConnection.On<int>("LeaveRequested", studentId => Dispatcher.Invoke(() =>
            {
                var targetStudent = ActiveStudents.FirstOrDefault(s => s.StudentId == studentId);
                if (targetStudent == null) return;

                targetStudent.IsLeaveRequested = true;
                targetStudent.Status = "Wants to Leave";
                targetStudent.StatusColor = "#FF9800";

                LogActivity(targetStudent.Email, "LEAVE_REQ", "Student requested leave approval.", "#FF9800");
                _studentsView.Refresh();
            }));

            try { await _hubConnection.StartAsync(); await _hubConnection.InvokeAsync("JoinRoom", _roomId.ToString()); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async void BtnApproveLeave_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not LiveStudentStatus student)
                return;

            try
            {
                await _hubConnection.InvokeAsync("GrantLeave", _roomId, student.StudentId);

                student.IsLeaveRequested = false;
                student.Status = "Unlock Granted";
                student.StatusColor = "#1B5E20";

                LogActivity(student.Email, "UNLOCK", "Instructor granted leave.", "#1B5E20");
                _studentsView.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to grant leave: {ex.Message}", "Grant Leave", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LogActivity(string email, string badge, string msg, string color)
        {
            if (EmptyLogFeedState != null) EmptyLogFeedState.Visibility = Visibility.Collapsed;
            LogFeed.Insert(0, new LogEntry { Timestamp = DateTime.Now.ToString("T"), StudentEmail = email, BadgeText = badge, BadgeColor = color, Message = msg });
        }

        private void UpdateParticipantCount()
        {
            if (EmptyParticipantsState != null && ActiveStudents.Count > 0) EmptyParticipantsState.Visibility = Visibility.Collapsed;
            TxtParticipantCount.Text = $"{ActiveStudents.Count}/{_enrolledCount}";
            var missing = Math.Max(0, _enrolledCount - ActiveStudents.Count);
            if (FindName("TxtMissingCount") is TextBlock txtMissing) txtMissing.Text = $"Missing: {missing}";
        }

        private async Task LoadParticipantsFromServerAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
                var response = await client.GetAsync($"{ApiEndpoints.Rooms}/{_roomId}/participants");
                if (!response.IsSuccessStatusCode) return;

                var participants = await response.Content.ReadFromJsonAsync<List<ParticipantDto>>() ?? new List<ParticipantDto>();
                _allParticipants = participants;
                _enrolledCount = participants.Count;

                ActiveStudents.Clear();

                foreach (var p in participants.Where(p => string.Equals(p.ConnectionStatus, "Connected", StringComparison.OrdinalIgnoreCase)))
                {
                    ActiveStudents.Add(new LiveStudentStatus
                    {
                        StudentId = p.StudentId,
                        Name = string.IsNullOrWhiteSpace(p.StudentName) ? p.StudentEmail : p.StudentName,
                        Email = p.StudentEmail,
                        ProfileImageUrl = string.IsNullOrWhiteSpace(p.ProfileImageUrl)
                            ? string.Empty
                            : (p.ProfileImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? p.ProfileImageUrl
                                : $"{ApiEndpoints.BaseUrl}{p.ProfileImageUrl}"),
                        Status = "Connected",
                        StatusColor = "#4CAF50"
                    });
                }

                _studentsView.Refresh();
                UpdateParticipantCount();
            }
            catch
            {
            }
        }

        private void BtnViewParticipants_Click(object sender, RoutedEventArgs e)
        {
            if (EnsureSessionNotEnded()) return;
            ShowParticipantsOverviewWindow();
        }

        private void ShowParticipantsOverviewWindow()
        {
            var rows = _allParticipants
                .Select(p => new ParticipantOverviewRow
                {
                    Name = string.IsNullOrWhiteSpace(p.StudentName) ? p.StudentEmail : p.StudentName,
                    Email = p.StudentEmail,
                    Enrollment = p.EnrollmentSource,
                    Status = string.Equals(p.ConnectionStatus, "Connected", StringComparison.OrdinalIgnoreCase)
                        ? "In Session"
                        : "Not in Session"
                })
                .OrderBy(r => r.Status)
                .ThenBy(r => r.Name)
                .ToList();

            int joined = rows.Count(r => r.Status == "In Session");
            int missing = rows.Count - joined;

            var window = new Window
            {
                Title = "Participants Overview",
                Owner = this,
                Width = 780,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brushes.White
            };

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new TextBlock
            {
                Text = $"Participants: {joined}/{rows.Count}  |  Missing: {missing}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B5E20"))
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            var hint = new TextBlock
            {
                Text = "In Session = currently connected. Not in Session = enrolled but not connected.",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = Brushes.DimGray
            };
            Grid.SetRow(hint, 1);
            grid.Children.Add(hint);

            var table = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                ItemsSource = rows
            };
            table.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Name", Binding = new Binding("Name"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            table.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Email", Binding = new Binding("Email"), Width = new DataGridLength(2.2, DataGridLengthUnitType.Star) });
            table.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Enrollment", Binding = new Binding("Enrollment"), Width = new DataGridLength(1.2, DataGridLengthUnitType.Star) });
            table.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Status", Binding = new Binding("Status"), Width = new DataGridLength(1.2, DataGridLengthUnitType.Star) });
            Grid.SetRow(table, 2);
            grid.Children.Add(table);

            window.Content = grid;
            window.ShowDialog();
        }

        // ======================== UI ACTIONS ========================

        private void ParticipantRow_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedStudent = (sender as Border)?.DataContext as LiveStudentStatus;
            if (_selectedStudent != null)
            {
                TxtSelectedName.Text = _selectedStudent.Name;
                TxtSelectedStatus.Text = _selectedStudent.Status.ToUpper();
                SelectedStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_selectedStudent.StatusColor));
                TxtAlertCount.Text = _selectedStudent.ViolationCount.ToString();

                TxtRiskLevel.Text = _selectedStudent.ViolationCount > 3 ? "DANGER" : (_selectedStudent.ViolationCount > 0 ? "WARNING" : "SAFE");
                TxtRiskLevel.Foreground = _selectedStudent.ViolationCount > 3 ? Brushes.Red : (_selectedStudent.ViolationCount > 0 ? Brushes.Orange : Brushes.Green);

                TxtLogHeader.Text = $"Logs: {_selectedStudent.Name}";
                ApplyAllFilters();
            }
        }

        private void BtnRemoveFromSession_Click(object sender, RoutedEventArgs e)
        {
            if (EnsureSessionNotEnded()) return;
            _ = RemoveSelectedStudentAsync();
        }

        private async Task RemoveSelectedStudentAsync()
        {
            if (_selectedStudent == null) return;

            if (MessageBox.Show($"Remove {_selectedStudent.Name} from this room?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
                var response = await client.PostAsync($"{ApiEndpoints.Rooms}/{_roomId}/sessions/remove/{_selectedStudent.StudentId}", null);

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Failed to remove participant from room.", "Remove Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LogActivity(_selectedStudent.Email, "KICKED", "Instructor removed student from room.", "#D32F2F");
                BtnShowGlobalLogs_Click(null, null);
                await LoadParticipantsFromServerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing participant: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnViolations_Click(object sender, RoutedEventArgs e)
        {
            if (EnsureSessionNotEnded()) return;
            if (_selectedStudent == null)
            {
                MessageBox.Show("Select a participant first.", "Violations", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
                var response = await client.GetAsync($"{ApiEndpoints.BaseUrl}/api/violations/room/{_roomId}");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Unable to load violations.", "Violations", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var violations = await response.Content.ReadFromJsonAsync<List<ViolationLogDto>>() ?? new List<ViolationLogDto>();
                var studentLogs = violations.Where(v => string.Equals(v.StudentEmail, _selectedStudent.Email, StringComparison.OrdinalIgnoreCase)).ToList();

                var summary = new[]
                {
                    BuildRuleSummary("VAC - Virtualization/Emulator", studentLogs, new[] { "VM", "EMULATOR", "VIRTUAL" }),
                    BuildRuleSummary("HAS - Hardware/Software Artifacts", studentLogs, new[] { "HARDWARE", "ARTIFACT", "SUSPICIOUS_SETUP" }),
                    BuildRuleSummary("RTFM - Focus/Alt+Tab", studentLogs, new[] { "ALT_TAB", "FOCUS", "WINDOW_SWITCH" }),
                    BuildRuleSummary("PBD - Unauthorized Process", studentLogs, new[] { "PROCESS", "BLACKLIST" }),
                    BuildRuleSummary("CSAD - Clipboard/Screenshot", studentLogs, new[] { "CLIPBOARD", "COPY", "PASTE", "PRINTSCREEN", "SCREENSHOT" }),
                    BuildRuleSummary("IDLE - Inactivity", studentLogs, new[] { "IDLE", "INACTIVITY" })
                };

                MessageBox.Show(
                    $"Violations Summary for {_selectedStudent.Name}\n\n" + string.Join("\n", summary) + $"\n\nTotal Logged Violations: {studentLogs.Count}",
                    "Violations Summary",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load violations: {ex.Message}", "Violations", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string BuildRuleSummary(string label, List<ViolationLogDto> logs, string[] tags)
        {
            int count = logs.Count(v => tags.Any(t =>
                (!string.IsNullOrWhiteSpace(v.Module) && v.Module.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrWhiteSpace(v.Description) && v.Description.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)));
            return $"• {label}: {count}";
        }

        private async void BtnEndSession_Click(object sender, RoutedEventArgs e)
        {
            if (_isSessionEnded)
            {
                this.Close();
                return;
            }

            var endedByTimer = _isEndingFromTimer;

            if (!_isEndingFromTimer && MessageBox.Show("End session?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            _isEndingFromTimer = true;

            if (_currentSessionId > 0)
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SessionManager.JwtToken);
                await client.PutAsync($"{ApiEndpoints.Rooms}/sessions/{_currentSessionId}/end", null);
            }

            _isMonitoringStarted = false;
            _isSessionEnded = true;
            _sessionTimer?.Stop(); // Stop timer
            if (FindName("TxtMonitoringState") is TextBlock monitoringState) monitoringState.Text = "Monitoring: Session Ended";
            if (FindName("TxtStartStopLabel") is TextBlock startStopLabel)
            {
                startStopLabel.Text = "Session Ended";
                BtnStartMonitoring.IsEnabled = false;
            }
            if (FindName("StartStopIcon") is PackIcon startStopIcon) startStopIcon.Kind = PackIconKind.CheckCircle;
            if (FindName("TxtCountdownDisplay") is TextBlock countdownDisplay) countdownDisplay.Text = "00:00";
            if (FindName("TxtMonitoringTimerDisplay") is TextBlock timerDisplay) timerDisplay.Text = "00:00:00";

            if (_hubConnection != null)
            {
                try { await _hubConnection.StopAsync(); } catch { }
            }

            if (!endedByTimer)
                this.Close();
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (!_isEndingFromTimer && _currentSessionId > 0 && _isMonitoringStarted)
            {
                try
                {
                    if (_hubConnection != null)
                    {
                        await _hubConnection.InvokeAsync("SetMonitoringState", _roomId, false);
                    }
                }
                catch { }

                if (_hubConnection != null) await _hubConnection.StopAsync();
                _sessionTimer?.Stop(); // Stop timer
            }
            else if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
            }
            _participantsRefreshTimer?.Stop();
            base.OnClosing(e);
        }

        private void StudentDropdown_Click(object sender, RoutedEventArgs e)
        {
            if (EnsureSessionNotEnded()) return;
            if (sender is Button btn && btn.ContextMenu != null) { btn.ContextMenu.PlacementTarget = btn; btn.ContextMenu.IsOpen = true; }
        }

        // ======================== MOCK DATA INJECTOR ========================

        private void BtnInjectMockData_Click(object sender, RoutedEventArgs e)
        {
            ActiveStudents.Add(new LiveStudentStatus { Email = "joe@tech.edu", Status = "Connected", StatusColor = "#4CAF50" });
            ActiveStudents.Add(new LiveStudentStatus { Email = "jane@tech.edu", Status = "ALERT: ALT_TAB", StatusColor = "#D32F2F", ViolationCount = 2 });
            ActiveStudents.Add(new LiveStudentStatus { Email = "adam@tech.edu", Status = "Connected", StatusColor = "#4CAF50" });

            LogActivity("joe@tech.edu", "JOINED", "Student joined.", "#4CAF50");
            LogActivity("jane@tech.edu", "VIOLATION", "ALT_TAB detected.", "#D32F2F");
            LogActivity("adam@tech.edu", "JOINED", "Student joined.", "#4CAF50");

            UpdateParticipantCount();

            // Notice how Jane automatically goes to the top because she has 2 violations!
            _studentsView.Refresh();
            ApplyAllFilters();
        }

        public class StartSessionResponse { public int SessionId { get; set; } }
    }

    public class LiveStudentStatus : INotifyPropertyChanged
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        private string _status, _statusColor;
        private int _violations;
        private bool _isLeaveRequested;
        public string Email { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;
        public Visibility HasProfileImageVisibility => string.IsNullOrWhiteSpace(ProfileImageUrl) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasNoProfileImageVisibility => string.IsNullOrWhiteSpace(ProfileImageUrl) ? Visibility.Visible : Visibility.Collapsed;
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        public string StatusColor { get => _statusColor; set { _statusColor = value; OnPropertyChanged(); } }
        public int ViolationCount { get => _violations; set { _violations = value; OnPropertyChanged(); } }
        public bool IsLeaveRequested { get => _isLeaveRequested; set { _isLeaveRequested = value; OnPropertyChanged(); } }
        public bool HasViolation => ViolationCount > 0;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class ParticipantDto
    {
        public int StudentId { get; set; }
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string EnrollmentSource { get; set; } = string.Empty;
        public string ParticipationStatus { get; set; } = string.Empty;
        public string ConnectionStatus { get; set; } = string.Empty;
    }

    public class ParticipantOverviewRow
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Enrollment { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class LogEntry { public string Timestamp { get; set; } public string StudentEmail { get; set; } public string BadgeText { get; set; } public string BadgeColor { get; set; } public string Message { get; set; } }

    public class ViolationLogDto
    {
        public string StudentEmail { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ViolationAlertPayload
    {
        public int StudentId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int SeverityScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}