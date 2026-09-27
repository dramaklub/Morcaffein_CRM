using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.data;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Branch management. Any authenticated employee can read the branch
    /// list (the topbar selector needs it); mutations are Admin-only and
    /// gated by the Branching module.
    ///
    /// Soft-delete only: Deactivate flips IsActive=false. Branches are
    /// referenced by Orders, Campaigns, and Feedbacks — hard delete would
    /// break those FKs, so it's deliberately not exposed.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/tenant/{companyId:int}/branches")]
    public class BranchController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterCrmDbContext _master;

        public BranchController(
            ITenantDbContextFactory tenantFactory,
            MasterCrmDbContext master)
        {
            _tenantFactory = tenantFactory;
            _master = master;
        }

        // GET api/tenant/{companyId}/branches
        // Returns active branches only. Used by the topbar selector and
        // employee/campaign dropdowns. Inactive branches are hidden.
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

        // GET api/tenant/{companyId}/branches/all
        // Returns ALL branches (active + inactive). Admin-only — the
        // management view needs to see deactivated branches to reactivate
        // them. Everyone else sees only the active list from GET /branches.
        [HttpGet("all")]
        public async Task<ActionResult<List<BranchResponseDto>>> GetAllBranches(int companyId)
        {
            if (!await IsCallerAdminAsync(companyId))
                return StatusCode(403, new { message = "Only admins can view all branches." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var branches = await tenant.Branches
                .AsNoTracking()
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

        // POST api/tenant/{companyId}/branches
        [HttpPost]
        public async Task<ActionResult<BranchResponseDto>> CreateBranch(
            int companyId,
            CreateBranchRequest request)
        {
            if (!await IsCallerAdminAsync(companyId))
                return StatusCode(403, new { message = "Only admins can create branches." });

            if (!await TenantHasBranchingAsync(companyId))
                return BadRequest(new { message = "This tenant's plan does not include branch management." });

            if (string.IsNullOrWhiteSpace(request.BranchName))
                return BadRequest(new { message = "Branch name is required." });

            var trimmedName = request.BranchName.Trim();
            if (trimmedName.Length > 100)
                return BadRequest(new { message = "Branch name cannot exceed 100 characters." });

            var trimmedAddress = (request.Address ?? string.Empty).Trim();
            if (trimmedAddress.Length > 250)
                return BadRequest(new { message = "Address cannot exceed 250 characters." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var duplicateName = await tenant.Branches
                .AnyAsync(b => b.BranchName.ToLower() == trimmedName.ToLower());
            if (duplicateName)
                return Conflict(new { message = $"A branch named \"{trimmedName}\" already exists." });

            var branch = new Branch
            {
                BranchName = trimmedName,
                Address = trimmedAddress,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            tenant.Branches.Add(branch);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new BranchResponseDto
            {
                BranchId = branch.BranchId,
                BranchName = branch.BranchName,
                Address = branch.Address,
                IsActive = branch.IsActive,
            });
        }

        // PUT api/tenant/{companyId}/branches/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<BranchResponseDto>> UpdateBranch(
            int companyId,
            int id,
            UpdateBranchRequest request)
        {
            if (!await IsCallerAdminAsync(companyId))
                return StatusCode(403, new { message = "Only admins can update branches." });

            if (!await TenantHasBranchingAsync(companyId))
                return BadRequest(new { message = "This tenant's plan does not include branch management." });

            if (string.IsNullOrWhiteSpace(request.BranchName))
                return BadRequest(new { message = "Branch name is required." });

            var trimmedName = request.BranchName.Trim();
            if (trimmedName.Length > 100)
                return BadRequest(new { message = "Branch name cannot exceed 100 characters." });

            var trimmedAddress = (request.Address ?? string.Empty).Trim();
            if (trimmedAddress.Length > 250)
                return BadRequest(new { message = "Address cannot exceed 250 characters." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var branch = await tenant.Branches.FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch is null)
                return NotFound(new { message = $"Branch {id} not found." });

            var duplicateName = await tenant.Branches
                .AnyAsync(b => b.BranchId != id && b.BranchName.ToLower() == trimmedName.ToLower());
            if (duplicateName)
                return Conflict(new { message = $"A branch named \"{trimmedName}\" already exists." });

            branch.BranchName = trimmedName;
            branch.Address = trimmedAddress;
            branch.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new BranchResponseDto
            {
                BranchId = branch.BranchId,
                BranchName = branch.BranchName,
                Address = branch.Address,
                IsActive = branch.IsActive,
            });
        }

        // POST api/tenant/{companyId}/branches/{id}/deactivate
        [HttpPost("{id:int}/deactivate")]
        public async Task<ActionResult<BranchResponseDto>> DeactivateBranch(int companyId, int id)
        {
            if (!await IsCallerAdminAsync(companyId))
                return StatusCode(403, new { message = "Only admins can deactivate branches." });

            if (!await TenantHasBranchingAsync(companyId))
                return BadRequest(new { message = "This tenant's plan does not include branch management." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var branch = await tenant.Branches.FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch is null)
                return NotFound(new { message = $"Branch {id} not found." });

            if (!branch.IsActive)
                return Conflict(new { message = "This branch is already inactive." });

            // Guard: don't allow deactivating the last remaining active branch.
            // Orders/Campaigns/Feedbacks always reference a branch, and every
            // tenant needs at least one branch for the topbar selector, the
            // employee assignment dropdown, and the POS to work.
            var activeBranchCount = await tenant.Branches.CountAsync(b => b.IsActive);
            if (activeBranchCount <= 1)
                return Conflict(new
                {
                    message = "Cannot deactivate the only active branch. Create another branch first."
                });

            // Guard: don't allow deactivating a branch that still has
            // employees pinned to it. Those employees would become
            // unassignable at login and their sessions would be scoped to
            // a hidden branch.
            var pinnedEmployeeCount = await tenant.Employees
                .CountAsync(e => e.BranchId == id && e.IsActive);
            if (pinnedEmployeeCount > 0)
                return Conflict(new
                {
                    message = $"Cannot deactivate this branch — {pinnedEmployeeCount} active employee(s) are still assigned to it. Reassign them first."
                });

            branch.IsActive = false;
            branch.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new BranchResponseDto
            {
                BranchId = branch.BranchId,
                BranchName = branch.BranchName,
                Address = branch.Address,
                IsActive = branch.IsActive,
            });
        }

        // POST api/tenant/{companyId}/branches/{id}/reactivate
        [HttpPost("{id:int}/reactivate")]
        public async Task<ActionResult<BranchResponseDto>> ReactivateBranch(int companyId, int id)
        {
            if (!await IsCallerAdminAsync(companyId))
                return StatusCode(403, new { message = "Only admins can reactivate branches." });

            if (!await TenantHasBranchingAsync(companyId))
                return BadRequest(new { message = "This tenant's plan does not include branch management." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var branch = await tenant.Branches.FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch is null)
                return NotFound(new { message = $"Branch {id} not found." });

            if (branch.IsActive)
                return Conflict(new { message = "This branch is already active." });

            branch.IsActive = true;
            branch.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new BranchResponseDto
            {
                BranchId = branch.BranchId,
                BranchName = branch.BranchName,
                Address = branch.Address,
                IsActive = branch.IsActive,
            });
        }

        // ---- helpers ----

        /// <summary>
        /// True if the calling employee (from the JWT) is an Admin in the
        /// specified tenant's DB. Reads EmployeeId from the JWT, then
        /// looks up the role in the tenant DB.
        /// </summary>
        private async Task<bool> IsCallerAdminAsync(int companyId)
        {
            var empIdClaim = User.FindFirst("employeeId")?.Value;
            if (!int.TryParse(empIdClaim, out var employeeId))
                return false;

            await using var tenant = await _tenantFactory.CreateAsync(companyId);
            var role = await tenant.Employees
                .AsNoTracking()
                .Where(e => e.EmployeeId == employeeId)
                .Select(e => e.Role)
                .FirstOrDefaultAsync();

            return role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// True if the tenant's current plan includes the Branching module.
        /// </summary>
        private async Task<bool> TenantHasBranchingAsync(int companyId)
        {
            var company = await _master.Companies
                .AsNoTracking()
                .Include(c => c.CurrentPlan)
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            return company?.CurrentPlan?.ModulesCsv
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains("Branching", StringComparer.OrdinalIgnoreCase) == true;
        }
    }
}