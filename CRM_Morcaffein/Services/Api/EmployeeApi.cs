using System.Net;
using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class EmployeeApi
    {
        private readonly HttpClient _http;

        public EmployeeApi()
        {
            _http = ApiClientFactory.Create();
        }

        public async Task<PagedResponse<EmployeeResponseDto>> GetEmployeesAsync(
            string? search = null, int page = 1, int pageSize = 25)
        {
            var url = $"api/tenant/{ApiConfig.CompanyId}/employees?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var response = await _http.GetAsync(url);
            await EnsureSuccess(response);

            var result = await response.Content.ReadFromJsonAsync<PagedResponse<EmployeeResponseDto>>();
            return result ?? new PagedResponse<EmployeeResponseDto>();
        }

        public async Task<EmployeeResponseDto?> GetEmployeeAsync(int employeeId)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/employees/{employeeId}");
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<EmployeeResponseDto>();
        }

        public async Task<EmployeeResponseDto> CreateEmployeeAsync(CreateEmployeeRequest request)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/employees", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<EmployeeResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty employee.");
        }

        public async Task<EmployeeResponseDto> UpdateEmployeeAsync(int employeeId, UpdateEmployeeRequest request)
        {
            var response = await _http.PutAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/employees/{employeeId}", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<EmployeeResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty employee.");
        }

        public async Task DeactivateEmployeeAsync(int employeeId)
        {
            var response = await _http.PostAsync(
                $"api/tenant/{ApiConfig.CompanyId}/employees/{employeeId}/deactivate", null);
            await EnsureSuccess(response);
        }

        public async Task ReactivateEmployeeAsync(int employeeId)
        {
            var response = await _http.PostAsync(
                $"api/tenant/{ApiConfig.CompanyId}/employees/{employeeId}/reactivate", null);
            await EnsureSuccess(response);
        }

        public async Task ResetPasswordAsync(int employeeId, string newPassword)
        {
            var body = new ResetPasswordRequest { NewPassword = newPassword };
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/employees/{employeeId}/reset-password", body);
            await EnsureSuccess(response);
        }

        private static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            var message = TryExtractMessage(body) ?? $"API returned {(int)response.StatusCode}.";
            throw new InvalidOperationException(message);
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