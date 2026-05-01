using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AcademicSentinel.Client.Models;
using AcademicSentinel.Client.Constants; // Added this using statement

namespace AcademicSentinel.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        public string? LastErrorMessage { get; set; }

        public AuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // Calls POST /api/auth/register
        public async Task<bool> RegisterAsync(UserRegisterDto registerData)
        {
            try
            {
                // Hits the /api/auth/register endpoint we just created 
                var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.AuthRegister, registerData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Calls POST /api/auth/login
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new UserLoginDto { Email = email, Password = password };

                // Now using ApiEndpoints.AuthLogin!
                var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.AuthLogin, loginData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UserResponseDto>();
                    if (result != null)
                    {
                        SessionManager.CurrentUser = result;
                        SessionManager.JwtToken = result.Token;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RequestPasswordResetCodeAsync(string email)
        {
            try
            {
                var request = new ForgotPasswordRequestDto { Email = email };
                var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.AuthForgotPassword, request);

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        LastErrorMessage = $"Server error ({response.StatusCode}): {errorContent}";
                    }
                    catch
                    {
                        LastErrorMessage = $"Server returned error: {response.StatusCode}";
                    }
                    return false;
                }

                LastErrorMessage = null;
                return true;
            }
            catch (HttpRequestException hre)
            {
                LastErrorMessage = $"Network error: {hre.Message}. Check if the server is running.";
                return false;
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"Unexpected error: {ex.Message}";
                return false;
            }
        }

        public async Task<string?> VerifyPasswordResetCodeAsync(string email, string code)
        {
            try
            {
                var request = new VerifyResetCodeRequestDto
                {
                    Email = email,
                    Code = code
                };

                var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.AuthVerifyResetCode, request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<VerifyResetCodeResponseDto>();
                if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.ResetToken))
                {
                    return null;
                }

                return result.ResetToken;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword, string resetToken)
        {
            try
            {
                var request = new ResetPasswordRequestDto
                {
                    Email = email,
                    NewPassword = newPassword,
                    ResetToken = resetToken
                };

                var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.AuthResetPassword, request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}