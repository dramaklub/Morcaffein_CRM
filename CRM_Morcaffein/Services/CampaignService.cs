using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// UI service for the Campaign tab. Pure coordination: the view calls
    /// these methods, they forward to the API. All business rules live on
    /// the API side.
    ///
    /// Branch scoping: the caller (FrmCampaignDetail) supplies the branchId
    /// for create/update. The value is either a specific branch ID (branch-
    /// scoped campaign) or null (company-wide campaign). The API enforces
    /// the rules — a pinned Manager can only submit their own branch, and
    /// only an all-branch Manager can submit null.
    ///
    /// Also responsible for invalidating the POS's CampaignCache whenever a
    /// Manager creates, edits, activates, or deactivates a campaign — otherwise
    /// the POS would keep serving stale active-campaign lists until restart.
    /// </summary>
    public class CampaignService
    {
        private readonly CampaignApi _api;

        public CampaignService()
        {
            _api = new CampaignApi();
        }

        // ---- List ----

        /// <summary>Every campaign regardless of status — the Manager's list.</summary>
        public Task<List<CampaignResponseDto>> GetAllAsync()
            => _api.GetForManagementAsync();

        /// <summary>Currently-active campaigns only — used by the POS.</summary>
        public Task<List<CampaignResponseDto>> GetActiveAsync()
            => _api.GetActiveCampaignsAsync();

        public Task<CampaignResponseDto?> GetByIdAsync(int campaignId)
            => _api.GetByIdAsync(campaignId);

        // ---- Mutations ----
        //
        // Each mutation invalidates CampaignCache on success, so the POS picks up the
        // change without needing an app restart.

        public async Task<CampaignResponseDto> CreateAsync(
             string promoName, string promoCategory, string reason, string description, string discountType,
             decimal discountValue, string targetTier,
            decimal? minPurchaseAmount, decimal? maxDiscountAmount,
            int maxRedemptionsPerCustomer, DateTime startDate, DateTime endDate,
            int madeByEmployeeId,
            int? branchId,
            int? requiredProductId = null, int? rewardProductId = null)
        {
            var request = new CreateCampaignRequest
            {
                PromoName = promoName,
                PromoCategory = promoCategory,
                Reason = reason,
                Description = description,
                DiscountType = discountType,
                DiscountValue = discountValue,
                TargetTier = targetTier,
                MinPurchaseAmount = minPurchaseAmount,
                MaxDiscountAmount = maxDiscountAmount,
                MaxRedemptionsPerCustomer = maxRedemptionsPerCustomer,
                StartDate = startDate,
                EndDate = endDate,
                RequiredProductId = requiredProductId,
                RewardProductId = rewardProductId,
                MadeBy = madeByEmployeeId,
                BranchId = branchId,
            };

            var result = await _api.CreateAsync(request);
            CampaignCache.Reset();
            return result;
        }

        public async Task<CampaignResponseDto> UpdateAsync(
            int campaignId,
            string promoName, string promoCategory, string reason, string description, string discountType,
            decimal discountValue, string targetTier,
            decimal? minPurchaseAmount, decimal? maxDiscountAmount,
            int maxRedemptionsPerCustomer, DateTime startDate, DateTime endDate,
            int madeByEmployeeId,
            int? branchId,
            int? requiredProductId = null, int? rewardProductId = null)
        {
            var request = new UpdateCampaignRequest
            {
                PromoName = promoName,
                PromoCategory = promoCategory,
                Reason = reason,
                Description = description,
                DiscountType = discountType,
                DiscountValue = discountValue,
                TargetTier = targetTier,
                MinPurchaseAmount = minPurchaseAmount,
                MaxDiscountAmount = maxDiscountAmount,
                MaxRedemptionsPerCustomer = maxRedemptionsPerCustomer,
                StartDate = startDate,
                EndDate = endDate,
                RequiredProductId = requiredProductId,
                RewardProductId = rewardProductId,
                MadeBy = madeByEmployeeId,
                BranchId = branchId,
            };

            var result = await _api.UpdateAsync(campaignId, request);
            CampaignCache.Reset();
            return result;
        }

        public async Task<CampaignResponseDto> ActivateAsync(int campaignId, int performedByEmployeeId)
        {
            var result = await _api.ActivateAsync(campaignId, performedByEmployeeId);
            CampaignCache.Reset();
            return result;
        }

        public async Task<CampaignResponseDto> DeactivateAsync(int campaignId, int performedByEmployeeId)
        {
            var result = await _api.DeactivateAsync(campaignId, performedByEmployeeId);
            CampaignCache.Reset();
            return result;
        }

        // ---- Admin approval flow ----

        /// <summary>Pending campaigns only — the Admin's Approvals queue.</summary>
        public Task<List<CampaignResponseDto>> GetPendingAsync()
            => _api.GetPendingForApprovalAsync();

        public async Task<CampaignResponseDto> ApproveAsync(int campaignId, int decidedByEmployeeId, string? notes)
        {
            var result = await _api.ApproveAsync(campaignId, decidedByEmployeeId, notes);
            // Approved campaigns become Active — POS's cached active list is stale.
            CampaignCache.Reset();
            return result;
        }

        public async Task<CampaignResponseDto> RejectAsync(int campaignId, int decidedByEmployeeId, string notes)
        {
            var result = await _api.RejectAsync(campaignId, decidedByEmployeeId, notes);
            CampaignCache.Reset();
            return result;
        }
    }
}