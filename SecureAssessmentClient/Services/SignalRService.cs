using Microsoft.AspNetCore.SignalR.Client;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;
using System.Diagnostics;
using System.Net.Http;

namespace SecureAssessmentClient.Services
{
    /// <summary>
    /// Real-time communication service using SignalR WebSocket connection
    /// Manages bidirectional communication with server hub for live events
    /// Handles automatic reconnection with exponential backoff
    /// </summary>
    public class SignalRService
    {
        private HubConnection _hubConnection;
        private readonly string _hubUrl;
        private string _sessionId;
        private bool _isConnected;
        private bool _isReconnecting;
        private int _reconnectionAttempts;
        private const int MAX_RECONNECTION_ATTEMPTS = 5;

        // Server-to-client events
        public event Action<string> OnSessionCountdownStarted;
        public event Action<string> OnSessionStarted;
        public event Action<string> OnSessionEnded;
        public event Action<DetectionSettings> OnDetectionSettingsUpdated;
        public event Action<string> OnDisconnected;
        public event Action<string> OnReconnected;
        public event Action<string> OnConnectionError;

        public SignalRService(string hubUrl)
        {
            _hubUrl = hubUrl ?? throw new ArgumentNullException(nameof(hubUrl));
            _isConnected = false;
            _isReconnecting = false;
            _reconnectionAttempts = 0;
        }

        /// <summary>
        /// Establishes SignalR connection to hub with Bearer token authentication
        /// Configures automatic reconnection strategy
        /// </summary>
        public async Task ConnectAsync(string token, string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new ArgumentException("Token cannot be null or empty", nameof(token));
                }

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
                }

                _sessionId = sessionId;
                Logger.Info($"Initiating SignalR connection to {_hubUrl} for session {sessionId}");

