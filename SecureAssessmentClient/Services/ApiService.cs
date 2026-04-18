using SecureAssessmentClient.Models.Authentication;
using SecureAssessmentClient.Models.Room;
using SecureAssessmentClient.Models.Monitoring;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Sets the Bearer token for authenticated API requests
        /// </summary>
        public void SetAuthToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            Logger.Info("Auth token set for API requests");
        }

        /// <summary>
        /// Authenticates user with email and password
        /// </summary>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Login failed: {response.StatusCode} - {responseContent}");
                    return new LoginResponse 
                    { 
                        Success = false, 
                        Message = $"Login failed: {response.StatusCode}" 
                    };
                }

                var result = JsonSerializer.Deserialize<LoginResponse>(responseContent);
                
                if (result?.Success == true && result?.Token != null)
                {
                    TokenManager.SaveToken(result.Token.AccessToken);
                    SetAuthToken(result.Token.AccessToken);
                    Logger.Info($"User {request.Email} authenticated successfully");
                }

                return result ?? new LoginResponse { Success = false, Message = "Invalid response format" };
            }
            catch (HttpRequestException ex)
            {
                Logger.Error("Network error during login", ex);
                return new LoginResponse { Success = false, Message = "Network error. Please check your connection." };
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error during login", ex);
                return new LoginResponse { Success = false, Message = "An unexpected error occurred during login." };
            }
        }

        /// <summary>
        /// Retrieves list of available rooms for current user
        /// </summary>
        public async Task<List<RoomDto>> GetAvailableRoomsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/rooms/my");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var rooms = JsonSerializer.Deserialize<List<RoomDto>>(content) ?? new List<RoomDto>();
                
                Logger.Info($"Retrieved {rooms.Count} available rooms");
                return rooms;
            }
            catch (HttpRequestException ex)
            {
                Logger.Error("Failed to get available rooms", ex);
                return new List<RoomDto>();
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error getting available rooms", ex);
                return new List<RoomDto>();
            }
        }

        /// <summary>
        /// Joins a specific exam room
        /// </summary>
        public async Task<bool> JoinRoomAsync(string roomId)
        {
            try
            {
                var request = new JoinRoomRequest { RoomId = roomId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/join", content);
                
                if (response.IsSuccessStatusCode)
                {
                    Logger.Info($"Successfully joined room {roomId}");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.Warn($"Failed to join room {roomId}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Network error joining room {roomId}", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error joining room {roomId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Retrieves detailed information about a specific room
        /// </summary>
        public async Task<RoomDto> GetRoomDetailsAsync(string roomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/rooms/{roomId}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var room = JsonSerializer.Deserialize<RoomDto>(content);
                
                Logger.Info($"Retrieved details for room {roomId}");
                return room;
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Network error getting room details for {roomId}", ex);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error getting room details for {roomId}", ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves detection settings for a specific room
        /// Used to configure monitoring behavior during exam
        /// </summary>
        public async Task<DetectionSettings> GetDetectionSettingsAsync(string roomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/rooms/{roomId}/settings");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var settings = JsonSerializer.Deserialize<DetectionSettings>(content);
                
                Logger.Info($"Retrieved detection settings for room {roomId}");
                return settings;
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Network error getting detection settings for {roomId}", ex);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error getting detection settings for {roomId}", ex);
                return null;
            }
        }

        /// <summary>
        /// Sends monitoring event to server via HTTP
        /// </summary>
        public async Task<bool> SendMonitoringEventAsync(MonitoringEvent monitoringEvent)
        {
            try
            {
                var json = JsonSerializer.Serialize(monitoringEvent);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/events/log", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warn($"Failed to send monitoring event: {response.StatusCode}");
                    return false;
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                Logger.Error("Network error sending monitoring event", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error sending monitoring event", ex);
                return false;
            }
        }

        /// <summary>
        /// Batch sends multiple monitoring events to server
        /// </summary>
        public async Task<bool> SendBatchMonitoringEventsAsync(List<MonitoringEvent> events)
        {
            try
            {
                var json = JsonSerializer.Serialize(events);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/events/batch", content);
                
                if (response.IsSuccessStatusCode)
                {
                    Logger.Info($"Batch sent {events.Count} monitoring events");
                    return true;
                }

                Logger.Warn($"Failed to send batch events: {response.StatusCode}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                Logger.Error("Network error sending batch monitoring events", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error sending batch monitoring events", ex);
                return false;
            }
        }

        /// <summary>
        /// Notifies server that student session has ended
        /// </summary>
        public async Task<bool> EndSessionAsync(string roomId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/end-session", null);
                
                if (response.IsSuccessStatusCode)
                {
                    Logger.Info($"Session ended for room {roomId}");
                    return true;
                }

                Logger.Warn($"Failed to end session for room {roomId}: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error ending session for room {roomId}", ex);
                return false;
            }
        }
    }
}
