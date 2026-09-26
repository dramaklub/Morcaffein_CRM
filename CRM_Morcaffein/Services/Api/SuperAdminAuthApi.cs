using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class SuperAdminAuthApi
    {
        private readonly HttpClient _http;

        public SuperAdminAuthApi()
        {
            _http = ApiClientFactory.Create();
        }

        public async Task<SuperAdminLoginResult> LoginAsync(string username, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(
                    "api/superadmin/auth/login",
                    new SuperAdminLoginRequest { Username = username, Password = password });

                if (!response.IsSuccessStatusCode)
                    return new SuperAdminLoginResult { Success = false };

                var data = await response.Content.ReadFromJsonAsync<SuperAdminLoginResponse>();
                if (data is null || string.IsNullOrWhiteSpace(data.Token))
                    return new SuperAdminLoginResult { Success = false };

                return new SuperAdminLoginResult
                {
                    Success = true,
                    Token = data.Token,
                    FullName = data.FullName,
                };
            }
            catch
            {
                return new SuperAdminLoginResult { Success = false };
            }
        }
    }

    public class SuperAdminLoginResult
    {
        public bool Success { get; init; }
        public string Token { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
    }
}