using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Returns the tenant's branch list. Used by the WinForms topbar branch
    /// selector. Any authenticated employee can read this — the selector is
    /// shown to Manager and Admin, but the endpoint itself is fine for any role
    /// (a Barista might need it to display their branch name somewhere).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/tenant/{companyId:int}/branches")]
    public class BranchController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public BranchController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        // GET api/tenant/{companyId}/branches
        [HttpGet]
        public async Task<ActionResult<List<BranchResponseDto>>> GetBranches(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var branches = await tenant.Branches
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchId)
                .Select(b => new BranchResponseDto
                {
                    BranchId = b.BranchId,
                    BranchName = b.BranchName,
                    Address = b.Address,
                    IsActive = b.IsActive,
                })
                .ToListAsync();

            return Ok(branches);
        }
    }
}