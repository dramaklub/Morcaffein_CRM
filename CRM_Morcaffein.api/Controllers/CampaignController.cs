using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tenant/{companyId:int}/campaigns")]
    public class CampaignController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public CampaignController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        // GET api/tenant/{companyId}/campaigns
        // Returns all currently-active campaigns (IsActive + within date range). The POS
        // path — Pending/Rejected/Inactive campaigns are excluded by the IsActive filter.
        [HttpGet]
        public async Task<ActionResult<List<CampaignResponseDto>>> GetActiveCampaigns(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var now = DateTime.UtcNow;

            var campaigns = await tenant.Campaigns
                .AsNoTracking()
                .Where(c => c.IsActive && now >= c.StartDate && now <= c.EndDate)
                .OrderBy(c => c.CampaignId)
                .Select(c => new CampaignResponseDto
                {
                    CampaignId = c.CampaignId,
                    PromoName = c.PromoName,
                    PromoCategory = c.PromoCategory,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    TargetTier = c.TargetTier,
                    MinPurchaseAmount = c.MinPurchaseAmount,
                    MaxDiscountAmount = c.MaxDiscountAmount,
                    MaxRedemptionsPerCustomer = c.MaxRedemptionsPerCustomer,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    IsActive = c.IsActive,
                    Status = c.Status,
                    MadeBy = c.MadeBy,
                    CreatedByName = c.Creator != null
                        ? (c.Creator.FirstName + " " + c.Creator.LastName).Trim()
                        : "—",
                    DecidedBy = c.DecidedBy,
                    DecidedAt = c.DecidedAt,
                    DecisionNotes = c.DecisionNotes,
                    RequiredProductId = c.RequiredProductId,
                    RequiredProductName = c.RequiredProduct != null ? c.RequiredProduct.ProductName : null,
                    RewardProductId = c.RewardProductId,
                    RewardProductName = c.RewardProduct != null ? c.RewardProduct.ProductName : null,
                })
                .ToListAsync();

            return Ok(campaigns);
        }

        // GET api/tenant/{companyId}/campaigns/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CampaignResponseDto>> GetCampaign(int companyId, int id)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var campaign = await tenant.Campaigns
                .AsNoTracking()
                .Where(c => c.CampaignId == id)
                .Select(c => new CampaignResponseDto
                {
                    CampaignId = c.CampaignId,
                    PromoName = c.PromoName,
                    PromoCategory = c.PromoCategory,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    TargetTier = c.TargetTier,
                    MinPurchaseAmount = c.MinPurchaseAmount,
                    MaxDiscountAmount = c.MaxDiscountAmount,
                    MaxRedemptionsPerCustomer = c.MaxRedemptionsPerCustomer,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    IsActive = c.IsActive,
                    Status = c.Status,
                    MadeBy = c.MadeBy,
                    CreatedByName = c.Creator != null
                        ? (c.Creator.FirstName + " " + c.Creator.LastName).Trim()
                        : "—",
                    DecidedBy = c.DecidedBy,
                    DecidedAt = c.DecidedAt,
                    DecisionNotes = c.DecisionNotes,
                    RequiredProductId = c.RequiredProductId,
                    RequiredProductName = c.RequiredProduct != null ? c.RequiredProduct.ProductName : null,
                    RewardProductId = c.RewardProductId,
                    RewardProductName = c.RewardProduct != null ? c.RewardProduct.ProductName : null,
                })
                .FirstOrDefaultAsync();

            if (campaign is null)
                return NotFound();

            return Ok(campaign);
        }

        // GET api/tenant/{companyId}/campaigns/manage
        //
        // Returns EVERY campaign regardless of status or date range, ordered newest first.
        // This is the Manager's list. The POS keeps using GET /campaigns, which filters
        // to currently-active campaigns only.
        [HttpGet("manage")]
        public async Task<ActionResult<List<CampaignResponseDto>>> GetForManagement(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var campaigns = await tenant.Campaigns
                .AsNoTracking()
                .OrderByDescending(c => c.CampaignId)
                .Select(c => new CampaignResponseDto
                {
                    CampaignId = c.CampaignId,
                    PromoName = c.PromoName,
                    PromoCategory = c.PromoCategory,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    TargetTier = c.TargetTier,
                    MinPurchaseAmount = c.MinPurchaseAmount,
                    MaxDiscountAmount = c.MaxDiscountAmount,
                    MaxRedemptionsPerCustomer = c.MaxRedemptionsPerCustomer,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    IsActive = c.IsActive,
                    Status = c.Status,
                    MadeBy = c.MadeBy,
                    CreatedByName = c.Creator != null
                        ? (c.Creator.FirstName + " " + c.Creator.LastName).Trim()
                        : "—",
                    DecidedBy = c.DecidedBy,
                    DecidedAt = c.DecidedAt,
                    DecisionNotes = c.DecisionNotes,
                    RequiredProductId = c.RequiredProductId,
                    RequiredProductName = c.RequiredProduct != null ? c.RequiredProduct.ProductName : null,
                    RewardProductId = c.RewardProductId,
                    RewardProductName = c.RewardProduct != null ? c.RewardProduct.ProductName : null,
                })
                .ToListAsync();

            return Ok(campaigns);
        }

        // POST api/tenant/{companyId}/campaigns
        //
        // Creates a campaign. Manager only. Status starts as "Pending" — it goes live
        // only when an Admin approves it from the Approvals tab.
        [HttpPost]
        public async Task<ActionResult<CampaignResponseDto>> CreateCampaign(
            int companyId,
            CreateCampaignRequest request)
        {
            var validationError = ValidateCampaignRequest(
                request.PromoName, request.PromoCategory, request.DiscountType,
                request.DiscountValue, request.TargetTier,
                request.MinPurchaseAmount, request.MaxDiscountAmount,
                request.MaxRedemptionsPerCustomer, request.StartDate, request.EndDate,
                request.RequiredProductId, request.RewardProductId);

            if (validationError is not null)
                return BadRequest(new { message = validationError });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var creator = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.MadeBy);

            if (creator is null)
                return BadRequest(new { message = "Creator not found." });

            if (!creator.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only managers can create campaigns." });

            if (IsB1T1(request.DiscountType))
            {
                var productError = await ValidateB1T1ProductsAsync(tenant, request.RequiredProductId, request.RewardProductId);
                if (productError is not null)
                    return BadRequest(new { message = productError });
            }

            var campaign = new Campaign
            {
                PromoName = request.PromoName.Trim(),
                PromoCategory = request.PromoCategory.Trim(),
                DiscountType = request.DiscountType,
                DiscountValue = IsB1T1(request.DiscountType) ? 0m : request.DiscountValue,
                TargetTier = request.TargetTier,
                MinPurchaseAmount = request.MinPurchaseAmount,
                MaxDiscountAmount = request.MaxDiscountAmount,
                MaxRedemptionsPerCustomer = request.MaxRedemptionsPerCustomer,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                RequiredProductId = IsB1T1(request.DiscountType) ? request.RequiredProductId : null,
                RewardProductId = IsB1T1(request.DiscountType) ? request.RewardProductId : null,
                MadeBy = request.MadeBy,
                Status = "Pending",
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            tenant.Campaigns.Add(campaign);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // Reload with the Creator nav so CreatedByName is populated in the response.
            await tenant.Entry(campaign).Reference(c => c.Creator).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RequiredProduct).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RewardProduct).LoadAsync();

            return Ok(MapToDto(campaign));
        }

        // PUT api/tenant/{companyId}/campaigns/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CampaignResponseDto>> UpdateCampaign(
            int companyId,
            int id,
            UpdateCampaignRequest request)
        {
            var validationError = ValidateCampaignRequest(
                request.PromoName, request.PromoCategory, request.DiscountType,
                request.DiscountValue, request.TargetTier,
                request.MinPurchaseAmount, request.MaxDiscountAmount,
                request.MaxRedemptionsPerCustomer, request.StartDate, request.EndDate,
                request.RequiredProductId, request.RewardProductId);

            if (validationError is not null)
                return BadRequest(new { message = validationError });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var editor = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.MadeBy);

            if (editor is null)
                return BadRequest(new { message = "Editor not found." });

            if (!editor.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only managers can edit campaigns." });

            if (IsB1T1(request.DiscountType))
            {
                var productError = await ValidateB1T1ProductsAsync(tenant, request.RequiredProductId, request.RewardProductId);
                if (productError is not null)
                    return BadRequest(new { message = productError });
            }

            var campaign = await tenant.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id);
            if (campaign is null)
                return NotFound();

            campaign.PromoName = request.PromoName.Trim();
            campaign.PromoCategory = request.PromoCategory.Trim();
            campaign.DiscountType = request.DiscountType;
            campaign.DiscountValue = IsB1T1(request.DiscountType) ? 0m : request.DiscountValue;
            campaign.TargetTier = request.TargetTier;
            campaign.MinPurchaseAmount = request.MinPurchaseAmount;
            campaign.MaxDiscountAmount = request.MaxDiscountAmount;
            campaign.MaxRedemptionsPerCustomer = request.MaxRedemptionsPerCustomer;
            campaign.StartDate = request.StartDate;
            campaign.EndDate = request.EndDate;
            campaign.RequiredProductId = IsB1T1(request.DiscountType) ? request.RequiredProductId : null;
            campaign.RewardProductId = IsB1T1(request.DiscountType) ? request.RewardProductId : null;
            campaign.UpdatedAt = DateTime.UtcNow;

            // A Rejected campaign that's edited goes back to Pending — the Manager is
            // resubmitting. A Pending or Active campaign keeps its status; only a Rejected
            // one changes here.
            if (campaign.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                campaign.Status = "Pending";
                campaign.IsActive = false;
                campaign.DecidedBy = null;
                campaign.DecidedAt = null;
                campaign.DecisionNotes = null;
            }

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            await tenant.Entry(campaign).Reference(c => c.Creator).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RequiredProduct).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RewardProduct).LoadAsync();

            return Ok(MapToDto(campaign));
        }

        // POST api/tenant/{companyId}/campaigns/{id}/activate
        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult<CampaignResponseDto>> ActivateCampaign(
            int companyId,
            int id,
            CampaignActionRequest request)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var actor = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.PerformedBy);

            if (actor is null)
                return BadRequest(new { message = "Actor not found." });

            if (!actor.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only managers can activate campaigns." });

            var campaign = await tenant.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id);
            if (campaign is null)
                return NotFound();

            // Pending campaigns are frozen until an Admin decides. Rejected campaigns
            // must be edited (which resubmits them as Pending) before they can go live.
            if (campaign.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Pending campaigns must be approved by an admin before they can go live." });

            if (campaign.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Rejected campaigns must be edited and resubmitted for approval." });

            campaign.Status = "Active";
            campaign.IsActive = true;
            campaign.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            await tenant.Entry(campaign).Reference(c => c.Creator).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RequiredProduct).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RewardProduct).LoadAsync();

            return Ok(MapToDto(campaign));
        }

        // POST api/tenant/{companyId}/campaigns/{id}/deactivate
        [HttpPost("{id:int}/deactivate")]
        public async Task<ActionResult<CampaignResponseDto>> DeactivateCampaign(
            int companyId,
            int id,
            CampaignActionRequest request)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var actor = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.PerformedBy);

            if (actor is null)
                return BadRequest(new { message = "Actor not found." });

            if (!actor.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only managers can deactivate campaigns." });

            var campaign = await tenant.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id);
            if (campaign is null)
                return NotFound();

            if (campaign.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Pending campaigns cannot be deactivated. They are awaiting admin approval." });

            campaign.Status = "Inactive";
            campaign.IsActive = false;
            campaign.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            await tenant.Entry(campaign).Reference(c => c.Creator).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RequiredProduct).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RewardProduct).LoadAsync();

            return Ok(MapToDto(campaign));
        }

        // GET api/tenant/{companyId}/campaigns/pending
        //
        // Returns only Pending campaigns, for the Admin's Approvals tab. Ordered oldest
        // first so the queue reads like a queue — oldest submission at the top.
        [HttpGet("pending")]
        public async Task<ActionResult<List<CampaignResponseDto>>> GetPendingCampaigns(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var campaigns = await tenant.Campaigns
                .AsNoTracking()
                .Where(c => c.Status == "Pending")
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CampaignResponseDto
                {
                    CampaignId = c.CampaignId,
                    PromoName = c.PromoName,
                    PromoCategory = c.PromoCategory,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    TargetTier = c.TargetTier,
                    MinPurchaseAmount = c.MinPurchaseAmount,
                    MaxDiscountAmount = c.MaxDiscountAmount,
                    MaxRedemptionsPerCustomer = c.MaxRedemptionsPerCustomer,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    IsActive = c.IsActive,
                    Status = c.Status,
                    MadeBy = c.MadeBy,
                    CreatedByName = c.Creator != null
                        ? (c.Creator.FirstName + " " + c.Creator.LastName).Trim()
                        : "—",
                    DecidedBy = c.DecidedBy,
                    DecidedAt = c.DecidedAt,
                    DecisionNotes = c.DecisionNotes,
                    RequiredProductId = c.RequiredProductId,
                    RequiredProductName = c.RequiredProduct != null ? c.RequiredProduct.ProductName : null,
                    RewardProductId = c.RewardProductId,
                    RewardProductName = c.RewardProduct != null ? c.RewardProduct.ProductName : null,
                })
                .ToListAsync();

            return Ok(campaigns);
        }

        // POST api/tenant/{companyId}/campaigns/{id}/approve
        //
        // Admin approves a Pending campaign. Flips it live and records the decision.
        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<CampaignResponseDto>> ApproveCampaign(
            int companyId,
            int id,
            DecideCampaignRequest request)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var decider = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.DecidedBy);

            if (decider is null)
                return BadRequest(new { message = "Deciding employee not found." });

            if (!decider.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                !decider.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only admins can approve campaigns." });

            var campaign = await tenant.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id);
            if (campaign is null)
                return NotFound();

            if (!campaign.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = $"Only pending campaigns can be approved (current status: {campaign.Status})." });

            campaign.Status = "Active";
            campaign.IsActive = true;
            campaign.DecidedBy = request.DecidedBy;
            campaign.DecidedAt = DateTime.UtcNow;
            campaign.DecisionNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            campaign.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            await tenant.Entry(campaign).Reference(c => c.Creator).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RequiredProduct).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RewardProduct).LoadAsync();

            return Ok(MapToDto(campaign));
        }

        // POST api/tenant/{companyId}/campaigns/{id}/reject
        //
        // Admin rejects a Pending campaign. Requires a reason. The campaign stays in the
        // DB so the Manager can edit and resubmit it.
        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<CampaignResponseDto>> RejectCampaign(
            int companyId,
            int id,
            DecideCampaignRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Notes))
                return BadRequest(new { message = "A rejection reason is required." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var decider = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.DecidedBy);

            if (decider is null)
                return BadRequest(new { message = "Deciding employee not found." });

            if (!decider.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                !decider.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only admins can reject campaigns." });

            var campaign = await tenant.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id);
            if (campaign is null)
                return NotFound();

            if (!campaign.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = $"Only pending campaigns can be rejected (current status: {campaign.Status})." });

            campaign.Status = "Rejected";
            campaign.IsActive = false;
            campaign.DecidedBy = request.DecidedBy;
            campaign.DecidedAt = DateTime.UtcNow;
            campaign.DecisionNotes = request.Notes.Trim();
            campaign.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            await tenant.Entry(campaign).Reference(c => c.Creator).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RequiredProduct).LoadAsync();
            await tenant.Entry(campaign).Reference(c => c.RewardProduct).LoadAsync();

            return Ok(MapToDto(campaign));
        }

        // ---- helpers ----

        private static async Task<string?> ValidateB1T1ProductsAsync(
            CRM_Morcaffein.infrastructure.data.TenantCrmDbContext tenant,
            int? requiredProductId, int? rewardProductId)
        {
            if (!requiredProductId.HasValue || !rewardProductId.HasValue)
                return "A B1T1 promo requires both a required product and a free (reward) product.";

            var requiredExists = await tenant.Products.AnyAsync(p => p.ProductId == requiredProductId.Value);
            if (!requiredExists)
                return $"Required product {requiredProductId.Value} not found.";

            var rewardExists = await tenant.Products.AnyAsync(p => p.ProductId == rewardProductId.Value);
            if (!rewardExists)
                return $"Reward product {rewardProductId.Value} not found.";

            return null;
        }

        private static bool IsB1T1(string discountType) =>
            discountType.Equals("B1T1", StringComparison.OrdinalIgnoreCase);

        private static string? ValidateCampaignRequest(
            string promoName, string promoCategory, string discountType,
            decimal discountValue, string targetTier,
            decimal? minPurchase, decimal? maxDiscount,
            int maxRedemptions, DateTime startDate, DateTime endDate,
            int? requiredProductId, int? rewardProductId)
        {
            if (string.IsNullOrWhiteSpace(promoName))
                return "Promo name is required.";
            if (string.IsNullOrWhiteSpace(promoCategory))
                return "Promo category is required.";
            if (string.IsNullOrWhiteSpace(discountType))
                return "Discount type is required.";

            if (!discountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase) &&
                !discountType.Equals("FixedAmount", StringComparison.OrdinalIgnoreCase) &&
                !IsB1T1(discountType))
                return "Discount type must be Percentage, FixedAmount, or B1T1.";

            if (IsB1T1(discountType))
            {
                if (!requiredProductId.HasValue || !rewardProductId.HasValue)
                    return "A B1T1 promo requires both a required product and a free (reward) product.";
            }
            else
            {
                if (discountValue <= 0)
                    return "Discount value must be greater than zero.";

                if (discountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase) && discountValue > 100)
                    return "Percentage discount cannot exceed 100.";
            }

            if (maxDiscount.HasValue && maxDiscount.Value <= 0)
                return "Max discount must be greater than zero if set.";

            if (minPurchase.HasValue && minPurchase.Value < 0)
                return "Minimum purchase cannot be negative.";

            if (maxRedemptions <= 0)
                return "Max redemptions per customer must be at least 1.";

            if (endDate <= startDate)
                return "End date must be after start date.";

            return null;
        }

        private static CampaignResponseDto MapToDto(Campaign c) => new()
        {
            CampaignId = c.CampaignId,
            PromoName = c.PromoName,
            PromoCategory = c.PromoCategory,
            DiscountType = c.DiscountType,
            DiscountValue = c.DiscountValue,
            TargetTier = c.TargetTier,
            MinPurchaseAmount = c.MinPurchaseAmount,
            MaxDiscountAmount = c.MaxDiscountAmount,
            MaxRedemptionsPerCustomer = c.MaxRedemptionsPerCustomer,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            IsActive = c.IsActive,
            Status = c.Status,
            MadeBy = c.MadeBy,
            CreatedByName = c.Creator != null
                ? (c.Creator.FirstName + " " + c.Creator.LastName).Trim()
                : "—",
            DecidedBy = c.DecidedBy,
            DecidedAt = c.DecidedAt,
            DecisionNotes = c.DecisionNotes,
            RequiredProductId = c.RequiredProductId,
            RequiredProductName = c.RequiredProduct?.ProductName,
            RewardProductId = c.RewardProductId,
            RewardProductName = c.RewardProduct?.ProductName,
        };
    }
}