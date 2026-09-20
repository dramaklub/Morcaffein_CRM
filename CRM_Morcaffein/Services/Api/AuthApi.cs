using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class LoginResult
    {
        public bool Success { get; init; }
        public LoginResponse? Data { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public class AuthApi
    {
        private readonly HttpClient _http;

        public AuthApi()
        {
            _http = new HttpClient { BaseAddress = new Uri(ApiConfig.BaseUrl) };
        }

        /// <summary>
        /// POSTs credentials to the tenant's login endpoint. NEVER throws for normal
        /// failure cases (wrong password, account locked, API unreachable) — those
        /// come back as a LoginResult with Success=false and an ErrorMessage.
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password,
            };

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsJsonAsync(
                    $"api/tenant/{ApiConfig.CompanyId}/auth/login", request);
            }
            catch (HttpRequestException ex)
            {
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = $"Cannot reach the server. Is the API running?\n\n{ex.Message}",
                };
            }
            catch (TaskCanceledException)
            {
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = "The request timed out. Please try again.",
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var message = TryExtractMessage(body) ?? $"Login failed ({(int)response.StatusCode}).";
                return new LoginResult { Success = false, ErrorMessage = message };
            }

            LoginResponse? data;
            try
            {
                data = await response.Content.ReadFromJsonAsync<LoginResponse>();
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected response from server: {ex.Message}",
                };
            }

            if (data is null)
                return new LoginResult { Success = false, ErrorMessage = "Login returned an empty response." };

            return new LoginResult { Success = true, Data = data };
        }

        private static string? TryExtractMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;

            const string marker = "\"message\":\"";
            var idx = body.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return null;

            var start = idx + marker.Length;
            var end = body.IndexOf('"', start);
            if (end < 0) return null;

            return body.Substring(start, end - start);
        }
    }
}