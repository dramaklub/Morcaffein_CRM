using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    /// <summary>
    /// The server rejecting a checkout (missing B1T1 item, campaign no longer active,
    /// a size that vanished mid-sale, etc.) is an ordinary, expected outcome — not a bug.
    /// CompleteOrderAsync reports it through this result instead of throwing, so the
    /// checkout path never raises an exception for something the cashier can just fix
    /// and retry.
    /// </summary>
    public class OrderCompletionResult
    {
        public bool Success { get; init; }
        public OrderResponseDto? Order { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public class OrderApi
    {
        private readonly HttpClient _http;

        public OrderApi()
        {
            _http = ApiClientFactory.Create();
        }

        // POST api/tenant/{companyId}/orders
        // Sends the completed cart. The server recomputes every total from the raw
        // product/size/modifier IDs and returns the canonical OrderResponseDto.
        public async Task<OrderCompletionResult> CompleteOrderAsync(CompleteOrderRequest request)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/orders", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var message = TryExtractMessage(body) ?? $"API returned {(int)response.StatusCode}.";
                return new OrderCompletionResult { Success = false, ErrorMessage = message };
            }

            var result = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
            if (result is null)
                return new OrderCompletionResult { Success = false, ErrorMessage = "API returned an empty order." };

            return new OrderCompletionResult { Success = true, Order = result };
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