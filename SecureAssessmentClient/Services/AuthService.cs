using SecureAssessmentClient.Models.Authentication;
using SecureAssessmentClient.Utilities;
using System.Net.Http;

namespace SecureAssessmentClient.Services
{
    /// <summary>
    /// Authentication service handling user login/logout and token management
    /// Provides higher-level authentication operations wrapping ApiService
    /// </summary>
    public class AuthService
    {
        private readonly ApiService _apiService;
        private UserInfo _currentUser;

        public event Action<UserInfo> OnUserAuthenticated;
        public event Action OnUserLoggedOut;

        public AuthService(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _currentUser = null;
        }

        /// <summary>
        /// Authenticates user with email and password
        /// Returns tuple with (Success, UserId, Message)
        /// </summary>
        public async Task<(bool Success, string UserId, string Message)> LoginAsync(string email, string password)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(email))
                {
                    return (false, null, "Email cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return (false, null, "Password cannot be empty");
                }

                // Create login request
                var request = new LoginRequest 
                { 
                    Email = email.Trim(), 
                    Password = password 
                };

                Logger.Info($"Attempting login for user {email}");

                // Call API
                var response = await _apiService.LoginAsync(request);

                // Check response
                if (!response.Success)
                {
                    Logger.Warn($"Login failed for {email}: {response.Message}");
                    return (false, null, response.Message ?? "Login failed");
                }

                // Validate response data
                if (response.User == null)
                {
                    Logger.Error($"Login response missing user data for {email}");
                    return (false, null, "Invalid login response");
                }

                if (response.Token == null)
                {
                    Logger.Error($"Login response missing token for {email}");
                    return (false, null, "Invalid login response - no token received");
                }

                // Store user info and raise event
                _currentUser = response.User;
                OnUserAuthenticated?.Invoke(_currentUser);

                Logger.Info($"User {email} (ID: {response.User.Id}, Role: {response.User.Role}) logged in successfully");

                return (true, response.User.Id, "Login successful");
            }
            catch (HttpRequestException ex)
            {
                Logger.Error("Network error during login attempt", ex);
                return (false, null, "Network error. Please check your connection.");
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error during login", ex);
                return (false, null, "An unexpected error occurred during login.");
            }
        }

        /// <summary>
        /// Logs out current user and clears authentication token
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                Logger.Info($"Logging out user {_currentUser?.Email ?? "unknown"}");

                // Clear token from storage
                TokenManager.ClearToken();

                // Reset user info
                _currentUser = null;

                // Raise event
                OnUserLoggedOut?.Invoke();

                Logger.Info("User logged out successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during logout", ex);
            }
        }

        /// <summary>
        /// Checks if user is currently authenticated
        /// </summary>
        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(TokenManager.GetToken());
        }

        /// <summary>
        /// Gets stored authentication token
        /// </summary>
        public string GetStoredToken()
        {
            return TokenManager.GetToken();
        }

        /// <summary>
        /// Gets currently authenticated user information
        /// </summary>
        public UserInfo GetCurrentUser()
        {
            return _currentUser;
        }

        /// <summary>
        /// Gets user ID of currently authenticated user
        /// </summary>
        public string GetCurrentUserId()
        {
            return _currentUser?.Id;
        }

        /// <summary>
        /// Gets user email of currently authenticated user
        /// </summary>
        public string GetCurrentUserEmail()
        {
            return _currentUser?.Email;
        }

        /// <summary>
        /// Gets user role of currently authenticated user
        /// </summary>
        public string GetCurrentUserRole()
        {
            return _currentUser?.Role;
        }

        /// <summary>
        /// Validates token is still valid
        /// In real implementation, would call server endpoint
        /// </summary>
        public async Task<bool> ValidateTokenAsync()
        {
            try
            {
                var token = TokenManager.GetToken();
                
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                // Token exists and is retrievable
                Logger.Info("Token validation successful");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating token", ex);
                return false;
            }
        }

        /// <summary>
        /// Re-initializes user state from stored token (useful on app startup)
        /// </summary>
        public bool InitializeFromStoredToken()
        {
            try
            {
                var token = TokenManager.GetToken();
                
                if (string.IsNullOrEmpty(token))
                {
                    Logger.Info("No stored token found");
                    _currentUser = null;
                    return false;
                }

                // Set the token for authenticated requests
                _apiService.SetAuthToken(token);
                
                Logger.Info("Initialized from stored token");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing from stored token", ex);
                TokenManager.ClearToken();
                return false;
            }
        }
    }
}
