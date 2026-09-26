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
    }
}