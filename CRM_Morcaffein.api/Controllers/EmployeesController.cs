using System.Security.Claims;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.data;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tenant/{companyId:int}/employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly MasterCrmDbContext _master;

        public EmployeesController(
            ITenantDbContextFactory tenantFactory,
            MasterCrmDbContext master)
        {
            _tenantFactory = tenantFactory;
            _master = master;
        }

        // ---- helpers ----

        private int GetCallerEmployeeId()
        {
            var claim = User.FindFirst("employeeId")?.Value
                ?? throw new InvalidOperationException("Missing employeeId claim.");
            return int.Parse(claim);
        }

        private bool CallerIsAdmin() => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        private void ForbidIfNotAdmin()
        {
            if (!CallerIsAdmin())
                throw new UnauthorizedAccessException();
        }

        // ---- endpoints ----

        // GET api/tenant/{companyId}/employees?search=&page=1&pageSize=25
        [HttpGet]
        public async Task<ActionResult<PagedResponse<EmployeeResponseDto>>> GetEmployees(
            int companyId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can manage employees." });

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 25;
            if (pageSize > 200) pageSize = 200;

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var query = tenant.Employees
                .AsNoTracking()
                .Include(e => e.Credential)
                .Include(e => e.Branch)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(e =>
                    (e.FirstName + " " + e.LastName).Contains(term) ||
                    e.ContactNumber.Contains(term) ||
                    (e.Credential != null && e.Credential.Username.Contains(term)) ||
                    e.EmployeeId.ToString() == term);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(e => e.EmployeeId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployeeResponseDto
                {
                    EmployeeId = e.EmployeeId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FullName = (e.FirstName + " " + e.LastName).Trim(),
                    ContactNumber = e.ContactNumber,
                    Email = e.Email,
                    Role = e.Role,
                    IsActive = e.IsActive,
                    BranchId = e.BranchId,
                    BranchName = e.Branch != null ? e.Branch.BranchName : null,
                    Username = e.Credential != null ? e.Credential.Username : null,
                    LastLoginDate = e.Credential != null ? e.Credential.LastLoginDate : null,
                    HasCredential = e.Credential != null
                })
                .ToListAsync();

            return Ok(new PagedResponse<EmployeeResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // GET api/tenant/{companyId}/employees/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EmployeeResponseDto>> GetEmployee(int companyId, int id)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can manage employees." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var employee = await tenant.Employees
                .AsNoTracking()
                .Include(e => e.Credential)
                .Include(e => e.Branch)
                .Where(e => e.EmployeeId == id)
                .Select(e => new EmployeeResponseDto
                {
                    EmployeeId = e.EmployeeId,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    FullName = (e.FirstName + " " + e.LastName).Trim(),
                    ContactNumber = e.ContactNumber,
                    Email = e.Email,
                    Role = e.Role,
                    IsActive = e.IsActive,
                    BranchId = e.BranchId,
                    BranchName = e.Branch != null ? e.Branch.BranchName : null,
                    Username = e.Credential != null ? e.Credential.Username : null,
                    LastLoginDate = e.Credential != null ? e.Credential.LastLoginDate : null,
                    HasCredential = e.Credential != null
                })
                .FirstOrDefaultAsync();

            if (employee is null)
                return NotFound();

            return Ok(employee);
        }

        // POST api/tenant/{companyId}/employees
        [HttpPost]
        public async Task<ActionResult<EmployeeResponseDto>> CreateEmployee(
            int companyId,
            CreateEmployeeRequest request)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can create employees." });

            // Validation
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest(new { message = "First name is required." });
            if (string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest(new { message = "Last name is required." });

            var cleanedPhone = new string((request.ContactNumber ?? "").Where(char.IsDigit).ToArray());
            if (cleanedPhone.Length < 10 || cleanedPhone.Length > 11)
                return BadRequest(new { message = "Contact number must be 10-11 digits." });

            var cleanedEmail = string.Empty;
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (!request.Email.Contains('@') || !request.Email.Contains('.'))
                    return BadRequest(new { message = "Invalid email format." });
                cleanedEmail = request.Email.Trim().ToLower();
            }

            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length < 3)
                return BadRequest(new { message = "Username must be at least 3 characters." });
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
                return BadRequest(new { message = "Password must be at least 4 characters." });

            var requestedRole = (request.Role ?? "Barista").Trim();
            if (requestedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                requestedRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new { message = "Only the SuperAdmin can create Admin accounts." });
            }
            if (!requestedRole.Equals("Barista", StringComparison.OrdinalIgnoreCase) &&
                !requestedRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Role must be Barista or Manager." });
            }

            var trimmedUsername = request.Username.Trim();

            // Global username uniqueness across ALL tenants — the CompanyUser
            // routing table has a unique index on Username, and the login flow
            // resolves a username to exactly one tenant. Two tenants can't
            // share a username.
            var usernameTakenInMaster = await _master.CompanyUsers
                .AnyAsync(cu => cu.Username == trimmedUsername);
            if (usernameTakenInMaster)
                return Conflict(new { message = "That username is already taken (by another tenant)." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // ---- Branch validation ----
            int? resolvedBranchId = request.BranchId;

            if (requestedRole.Equals("Barista", StringComparison.OrdinalIgnoreCase) &&
                resolvedBranchId is null)
            {
                return BadRequest(new { message = "Baristas must be assigned to a branch." });
            }

            string? resolvedBranchName = null;
            if (resolvedBranchId.HasValue)
            {
                resolvedBranchName = await tenant.Branches
                    .AsNoTracking()
                    .Where(b => b.BranchId == resolvedBranchId.Value && b.IsActive)
                    .Select(b => b.BranchName)
                    .FirstOrDefaultAsync();

                if (resolvedBranchName is null)
                    return BadRequest(new { message = $"Branch {resolvedBranchId.Value} not found or is inactive." });
            }

            // Uniqueness within the tenant's own Credentials table.
            var usernameTakenInTenant = await tenant.Credentials
                .AnyAsync(c => c.Username == trimmedUsername);
            if (usernameTakenInTenant)
                return Conflict(new { message = "That username is already taken." });

            var employee = new Employee
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                ContactNumber = cleanedPhone,
                Email = cleanedEmail,
                Role = requestedRole,
                BranchId = resolvedBranchId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var credential = new Credential
            {
                Username = trimmedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Employee = employee,
                FailedLoginAttempts = 0,
                IsLocked = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // ---- Step 1: reserve the username in the master routing table ----
            // Master-first so a failure here doesn't leave orphaned tenant rows
            // that can't log in. If this succeeds and the tenant write below
            // fails, we delete this row to keep the two DBs consistent.
            var routingRow = new CompanyUser
            {
                CompanyId = companyId,
                Username = trimmedUsername,
                CreatedAt = DateTime.UtcNow,
            };
            _master.CompanyUsers.Add(routingRow);

            try
            {
                await _master.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // ---- Step 2: write the tenant-side Employee + Credential ----
            tenant.Employees.Add(employee);
            tenant.Credentials.Add(credential);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Compensating rollback: undo the master row so the username
                // is free again and the two DBs stay consistent.
                try
                {
                    _master.CompanyUsers.Remove(routingRow);
                    await _master.SaveChangesAsync();
                }
                catch
                {
                    // Rollback failed — this is bad but very rare. Log it.
                    // The sync-company-users endpoint can clean up stragglers.
                }

                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new EmployeeResponseDto
            {
                EmployeeId = employee.EmployeeId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                FullName = employee.FullName,
                ContactNumber = employee.ContactNumber,
                Email = employee.Email,
                Role = employee.Role,
                IsActive = employee.IsActive,
                BranchId = employee.BranchId,
                BranchName = resolvedBranchName,
                Username = credential.Username,
                LastLoginDate = credential.LastLoginDate,
                HasCredential = true
            });
        }

        // PUT api/tenant/{companyId}/employees/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<EmployeeResponseDto>> UpdateEmployee(
            int companyId,
            int id,
            UpdateEmployeeRequest request)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can update employees." });

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest(new { message = "First name is required." });
            if (string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest(new { message = "Last name is required." });

            var cleanedPhone = new string((request.ContactNumber ?? "").Where(char.IsDigit).ToArray());
            if (cleanedPhone.Length < 10 || cleanedPhone.Length > 11)
                return BadRequest(new { message = "Contact number must be 10-11 digits." });

            var cleanedEmail = string.Empty;
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (!request.Email.Contains('@') || !request.Email.Contains('.'))
                    return BadRequest(new { message = "Invalid email format." });
                cleanedEmail = request.Email.Trim().ToLower();
            }

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var employee = await tenant.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee is null)
                return NotFound();

            if (employee.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                employee.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Admin accounts cannot be edited from this console." });

            if (!employee.IsActive)
                return StatusCode(403, new { message = "Inactive employees cannot be edited. Reactivate them first." });

            var requestedRole = (request.Role ?? "").Trim();
            if (requestedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                requestedRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only the SuperAdmin can promote to Admin." });
            if (!requestedRole.Equals("Barista", StringComparison.OrdinalIgnoreCase) &&
                !requestedRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Role must be Barista or Manager." });

            int? resolvedBranchId = request.BranchId;

            if (requestedRole.Equals("Barista", StringComparison.OrdinalIgnoreCase) &&
                resolvedBranchId is null)
            {
                return BadRequest(new { message = "Baristas must be assigned to a branch." });
            }

            string? resolvedBranchName = null;
            if (resolvedBranchId.HasValue)
            {
                resolvedBranchName = await tenant.Branches
                    .AsNoTracking()
                    .Where(b => b.BranchId == resolvedBranchId.Value && b.IsActive)
                    .Select(b => b.BranchName)
                    .FirstOrDefaultAsync();

                if (resolvedBranchName is null)
                    return BadRequest(new { message = $"Branch {resolvedBranchId.Value} not found or is inactive." });
            }

            // ---- Username change (optional) ----
            var requestedUsername = (request.Username ?? string.Empty).Trim();
            Credential? credential = null;
            string? oldUsername = null;
            CompanyUser? routingRow = null;
            bool usernameChanging = false;

            if (!string.IsNullOrEmpty(requestedUsername))
            {
                if (requestedUsername.Length < 3)
                    return BadRequest(new { message = "Username must be at least 3 characters." });

                credential = await tenant.Credentials
                    .FirstOrDefaultAsync(c => c.EmployeeId == employee.EmployeeId);

                if (credential is null)
                    return BadRequest(new { message = "This employee has no credential to update." });

                if (!string.Equals(credential.Username, requestedUsername, StringComparison.Ordinal))
                {
                    usernameChanging = true;
                    oldUsername = credential.Username;

                    // Uniqueness within the tenant.
                    var takenInTenant = await tenant.Credentials
                        .AnyAsync(c => c.Username == requestedUsername && c.EmployeeId != employee.EmployeeId);

                    if (takenInTenant)
                        return Conflict(new { message = "That username is already taken." });

                    // Global uniqueness in the master routing table.
                    var takenInMaster = await _master.CompanyUsers
                        .AnyAsync(cu => cu.Username == requestedUsername);

                    if (takenInMaster)
                        return Conflict(new { message = "That username is already taken (by another tenant)." });

                    routingRow = await _master.CompanyUsers
                        .FirstOrDefaultAsync(cu => cu.CompanyId == companyId && cu.Username == oldUsername);

                    if (routingRow is null)
                    {
                        // The routing row is missing — the account can't log in
                        // anyway. Rather than silently create one, tell the admin
                        // to run the sync-company-users dev endpoint first.
                        return Conflict(new
                        {
                            message = "This account has no login routing entry. Run the sync-company-users endpoint, then retry."
                        });
                    }

                    // ---- Step 1: update the master routing row ----
                    routingRow.Username = requestedUsername;
                    try
                    {
                        await _master.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
                    }

                    credential.Username = requestedUsername;
                    credential.UpdatedAt = DateTime.UtcNow;
                }
            }

            employee.FirstName = request.FirstName.Trim();
            employee.LastName = request.LastName.Trim();
            employee.ContactNumber = cleanedPhone;
            employee.Email = cleanedEmail;
            employee.Role = requestedRole;
            employee.BranchId = resolvedBranchId;
            employee.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Rollback the master username change if the tenant write failed.
                if (usernameChanging && routingRow is not null && oldUsername is not null)
                {
                    try
                    {
                        routingRow.Username = oldUsername;
                        await _master.SaveChangesAsync();
                    }
                    catch
                    {
                        // Compensating rollback failed. The two tables are now
                        // out of sync for this user, and login will fail until
                        // the username is manually realigned in the master DB.
                    }
                }

                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            credential = await tenant.Credentials
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.EmployeeId == employee.EmployeeId);

            return Ok(new EmployeeResponseDto
            {
                EmployeeId = employee.EmployeeId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                FullName = employee.FullName,
                ContactNumber = employee.ContactNumber,
                Email = employee.Email,
                Role = employee.Role,
                IsActive = employee.IsActive,
                BranchId = employee.BranchId,
                BranchName = resolvedBranchName,
                Username = credential?.Username,
                LastLoginDate = credential?.LastLoginDate,
                HasCredential = credential != null
            });
        }

        // POST api/tenant/{companyId}/employees/{id}/deactivate
        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> DeactivateEmployee(int companyId, int id)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can deactivate employees." });

            var callerId = GetCallerEmployeeId();
            if (callerId == id)
                return BadRequest(new { message = "You cannot deactivate your own account." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var employee = await tenant.Employees
                .Include(e => e.Credential)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee is null)
                return NotFound();

            if (employee.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                employee.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Admin accounts cannot be deactivated from this console." });

            employee.IsActive = false;
            employee.UpdatedAt = DateTime.UtcNow;

            if (employee.Credential is not null)
            {
                employee.Credential.IsActive = false;
                employee.Credential.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // The CompanyUser routing row is intentionally left in place —
            // the tenant-side Credential.IsActive=false check is what refuses
            // the login. Leaving the routing row means we don't have to
            // recreate it on reactivate, and the username stays reserved so
            // another tenant can't take it.
            return NoContent();
        }

        // POST api/tenant/{companyId}/employees/{id}/reactivate
        [HttpPost("{id:int}/reactivate")]
        public async Task<IActionResult> ReactivateEmployee(int companyId, int id)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can reactivate employees." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var employee = await tenant.Employees
                .Include(e => e.Credential)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee is null)
                return NotFound();

            if (employee.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                employee.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Admin accounts cannot be reactivated from this console." });

            employee.IsActive = true;
            employee.UpdatedAt = DateTime.UtcNow;

            if (employee.Credential is not null)
            {
                employee.Credential.IsActive = true;
                employee.Credential.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return NoContent();
        }

        // POST api/tenant/{companyId}/employees/{id}/reset-password
        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(
            int companyId,
            int id,
            ResetPasswordRequest request)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can reset passwords." });

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 4)
                return BadRequest(new { message = "Password must be at least 4 characters." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var credential = await tenant.Credentials
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.EmployeeId == id);

            if (credential is null)
                return NotFound(new { message = "This employee has no credential to reset." });

            if (credential.Employee is not null &&
                (credential.Employee.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                 credential.Employee.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
                return StatusCode(403, new { message = "Admin passwords cannot be reset from this console." });

            if (credential.Employee is not null && !credential.Employee.IsActive)
                return StatusCode(403, new { message = "Inactive employees cannot have their password reset. Reactivate them first." });

            credential.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            credential.FailedLoginAttempts = 0;
            credential.IsLocked = false;
            credential.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return NoContent();
        }
    }
}