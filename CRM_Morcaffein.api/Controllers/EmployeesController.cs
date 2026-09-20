using System.Security.Claims;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    [ApiController]
    [Authorize]   // any authenticated employee can hit this controller...
    [Route("api/tenant/{companyId:int}/employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public EmployeesController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
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

            // Role — Admin cannot create Admins
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

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Uniqueness of username
            var usernameTaken = await tenant.Credentials
                .AnyAsync(c => c.Username == request.Username.Trim());
            if (usernameTaken)
                return Conflict(new { message = "That username is already taken." });

            var employee = new Employee
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                ContactNumber = cleanedPhone,
                Email = cleanedEmail,
                Role = requestedRole,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var credential = new Credential
            {
                Username = request.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Employee = employee,
                FailedLoginAttempts = 0,
                IsLocked = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            tenant.Employees.Add(employee);
            tenant.Credentials.Add(credential);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
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

            // Guard: cannot edit an Admin's account
            if (employee.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                employee.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Admin accounts cannot be edited from this console." });

            // Guard: cannot edit an inactive employee. Reactivate them first, then edit.
            if (!employee.IsActive)
                return StatusCode(403, new { message = "Inactive employees cannot be edited. Reactivate them first." });

            // Guard: cannot change someone's role to Admin
            var requestedRole = (request.Role ?? "").Trim();
            if (requestedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                requestedRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only the SuperAdmin can promote to Admin." });
            if (!requestedRole.Equals("Barista", StringComparison.OrdinalIgnoreCase) &&
                !requestedRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Role must be Barista or Manager." });

            // ---- Username change (optional) ----
            // If Username is provided, validate and update the credential in the same
            // SaveChanges. If it's null/empty, leave it alone. If it matches the current
            // value, that's a no-op — no error, no unnecessary update.
            var requestedUsername = (request.Username ?? string.Empty).Trim();
            Credential? credential = null;

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
                    // Uniqueness check — case-sensitive, matching CreateEmployee's AnyAsync.
                    var taken = await tenant.Credentials
                        .AnyAsync(c => c.Username == requestedUsername && c.EmployeeId != employee.EmployeeId);

                    if (taken)
                        return Conflict(new { message = "That username is already taken." });

                    credential.Username = requestedUsername;
                    credential.UpdatedAt = DateTime.UtcNow;
                }
            }

            employee.FirstName = request.FirstName.Trim();
            employee.LastName = request.LastName.Trim();
            employee.ContactNumber = cleanedPhone;
            employee.Email = cleanedEmail;
            employee.Role = requestedRole;
            employee.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // Reload the credential (AsNoTracking is fine for the response DTO).
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

            // Guard: cannot reset a password for an inactive employee. Reactivate first.
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