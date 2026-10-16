using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.infrastructure.data;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Endpoints about the *caller's own* tenant context. Currently just
    /// entitlements — what the tenant's subscription plan allows.
    ///
    /// Every authenticated employee can call this; it's read-only and
    /// scoped to the companyId in the URL, which the client supplies from
    /// the login response.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/tenant/{companyId:int}/me")]
    public class MeController : ControllerBase
    {
        private readonly MasterCrmDbContext _master;

        public MeController(MasterCrmDbContext master)
        {
            _master = master;
        }

        // GET api/tenant/{companyId}/me/entitlements
        [HttpGet("entitlements")]
        public async Task<ActionResult<EntitlementsResponseDto>> GetEntitlements(int companyId)
        {
            var company = await _master.Companies
                .AsNoTracking()
                .Include(c => c.CurrentPlan)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company is null)
                return NotFound(new { message = $"Company {companyId} not found." });

            var modules = new List<string>();

            if (company.CurrentPlan is not null && !string.IsNullOrWhiteSpace(company.CurrentPlan.ModulesCsv))
            {
                modules = company.CurrentPlan.ModulesCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            return Ok(new EntitlementsResponseDto
            {
                Modules = modules,
                PlanId = company.CurrentPlanId,
                PlanName = company.CurrentPlan?.Name,
            });
        }

        // GET api/tenant/{companyId}/me/terms
        //
        // Returns the T&C document for the tenant's current plan, plus the
        // acceptance metadata from the current Subscription row. Admin-only
        // — the tenant's Admin is the one who agreed to the terms; Managers
        // and Baristas don't see this surface.
        //
        // Reads from master (Companies, Plans, TermsAndConditions,
        // Subscriptions) — no tenant DB access needed.
        [HttpGet("terms")]
        public async Task<ActionResult<TenantTermsDto>> GetTerms(int companyId)
        {
            // Server-side enforcement — Admin only.
            if (!User.IsInRole("Admin"))
                return StatusCode(403, new { message = "Only admins can view subscription terms." });

            var company = await _master.Companies
                .AsNoTracking()
                .Include(c => c.CurrentPlan)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (company is null)
                return NotFound(new { message = $"Company {companyId} not found." });

            // No plan assigned — nothing to show.
            if (company.CurrentPlanId is null)
                return Ok(new TenantTermsDto { HasTerms = false });

            // Look up the T&C row for the current plan.
            var terms = await _master.TermsAndConditions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PlanId == company.CurrentPlanId.Value);

            if (terms is null)
                return Ok(new TenantTermsDto
                {
                    HasTerms = false,
                    PlanId = company.CurrentPlanId,
                    PlanName = company.CurrentPlan?.Name,
                });

            // Current subscription — the row with EndedAt == null.
            var subscription = await _master.Subscriptions
                .AsNoTracking()
                .Where(s => s.CompanyId == companyId && s.EndedAt == null)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();

            return Ok(new TenantTermsDto
            {
                HasTerms = true,
                PlanId = company.CurrentPlanId,
                PlanName = company.CurrentPlan?.Name,
                Title = terms.Title,
                Body = terms.Body,
                Version = terms.Version,
                EffectiveFrom = terms.EffectiveFrom,
                AcceptedBy = subscription?.TermsAcceptedBy,
                AcceptedAt = subscription?.TermsAcceptedAt,
            });
        }
    }
}