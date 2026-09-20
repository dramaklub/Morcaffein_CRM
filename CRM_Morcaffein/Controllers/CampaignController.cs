using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.Controllers
{
    /// <summary>
    /// UI controller for the Campaign tab (WinForms). Pure coordination: the View calls
    /// these methods, they forward to the API. All business rules live on the API side.
    ///
    /// Also responsible for invalidating the POS's CampaignCache whenever a Manager
    /// creates, edits, activates, or deactivates a campaign — otherwise the POS would
    /// keep serving stale active-campaign lists until the app restarts.
    /// </summary>
    public class CampaignController
    {
        private readonly CampaignApi _api;

        public CampaignController()
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
        // change without needing an app restart. Invalidating on failure would be wrong —
        // a rejected edit shouldn't reset the cache.

        public async Task<CampaignResponseDto> CreateAsync(
            string promoName, string promoCategory, string discountType,
            decimal discountValue, string targetTier,
            decimal? minPurchaseAmount, decimal? maxDiscountAmount,
            int maxRedemptionsPerCustomer, DateTime startDate, DateTime endDate,
            int madeByEmployeeId,
            int? requiredProductId = null, int? rewardProductId = null)
        {
            var request = new CreateCampaignRequest
            {
                PromoName = promoName,
                PromoCategory = promoCategory,
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
            };

            var result = await _api.CreateAsync(request);
            CampaignCache.Reset();
            return result;
        }

        public async Task<CampaignResponseDto> UpdateAsync(
            int campaignId,
            string promoName, string promoCategory, string discountType,
            decimal discountValue, string targetTier,
            decimal? minPurchaseAmount, decimal? maxDiscountAmount,
            int maxRedemptionsPerCustomer, DateTime startDate, DateTime endDate,
            int madeByEmployeeId,
            int? requiredProductId = null, int? rewardProductId = null)
        {
            var request = new UpdateCampaignRequest
            {
                PromoName = promoName,
                PromoCategory = promoCategory,
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
            // Approved campaigns become Active, so the POS's cached active list is now stale.
            CampaignCache.Reset();
            return result;
        }

        public async Task<CampaignResponseDto> RejectAsync(int campaignId, int decidedByEmployeeId, string notes)
        {
            var result = await _api.RejectAsync(campaignId, decidedByEmployeeId, notes);
            // Rejected campaigns are not Active, so the POS cache is unaffected in practice —
            // but reset anyway for consistency, in case a previously-active campaign was
            // somehow rejected (it can't be, but the reset is cheap and safe).
            CampaignCache.Reset();
            return result;
        }
    }
}
    