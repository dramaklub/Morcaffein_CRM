using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// UI-facing service for branch operations. Wraps BranchApi and exposes
    /// a friendly surface the views can call — same shape as the other
    /// services in this folder (EmployeeService, CustomerService, etc.).
    ///
    /// Read methods are open; write methods are Admin-only on the server,
    /// so a non-admin calling them will get a 403 which surfaces as an
    /// InvalidOperationException with the API's message.
    /// </summary>
    public class BranchService
    {
        private readonly BranchApi _api;

        public BranchService()
        {
            _api = new BranchApi();
        }

        /// <summary>Active branches only. Used by the topbar selector and dropdowns.</summary>
        public Task<List<BranchResponseDto>> GetActiveAsync()
            => _api.GetBranchesAsync();

        /// <summary>All branches (active + inactive). Admin-only.</summary>
        public Task<List<BranchResponseDto>> GetAllAsync()
            => _api.GetAllBranchesAsync();

        public Task<BranchResponseDto> CreateAsync(string branchName, string address)
            => _api.CreateAsync(new CreateBranchRequest
            {
                BranchName = branchName,
                Address = address,
            });

        public Task<BranchResponseDto> UpdateAsync(int branchId, string branchName, string address)
            => _api.UpdateAsync(branchId, new UpdateBranchRequest
            {
                BranchName = branchName,
                Address = address,
            });

        public Task<BranchResponseDto> DeactivateAsync(int branchId)
            => _api.DeactivateAsync(branchId);

        public Task<BranchResponseDto> ReactivateAsync(int branchId)
            => _api.ReactivateAsync(branchId);
    }
}