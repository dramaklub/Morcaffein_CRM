using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// UI controller for the Customers tab (WinForms). Pure coordination: the View calls
    /// these methods, they forward to the API. All business rules live on the API side.
    /// </summary>
    public class CustomerService
    {
        private readonly CustomerApi _customers;
        private readonly PointAdjustmentApi _adjustments;

        public CustomerService()
        {
            _customers = new CustomerApi();
            _adjustments = new PointAdjustmentApi();
        }

        // ---- Customers ----

        public Task<PagedResponse<CustomerResponseDto>> SearchAsync(
            string searchText, int page = 1, int pageSize = 25, string? activeFilter = null)
            => _customers.GetCustomersAsync(searchText, page, pageSize, activeFilter);

        public Task<CustomerResponseDto?> FindByPhoneOrIdAsync(string query)
            => _customers.FindCustomerAsync(query);

        public Task<CustomerResponseDto?> GetByIdAsync(int customerId)
            => _customers.GetCustomerAsync(customerId);

        public Task<CustomerResponseDto> RegisterAsync(
            string firstName, string lastName, string phone, string? email, DateTime? birthday)
        {
            var request = new CreateCustomerDto
            {
                FirstName = firstName,
                LastName = lastName,
                ContactNumber = phone,
                Email = email,
                Birthday = birthday,
            };
            return _customers.CreateCustomerAsync(request);
        }

        public Task<CustomerResponseDto> UpdateCustomerAsync(
            int customerId, string firstName, string lastName, string phone,
            string? email, DateTime? birthday)
                {
                    var request = new UpdateCustomerDto
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        ContactNumber = phone,
                        Email = email,
                        Birthday = birthday,
                    };
                    return _customers.UpdateCustomerAsync(customerId, request);
                }
        /// <summary>
        /// Soft-deletes a customer — the API's DELETE endpoint sets IsActive = false,
        /// preserving the row (order history, ledger, points) for later reactivation.
        /// Manager only; UI gates this.
        /// </summary>
        public Task DeactivateCustomerAsync(int customerId)
            => _customers.DeleteCustomerAsync(customerId);

        /// <summary>
        /// Reverses a soft delete — sets IsActive = true. Manager only; UI gates this.
        /// </summary>
        public Task ReactivateCustomerAsync(int customerId)
            => _customers.ReactivateCustomerAsync(customerId);

        public Task<List<OrderHistoryDto>> GetOrderHistoryAsync(int customerId)
            => _customers.GetOrderHistoryAsync(customerId);

        public Task<List<PointLedgerResponseDto>> GetLedgerAsync(int customerId)
            => _customers.GetLedgerAsync(customerId);

        // ---- Point adjustments ----

        public Task<List<PointAdjustmentResponseDto>> GetPendingAdjustmentsAsync(int customerId)
            => _adjustments.GetPendingAsync(customerId);

        public Task<PointAdjustmentResponseDto> RequestAdjustmentAsync(
            int customerId, int pointsChange, string reason, int requestedByEmployeeId)
            => _adjustments.RequestAsync(customerId, pointsChange, reason, requestedByEmployeeId);

        public Task<PointAdjustmentResponseDto> ApproveAdjustmentAsync(
            int requestId, int approvedByEmployeeId, string? notes = null)
            => _adjustments.ApproveAsync(requestId, approvedByEmployeeId, notes);

        public Task<PointAdjustmentResponseDto> RejectAdjustmentAsync(
            int requestId, int decidedByEmployeeId, string notes)
            => _adjustments.RejectAsync(requestId, decidedByEmployeeId, notes);
    }
}