using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.api.Services;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly JwtTokenService _jwt;

        public AuthController(ITenantDbContextFactory tenantFactory, JwtTokenService jwt)
        {
            _tenantFactory = tenantFactory;
            _jwt = jwt;
        }

        // POST api/tenant/{companyId}/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(int companyId, LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Look up the credential by username
            var credential = await tenant.Credentials
                .FirstOrDefaultAsync(c => c.Username == request.Username);

            // Same message whether the username doesn't exist or the password is wrong —
            // do NOT reveal which one is wrong (standard practice).
            const string invalidMessage = "Invalid username or password.";

            if (credential is null)
                return Unauthorized(new { message = invalidMessage });

            if (!credential.IsActive)
                return Unauthorized(new { message = "This account has been deactivated." });

            if (credential.IsLocked)
                return Unauthorized(new { message = "This account is locked. Contact an administrator." });

            // Verify password against BCrypt hash
            bool passwordOk;
            try
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash);
            }
            catch
            {
                // Malformed hash in DB — treat as wrong password.
                passwordOk = false;
            }

            if (!passwordOk)
            {
                // Increment failed attempts; lock at 5
                credential.FailedLoginAttempts++;
                if (credential.FailedLoginAttempts >= 5)
                    credential.IsLocked = true;
                credential.UpdatedAt = DateTime.UtcNow;

                await tenant.SaveChangesAsync();

                return Unauthorized(new { message = invalidMessage });
            }

            // Success — reset counters, stamp login time
            credential.FailedLoginAttempts = 0;
            credential.LastLoginDate = DateTime.UtcNow;
            credential.UpdatedAt = DateTime.UtcNow;

            // Load the employee for the token
            var employee = await tenant.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == credential.EmployeeId);

            if (employee is null)
                return Unauthorized(new { message = "Employee record not found." });

            if (!employee.IsActive)
                return Unauthorized(new { message = "This employee is inactive." });

            await tenant.SaveChangesAsync();

            // Issue token
            var token = _jwt.CreateToken(employee, companyId);
            var expiresAt = _jwt.GetExpiryUtc();

            return Ok(new LoginResponse
            {
                Token = token,
                EmployeeId = employee.EmployeeId,
                CompanyId = companyId,
                Role = employee.Role,
                FullName = employee.FullName,
                ExpiresAt = expiresAt,
            });
        }
    }
}