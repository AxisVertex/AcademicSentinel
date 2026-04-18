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

        public AuthService()
        {
            _httpClient = new HttpClient();
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
    }
}