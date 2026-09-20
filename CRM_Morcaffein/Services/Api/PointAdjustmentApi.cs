using System.Net;
using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class PointAdjustmentApi
    {
        private readonly HttpClient _http;

        public PointAdjustmentApi()
        {
            _http = ApiClientFactory.Create();
        }

        public async Task<List<PointAdjustmentResponseDto>> GetPendingAsync(int? customerId = null)
        {
            var url = $"api/tenant/{ApiConfig.CompanyId}/point-adjustments/pending";
            if (customerId.HasValue)
                url += $"?customerId={customerId.Value}";

            var response = await _http.GetAsync(url);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<PointAdjustmentResponseDto>>();
            return result ?? new List<PointAdjustmentResponseDto>();
        }

        public async Task<PointAdjustmentResponseDto> RequestAsync(
            int customerId, int pointsChange, string reason, int requestedBy)
        {
            var request = new CreatePointAdjustmentDto
            {
                CustomerId = customerId,
                PointsChange = pointsChange,
                Reason = reason,
                RequestedBy = requestedBy,
            };
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/point-adjustments", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<PointAdjustmentResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty response.");
        }

        public async Task<PointAdjustmentResponseDto> ApproveAsync(int requestId, int decidedBy, string? notes = null)
        {
            var body = new DecidePointAdjustmentDto { DecidedBy = decidedBy, Notes = notes };
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/point-adjustments/{requestId}/approve", body);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<PointAdjustmentResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty response.");
        }

        public async Task<PointAdjustmentResponseDto> RejectAsync(int requestId, int decidedBy, string reason)
        {
            var body = new DecidePointAdjustmentDto { DecidedBy = decidedBy, Notes = reason };
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/point-adjustments/{requestId}/reject", body);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<PointAdjustmentResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty response.");
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