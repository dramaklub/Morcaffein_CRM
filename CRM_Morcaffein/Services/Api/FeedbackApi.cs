using System.Net;
using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class FeedbackApi
    {
        private readonly HttpClient _http;

        public FeedbackApi()
        {
            _http = ApiClientFactory.Create();
        }

        // GET api/tenant/{companyId}/feedback?search=...&page=...&pageSize=...&status=...
        // status is appended at the END of the signature so existing positional callers
        // (search, page, pageSize) keep binding to the same parameters.
        public async Task<PagedResponse<FeedbackResponseDto>> GetTicketsAsync(
            string? search = null, int page = 1, int pageSize = 25, string? status = null)
        {
            var url = $"api/tenant/{ApiConfig.CompanyId}/feedback?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(status))
                url += $"&status={Uri.EscapeDataString(status)}";

            var response = await _http.GetAsync(url);
            await EnsureSuccess(response);

            var result = await response.Content.ReadFromJsonAsync<PagedResponse<FeedbackResponseDto>>();
            return result ?? new PagedResponse<FeedbackResponseDto>();
        }

        // GET api/tenant/{companyId}/feedback/{id}
        public async Task<FeedbackResponseDto?> GetTicketAsync(int ticketId)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/feedback/{ticketId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<FeedbackResponseDto>();
        }

        // POST api/tenant/{companyId}/feedback
        public async Task<FeedbackResponseDto> LogFeedbackAsync(LogFeedbackRequest request)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/feedback", request);
            await EnsureSuccess(response);

            var result = await response.Content.ReadFromJsonAsync<FeedbackResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty feedback ticket.");
        }

        // POST api/tenant/{companyId}/feedback/{id}/cancel
        public async Task<FeedbackResponseDto> CancelFeedbackAsync(int ticketId, int cancelledBy, string? reason = null)
        {
            var body = new CancelFeedbackRequest { CancelledBy = cancelledBy, Reason = reason };

            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/feedback/{ticketId}/cancel", body);
            await EnsureSuccess(response);

            var result = await response.Content.ReadFromJsonAsync<FeedbackResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty feedback ticket.");
        }

        // POST api/tenant/{companyId}/feedback/{id}/resolve
        public async Task<FeedbackResponseDto> ResolveFeedbackAsync(
            int ticketId, int decidedBy, int bonusPoints, string? notes)
        {
            var body = new ResolveFeedbackRequest
            {
                DecidedBy = decidedBy,
                BonusPoints = bonusPoints,
                Notes = notes,
            };

            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/feedback/{ticketId}/resolve", body);
            await EnsureSuccess(response);

            var result = await response.Content.ReadFromJsonAsync<FeedbackResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty feedback ticket.");
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