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
        // GET api/superadmin/dashboard
        //
        // One combined payload for the SuperAdmin dashboard: platform summary,
        // companies-per-plan distribution, and the most recent subscription
        // assignments. All three queries run against the master DB only.
        [HttpGet("dashboard")]
        public async Task<ActionResult<SuperAdminDashboardDto>> GetDashboard()
        {
            // ---- 1. Summary numbers ----
            var companies = await _master.Companies
                .AsNoTracking()
                .Include(c => c.CurrentPlan)
                .ToListAsync();

            var totalCompanies = companies.Count;
            var activeSubscriptions = companies.Count(c => c.CurrentPlanId.HasValue);
            var mrr = companies
                .Where(c => c.CurrentPlanId.HasValue && c.CurrentPlan is not null)
                .Sum(c => c.CurrentPlan!.MonthlyPrice);

            // ---- 2. Companies per plan ----
            // Group by the current plan, plus a synthetic "No Plan" bucket
            // for companies with CurrentPlanId == null. Ordered by plan
            // so the chart legend reads consistently (Basic, Standard, Full).
            var perPlan = companies
                .Where(c => c.CurrentPlanId.HasValue && c.CurrentPlan is not null)
                .GroupBy(c => new { c.CurrentPlanId, c.CurrentPlan!.Name })
                .Select(g => new CompaniesPerPlanPoint
                {
                    PlanId = g.Key.CurrentPlanId,
                    PlanName = g.Key.Name,
                    CompanyCount = g.Count(),
                })
                .OrderBy(p => p.PlanId)
                .ToList();

            // No-plan bucket — always included, even if zero, so the chart
            // renders the fourth bar consistently.
            var noPlanCount = companies.Count(c => !c.CurrentPlanId.HasValue);
            perPlan.Add(new CompaniesPerPlanPoint
            {
                PlanId = null,
                PlanName = "No Plan",
                CompanyCount = noPlanCount,
            });

            // ---- 3. Recent subscriptions ----
            var recent = await _master.Subscriptions
                .AsNoTracking()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .OrderByDescending(s => s.StartedAt)
                .Take(10)
                .Select(s => new RecentSubscriptionPoint
                {
                    SubscriptionId = s.SubscriptionId,
                    CompanyName = s.Company != null ? s.Company.CompanyName : "—",
                    PlanName = s.Plan != null ? s.Plan.Name : "—",
                    PlanMonthlyPrice = s.Plan != null ? s.Plan.MonthlyPrice : 0m,
                    StartedAt = s.StartedAt,
                    TermsAcceptedBy = s.TermsAcceptedBy,
                })
                .ToListAsync();

            // ---- Compose ----
            return Ok(new SuperAdminDashboardDto
            {
                Summary = new SuperAdminDashboardSummary
                {
                    TotalCompanies = totalCompanies,
                    ActiveSubscriptions = activeSubscriptions,
                    MonthlyRecurringRevenue = mrr,
                },
                CompaniesPerPlan = perPlan,
                RecentSubscriptions = recent,
            });
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

        // GET api/superadmin/terms
        //
        // Returns the list of T&C documents — one per plan. No body text;
        // the client fetches the full document per-card on click.
        [HttpGet("terms")]
        public async Task<ActionResult<List<TermsSummaryDto>>> GetTermsList()
        {
            var terms = await _master.TermsAndConditions
                .AsNoTracking()
                .Include(t => t.Plan)
                .OrderBy(t => t.PlanId)
                .Select(t => new TermsSummaryDto
                {
                    TermsId = t.TermsId,
                    PlanId = t.PlanId,
                    PlanName = t.Plan != null ? t.Plan.Name : null,
                    Title = t.Title,
                    Version = t.Version,
                    EffectiveFrom = t.EffectiveFrom,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                })
                .ToListAsync();

            return Ok(terms);
        }

        // GET api/superadmin/terms/{id}
        //
        // Returns a full T&C document including the body text.
        [HttpGet("terms/{id:int}")]
        public async Task<ActionResult<TermsDetailDto>> GetTermsDetail(int id)
        {
            var terms = await _master.TermsAndConditions
                .AsNoTracking()
                .Include(t => t.Plan)
                .Where(t => t.TermsId == id)
                .Select(t => new TermsDetailDto
                {
                    TermsId = t.TermsId,
                    PlanId = t.PlanId,
                    PlanName = t.Plan != null ? t.Plan.Name : null,
                    Title = t.Title,
                    Body = t.Body,
                    Version = t.Version,
                    EffectiveFrom = t.EffectiveFrom,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                })
                .FirstOrDefaultAsync();

            if (terms is null)
                return NotFound(new { message = $"Terms document {id} not found." });

            return Ok(terms);
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