                // Build hub connection with automatic reconnection
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl, options =>
                    {
                        options.AccessTokenProvider = async () => token;
                        options.SkipNegotiation = false;
                    })
                    .WithAutomaticReconnect(new[] {
                        TimeSpan.Zero,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10)
                    })
                    .Build();

                // Register server-to-client methods
                RegisterServerMethods();

                // Register lifecycle events
                RegisterLifecycleEvents();

                // Start connection
                await _hubConnection.StartAsync();
                _isConnected = true;
                _reconnectionAttempts = 0;

                Logger.Info("SignalR connection established successfully");
            }
            catch (HttpRequestException ex)
            {
                Logger.Error("Network error establishing SignalR connection", ex);
                OnConnectionError?.Invoke($"Network error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error establishing SignalR connection", ex);
                OnConnectionError?.Invoke($"Connection error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers all server-to-client method handlers
        /// </summary>
        private void RegisterServerMethods()
        {
            // Exam countdown notification
            _hubConnection.On<string>("SessionCountdownStarted", (countdownData) =>
            {
                Logger.Info($"Countdown started: {countdownData}");
                OnSessionCountdownStarted?.Invoke(countdownData);
            });

            // Exam session start notification
            _hubConnection.On<string>("SessionStarted", (sessionData) =>
            {
                Logger.Info("Session started signal received");
                OnSessionStarted?.Invoke(sessionData);
            });

            // Exam session end notification
            _hubConnection.On<string>("SessionEnded", (sessionData) =>
            {
                Logger.Info("Session ended signal received");
                OnSessionEnded?.Invoke(sessionData);
            });

            // Detection settings update from server
            _hubConnection.On<DetectionSettings>("UpdateDetectionSettings", (settings) =>
            {
                Logger.Info("Detection settings updated from server");
                OnDetectionSettingsUpdated?.Invoke(settings);
            });

            // Server acknowledgment of event receipt
            _hubConnection.On<string>("EventAcknowledged", (eventId) =>
            {
                Logger.Debug($"Server acknowledged event: {eventId}");
            });

            // Server notification of suspicious behavior
            _hubConnection.On<string>("SuspiciousBehaviorAlert", (alertData) =>
            {
                Logger.Warn($"Suspicious behavior alert: {alertData}");
            });
        }

        /// <summary>
        /// Registers connection lifecycle event handlers
        /// </summary>
        private void RegisterLifecycleEvents()
        {
            // Handle disconnection
            _hubConnection.Closed += async (ex) =>
            {
                _isConnected = false;
                var errorMsg = ex?.Message ?? "Disconnected from server";
                Logger.Warn($"SignalR connection closed: {errorMsg}");
                OnDisconnected?.Invoke(errorMsg);

                // Attempt reconnection
                await AttemptReconnectionAsync();
            };

            // Handle successful reconnection
            _hubConnection.Reconnected += async (connectionId) =>
            {
                _isConnected = true;
                _reconnectionAttempts = 0;
                Logger.Info($"SignalR reconnected with connection ID: {connectionId}");
                OnReconnected?.Invoke(connectionId ?? "Unknown");
            };

            // Handle reconnection failure
            _hubConnection.Reconnecting += async (ex) =>
            {
                _isReconnecting = true;
                Logger.Warn($"SignalR attempting reconnection... (Attempt {_reconnectionAttempts + 1}/{MAX_RECONNECTION_ATTEMPTS})");
            };
        }

        /// <summary>
        /// Attempts to reconnect with exponential backoff
        /// </summary>
        private async Task AttemptReconnectionAsync()
        {
            try
            {
                _isReconnecting = true;

                while (_reconnectionAttempts < MAX_RECONNECTION_ATTEMPTS && !_isConnected)
                {
                    _reconnectionAttempts++;
                    int delayMs = (int)Math.Pow(2, _reconnectionAttempts) * 1000; // Exponential backoff
                    
                    Logger.Info($"Reconnection attempt {_reconnectionAttempts}/{MAX_RECONNECTION_ATTEMPTS} in {delayMs}ms");
                    
                    await Task.Delay(delayMs);

                    try
                    {
                        if (_hubConnection?.State == HubConnectionState.Disconnected)
                        {
                            await _hubConnection.StartAsync();
                            _isConnected = true;
                            _reconnectionAttempts = 0;
                            _isReconnecting = false;
                            Logger.Info("Successfully reconnected to SignalR hub");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Reconnection attempt {_reconnectionAttempts} failed: {ex.Message}");
                    }
                }

                if (_reconnectionAttempts >= MAX_RECONNECTION_ATTEMPTS && !_isConnected)
                {
                    Logger.Error("Maximum reconnection attempts reached");
                    OnConnectionError?.Invoke("Unable to reconnect to server. Please restart the exam.");
                    _isReconnecting = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error during reconnection attempt", ex);
                _isReconnecting = false;
            }
        }

        /// <summary>
        /// Sends a single monitoring event to server
        /// </summary>
        public async Task<bool> SendMonitoringEventAsync(MonitoringEvent monitoringEvent)
        {
            try
            {
                if (!IsConnected)
                {
                    Logger.Warn("Cannot send event: SignalR not connected");
                    return false;
                }

                if (monitoringEvent == null)
                {
                    Logger.Error("Cannot send null monitoring event");
                    return false;
                }

                // Set session ID if not already set
                if (string.IsNullOrEmpty(monitoringEvent.SessionId))
                {
                    monitoringEvent.SessionId = _sessionId;
                }

                // Send to server
                await _hubConnection.SendAsync("SendMonitoringEvent", monitoringEvent);
                
                Logger.Debug($"Sent monitoring event: {monitoringEvent.EventType}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error sending monitoring event via SignalR", ex);
                return false;
            }
        }

        /// <summary>
        /// Sends batch of monitoring events to server
        /// More efficient than individual sends for multiple events
        /// </summary>
        public async Task<bool> SendBatchMonitoringEventsAsync(List<MonitoringEvent> events)
        {
            try
            {
                if (!IsConnected)
                {
                    Logger.Warn("Cannot send batch: SignalR not connected");
                    return false;
                }

                if (events == null || events.Count == 0)
                {
                    Logger.Warn("Attempted to send empty event batch");
                    return false;
                }

                // Set session ID for all events
                foreach (var evt in events)
                {
                    if (string.IsNullOrEmpty(evt.SessionId))
                    {
                        evt.SessionId = _sessionId;
                    }
                }

                // Send batch to server
                await _hubConnection.SendAsync("SendBatchMonitoringEvents", events);
                
                Logger.Info($"Sent batch of {events.Count} monitoring events");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error sending batch monitoring events via SignalR", ex);
                return false;
            }
        }

        /// <summary>
        /// Requests latest detection settings from server
        /// Useful for updating monitoring configuration mid-session
        /// </summary>
        public async Task<bool> RequestDetectionSettingsAsync(string roomId)
        {
            try
            {
                if (!IsConnected)
                {
                    Logger.Warn("Cannot request settings: SignalR not connected");
                    return false;
                }

                await _hubConnection.SendAsync("RequestDetectionSettings", roomId);
                Logger.Info($"Requested detection settings for room {roomId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error requesting detection settings", ex);
                return false;
            }
        }

        /// <summary>
        /// Notifies server that exam session is ending
        /// Allows server to perform cleanup and finalization
        /// </summary>
        public async Task<bool> NotifySessionEndingAsync()
        {
            try
            {
                if (!IsConnected)
                {
                    Logger.Warn("Cannot notify session end: SignalR not connected");
                    return false;
                }

                await _hubConnection.SendAsync("NotifySessionEnding", _sessionId);
                Logger.Info("Notified server of session ending");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error notifying session end", ex);
                return false;
            }
        }

        /// <summary>
        /// Sends heartbeat to server to maintain connection and signal liveness
        /// Useful for detecting dead connections
        /// </summary>
        public async Task<bool> SendHeartbeatAsync()
        {
            try
            {
                if (!IsConnected)
                {
                    return false;
                }

                await _hubConnection.SendAsync("Heartbeat", _sessionId, DateTime.UtcNow);
                Logger.Debug("Heartbeat sent to server");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error sending heartbeat", ex);
                return false;
            }
        }

        /// <summary>
        /// Disconnects from SignalR hub gracefully
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                if (_hubConnection != null)
                {
                    Logger.Info("Disconnecting from SignalR hub");
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _isConnected = false;
                    Logger.Info("SignalR disconnection complete");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error disconnecting from SignalR", ex);
            }
        }

        /// <summary>
        /// Gets current connection state
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected && _hubConnection?.State == HubConnectionState.Connected; }
        }

        /// <summary>
        /// Gets whether reconnection is in progress
        /// </summary>
        public bool IsReconnecting
        {
            get { return _isReconnecting; }
        }

        /// <summary>
        /// Gets current session ID
        /// </summary>
        public string SessionId
        {
            get { return _sessionId; }
        }

        /// <summary>
        /// Gets hub connection state
        /// </summary>
        public HubConnectionState? ConnectionState
        {
            get { return _hubConnection?.State; }
        }

        /// <summary>
        /// Authenticates with the server and retrieves JWT token
        /// Must be called before connecting to SignalR hub
        /// </summary>
        public async Task<string> AuthenticateAsync(string serverBaseUrl, string email, string password)
        {
            try
            {
                Logger.Info($"Authenticating user: {email}");

                // For development: ignore SSL certificate validation
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (var httpClient = new HttpClient(handler))
                {
                    var loginRequest = new { email, password };
                    var json = System.Text.Json.JsonSerializer.Serialize(loginRequest);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync($"{serverBaseUrl}/api/auth/login", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);

                        var token = jsonDoc.RootElement.GetProperty("token").GetString();
                        var studentId = jsonDoc.RootElement.GetProperty("id").GetInt32();

                        _sessionId = studentId.ToString();
                        Logger.Info($"✅ Authentication successful - Token: {token.Substring(0, 20)}...");
                        return token;
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        Logger.Error($"❌ Authentication failed: {response.StatusCode} - {errorBody}");
                        throw new Exception($"Authentication failed: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("❌ Authentication error", ex);
                throw;
            }
        }

        /// <summary>
        /// Joins an exam room on the server
        /// Must be called after successful SignalR connection
        /// </summary>
        public async Task<bool> JoinExamAsync(int roomId)
        {
            try
            {
                if (!IsConnected)
                {
                    Logger.Warn("Cannot join room: SignalR not connected");
                    return false;
                }

                Logger.Info($"Joining exam room: {roomId}");

                // Call hub method: JoinLiveExam
                await _hubConnection.InvokeAsync("JoinLiveExam", roomId);

                Logger.Info($"✅ Successfully joined room {roomId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Error joining exam room: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Sends a monitoring event for a specific exam
        /// Used for real-time detection transmission
        /// </summary>
        public async Task<bool> SendExamMonitoringEventAsync(int roomId, int studentId, string eventType, 
            int severityScore, string description)
        {
            try
            {
                if (!IsConnected)
                {
                    Logger.Warn("Cannot send exam event: SignalR not connected");
                    return false;
                }

                var eventData = new
                {
                    eventType = eventType,
                    severityScore = severityScore,
                    description = description,
                    timestamp = DateTime.UtcNow
                };

                Logger.Debug($"Sending exam monitoring event: {eventType} (Severity: {severityScore})");

                // Call hub method: SendMonitoringEvent
                await _hubConnection.InvokeAsync("SendMonitoringEvent", roomId, studentId, eventData);

                Logger.Debug($"✅ Exam event transmitted: {eventType}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("❌ Error sending exam monitoring event", ex);
                return false;
            }
        }
    }
}
