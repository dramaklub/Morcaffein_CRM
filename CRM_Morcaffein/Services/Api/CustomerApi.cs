using System.Net;
using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class CustomerApi
    {
        private readonly HttpClient _http;

        public CustomerApi()
        {
            _http = ApiClientFactory.Create();
        }

        public async Task<PagedResponse<CustomerResponseDto>> GetCustomersAsync(
            string? search = null, int page = 1, int pageSize = 25, string? activeFilter = null)
        {
            var url = $"api/tenant/{ApiConfig.CompanyId}/customers?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(activeFilter))
                url += $"&activeFilter={Uri.EscapeDataString(activeFilter)}";

            var response = await _http.GetAsync(url);
            await EnsureSuccess(response);

            var result = await response.Content.ReadFromJsonAsync<PagedResponse<CustomerResponseDto>>();
            return result ?? new PagedResponse<CustomerResponseDto>();
        }

        public async Task<CustomerResponseDto?> GetCustomerAsync(int id)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/customers/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<CustomerResponseDto>();
        }

        public async Task<CustomerResponseDto?> FindCustomerAsync(string query)
        {
            var response = await _http.GetAsync(
                $"api/tenant/{ApiConfig.CompanyId}/customers/find?query={Uri.EscapeDataString(query)}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<CustomerResponseDto>();
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto request)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/customers", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CustomerResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty customer.");
        }

        public async Task<CustomerResponseDto> UpdateCustomerAsync(int id, UpdateCustomerDto request)
        {
            var response = await _http.PutAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/customers/{id}", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CustomerResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty customer.");
        }

        public async Task DeleteCustomerAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/tenant/{ApiConfig.CompanyId}/customers/{id}");
            await EnsureSuccess(response);
        }
        public async Task ReactivateCustomerAsync(int id)
        {
            var response = await _http.PostAsync(
                $"api/tenant/{ApiConfig.CompanyId}/customers/{id}/reactivate", null);
            await EnsureSuccess(response);
        }
        public async Task<List<PointLedgerResponseDto>> GetLedgerAsync(int customerId)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/customers/{customerId}/ledger");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<PointLedgerResponseDto>>();
            return result ?? new List<PointLedgerResponseDto>();
        }

        public async Task<List<OrderHistoryDto>> GetOrderHistoryAsync(int customerId)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/customers/{customerId}/orders");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<OrderHistoryDto>>();
            return result ?? new List<OrderHistoryDto>();
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