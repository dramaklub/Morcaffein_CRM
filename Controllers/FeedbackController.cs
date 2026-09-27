using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.Controllers
{
    /// <summary>
    /// UI controller for the Feedback tab (WinForms). Pure coordination: the View calls
    /// these methods, they forward to the API. All business rules live on the API side.
    /// </summary>
    public class FeedbackController
    {
        private readonly FeedbackApi _feedbackApi;
        private readonly CustomerApi _customerApi;

        public FeedbackController()
        {
            _feedbackApi = new FeedbackApi();
            _customerApi = new CustomerApi();
        }

        // ---- Feedback tickets ----

        public Task<PagedResponse<FeedbackResponseDto>> GetTicketsAsync(
            string searchText, int page = 1, int pageSize = 25, string? status = null)
            => _feedbackApi.GetTicketsAsync(searchText, page, pageSize, status);

        public Task<FeedbackResponseDto> LogFeedbackAsync(
            int customerId, int loggedByEmployeeId, int rating, string category, string staffNotes)
        {
            var request = new LogFeedbackRequest
            {
                CustomerId = customerId,
                LoggedBy = loggedByEmployeeId,
                Rating = rating,
                Category = category,
                StaffNotes = staffNotes,
                BranchId = Session.EmployeeSession.Current?.CurrentBranchId
             ?? Session.EmployeeSession.Current?.Employee.BranchId
             ?? 1,
            };
            return _feedbackApi.LogFeedbackAsync(request);
        }

        public Task<FeedbackResponseDto> CancelTicketAsync(int ticketId, int cancelledByEmployeeId, string? reason = null)
            => _feedbackApi.CancelFeedbackAsync(ticketId, cancelledByEmployeeId, reason);

        // ---- Customer lookup (for the Log Feedback modal's picker) ----

        public async Task<List<CustomerResponseDto>> SearchCustomersAsync(string query)
        {
            // The Log Feedback modal's customer picker doesn't page — it wants a flat list
            // of matches. Request a large page size and unwrap.
            var response = await _customerApi.GetCustomersAsync(query, page: 1, pageSize: 200);
            return response.Items;
        }
        public Task<FeedbackResponseDto> ResolveAsync(
            int ticketId, int decidedByEmployeeId, int bonusPoints, string? notes)
            => _feedbackApi.ResolveFeedbackAsync(ticketId, decidedByEmployeeId, bonusPoints, notes);
    }
}