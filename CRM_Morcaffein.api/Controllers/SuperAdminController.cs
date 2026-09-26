using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.infrastructure.data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRM_Morcaffein.domain.entities;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Read-only management endpoints for the SuperAdmin console. Writes
    /// (create company, assign plan, record T&C) come later — this pass is
    /// list-only so the shell has something to render.
    ///
    /// [Authorize(Roles = "SuperAdmin")] gates the whole controller — only
    /// tokens issued by SuperAdminAuthController can call these endpoints.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    [Route("api/superadmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly MasterCrmDbContext _master;

        public SuperAdminController(MasterCrmDbContext master)
        {
            _master = master;
        }

        // GET api/superadmin/companies
        [HttpGet("companies")]
        public async Task<ActionResult<List<SuperAdminCompanyDto>>> GetCompanies()
        {
            var companies = await _master.Companies
                .AsNoTracking()
                .Include(c => c.CurrentPlan)
                .OrderBy(c => c.CompanyId)
                .Select(c => new SuperAdminCompanyDto
                {
                    CompanyId = c.CompanyId,
                    CompanyCode = c.CompanyCode,
                    CompanyName = c.CompanyName,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    CurrentPlanId = c.CurrentPlanId,
                    CurrentPlanName = c.CurrentPlan != null ? c.CurrentPlan.Name : null,
                })
                .ToListAsync();

            return Ok(companies);
        }

        // GET api/superadmin/plans
        [HttpGet("plans")]
        public async Task<ActionResult<List<SuperAdminPlanDto>>> GetPlans()
        {
            var plans = await _master.Plans
                .AsNoTracking()
                .OrderBy(p => p.PlanId)
                .Select(p => new SuperAdminPlanDto
                {
                    PlanId = p.PlanId,
                    Name = p.Name,
                    Description = p.Description,
                    ModulesCsv = p.ModulesCsv,
                    MonthlyPrice = p.MonthlyPrice,
                    IsActive = p.IsActive,
                })
                .ToListAsync();

            return Ok(plans);
        }

        // POST api/superadmin/companies/{id}/assign-plan
        //
        // Assigns a plan to a company. Two side effects in one transaction:
        //   1. Marks any currently-active Subscription for this company as ended
        //      (stamps EndedAt = now).
        //   2. Inserts a new Subscription row with StartedAt = now, EndedAt = null.
        //   3. Updates Company.CurrentPlanId to the new plan.
        //
        // All three happen together — if any fails, the transaction rolls back.
        [HttpPost("companies/{id:int}/assign-plan")]
        public async Task<ActionResult<SubscriptionResponseDto>> AssignPlan(
            int id,
            AssignPlanRequest request)
        {
            if (request.PlanId <= 0)
                return BadRequest(new { message = "A valid plan is required." });

            if (string.IsNullOrWhiteSpace(request.TermsAcceptedBy))
                return BadRequest(new { message = "Who accepted the T&C is required." });

            var company = await _master.Companies
                .FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company is null)
                return NotFound(new { message = $"Company {id} not found." });

            var plan = await _master.Plans
                .FirstOrDefaultAsync(p => p.PlanId == request.PlanId);
            if (plan is null)
                return BadRequest(new { message = $"Plan {request.PlanId} not found." });

            if (!plan.IsActive)
                return BadRequest(new { message = $"Plan \"{plan.Name}\" is inactive and cannot be assigned." });

            var now = DateTime.UtcNow;

            // Close out the currently-active subscription (if any).
            var currentSubscription = await _master.Subscriptions
                .FirstOrDefaultAsync(s => s.CompanyId == id && s.EndedAt == null);
            if (currentSubscription is not null)
            {
                currentSubscription.EndedAt = now;
                currentSubscription.UpdatedAt = now;
            }

            // Open a new subscription.
            var subscription = new CRM_Morcaffein.domain.entities.Subscription
            {
                CompanyId = id,
                PlanId = request.PlanId,
                StartedAt = now,
                EndedAt = null,
                TermsAcceptedBy = request.TermsAcceptedBy.Trim(),
                TermsAcceptedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _master.Subscriptions.Add(subscription);

            // Update the fast-path field on Company.
            company.CurrentPlanId = request.PlanId;

            try
            {
                await _master.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // Reload the navs we need for the response.
            await _master.Entry(subscription).Reference(s => s.Company).LoadAsync();
            await _master.Entry(subscription).Reference(s => s.Plan).LoadAsync();

            return Ok(MapSubscriptionToDto(subscription));
        }

        // GET api/superadmin/companies/{id}/subscriptions
        //
        // Returns the full subscription history for a company, newest first.
        // The current subscription is the row with EndedAt == null.
        [HttpGet("companies/{id:int}/subscriptions")]
        public async Task<ActionResult<List<SubscriptionResponseDto>>> GetSubscriptions(int id)
        {
            var companyExists = await _master.Companies
                .AnyAsync(c => c.CompanyId == id);
            if (!companyExists)
                return NotFound(new { message = $"Company {id} not found." });

            var subscriptions = await _master.Subscriptions
                .AsNoTracking()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .Where(s => s.CompanyId == id)
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new SubscriptionResponseDto
                {
                    SubscriptionId = s.SubscriptionId,
                    CompanyId = s.CompanyId,
                    CompanyName = s.Company != null ? s.Company.CompanyName : "—",
                    PlanId = s.PlanId,
                    PlanName = s.Plan != null ? s.Plan.Name : "—",
                    PlanModulesCsv = s.Plan != null ? s.Plan.ModulesCsv : string.Empty,
                    PlanMonthlyPrice = s.Plan != null ? s.Plan.MonthlyPrice : 0m,
                    StartedAt = s.StartedAt,
                    EndedAt = s.EndedAt,
                    TermsAcceptedBy = s.TermsAcceptedBy,
                    TermsAcceptedAt = s.TermsAcceptedAt,
                })
                .ToListAsync();

            return Ok(subscriptions);
        }

        // ---- helpers ----

        private static SubscriptionResponseDto MapSubscriptionToDto(
            CRM_Morcaffein.domain.entities.Subscription s) => new()
            {
                SubscriptionId = s.SubscriptionId,
                CompanyId = s.CompanyId,
                CompanyName = s.Company?.CompanyName ?? "—",
                PlanId = s.PlanId,
                PlanName = s.Plan?.Name ?? "—",
                PlanModulesCsv = s.Plan?.ModulesCsv ?? string.Empty,
                PlanMonthlyPrice = s.Plan?.MonthlyPrice ?? 0m,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                TermsAcceptedBy = s.TermsAcceptedBy,
                TermsAcceptedAt = s.TermsAcceptedAt,
            };
    }
}