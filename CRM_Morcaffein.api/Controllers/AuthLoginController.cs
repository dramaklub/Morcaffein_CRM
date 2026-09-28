using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.api.Services;
using CRM_Morcaffein.infrastructure.data;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Tenant-agnostic login. Unlike the tenant-scoped endpoint
    /// (POST /api/tenant/{companyId}/auth/login), this one takes only
    /// username + password and resolves which tenant the user belongs
    /// to via the master DB's CompanyUsers routing table.
    ///
    /// This is the login the client uses going forward. The old
    /// tenant-scoped endpoint can be kept for .http testing or removed
    /// once the client migration is complete.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/auth")]
    public class AuthLoginController : ControllerBase
    {
        private readonly MasterCrmDbContext _master;
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly JwtTokenService _jwt;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuthLoginController> _logger;

        public AuthLoginController(
            MasterCrmDbContext master,
            ITenantDbContextFactory tenantFactory,
            JwtTokenService jwt,
            IServiceScopeFactory scopeFactory,
            ILogger<AuthLoginController> logger)
        {
            _master = master;
            _tenantFactory = tenantFactory;
            _jwt = jwt;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            const string invalidMessage = "Invalid username or password.";

            // ---- 1. Route the username to a tenant via the master DB ----
            var routing = await _master.CompanyUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(cu => cu.Username == request.Username);

            if (routing is null)
                return Unauthorized(new { message = invalidMessage });

            // ---- 2. Resolve that tenant's DB ----
            await using var tenant = await _tenantFactory.CreateAsync(routing.CompanyId);

            // ---- 3. Look up the credential in the tenant DB ----
            var credential = await tenant.Credentials
                .FirstOrDefaultAsync(c => c.Username == request.Username);

            if (credential is null)
                return Unauthorized(new { message = invalidMessage });

            if (!credential.IsActive)
                return Unauthorized(new { message = "This account has been deactivated." });

            if (credential.IsLocked)
                return Unauthorized(new { message = "This account is locked. Contact an administrator." });

            // ---- 4. Verify password against BCrypt hash ----
            bool passwordOk;
            try
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash);
            }
            catch
            {
                passwordOk = false;
            }

            if (!passwordOk)
            {
                credential.FailedLoginAttempts++;
                if (credential.FailedLoginAttempts >= 5)
                    credential.IsLocked = true;
                credential.UpdatedAt = DateTime.UtcNow;

                await tenant.SaveChangesAsync();

                return Unauthorized(new { message = invalidMessage });
            }

            // ---- 5. Success — reset counters, stamp last login, load employee ----
            credential.FailedLoginAttempts = 0;
            credential.LastLoginDate = DateTime.UtcNow;
            credential.UpdatedAt = DateTime.UtcNow;

            var employee = await tenant.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == credential.EmployeeId);

            if (employee is null)
                return Unauthorized(new { message = "Employee record not found." });

            if (!employee.IsActive)
                return Unauthorized(new { message = "This employee is inactive." });

            await tenant.SaveChangesAsync();

            // ---- 6. Issue token ----
            var token = _jwt.CreateToken(employee, routing.CompanyId);
            var expiresAt = _jwt.GetExpiryUtc();

            // ---- 7. Fire-and-forget sync for this company ----
            // Kicks off a sync pass for the tenant the user just logged into.
            // The point is to push anything that's been pending since their
            // last session, without making the login wait for a cloud round-
            // trip. Failures are captured inside SyncService and reported
            // via SyncStatusStore (which the topbar indicator polls).
            //
            // We resolve SyncService from a fresh scope because the request
            // scope is disposed when this endpoint returns — the background
            // task outlives it.
            var companyIdForSync = routing.CompanyId;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
                    var result = await syncService.RunTenantAsync(companyIdForSync);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "[Login-sync] Company {CompanyId}: OK. Synced {Synced}, took {Ms}ms.",
                            companyIdForSync,
                            result.TotalRowsSynced,
                            (result.FinishedAt - result.StartedAt).TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Login-sync] Company {CompanyId}: FAILED — {Error}",
                            companyIdForSync, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    // SyncService already captures exceptions, so this is
                    // belt-and-suspenders for bugs in the service itself.
                    _logger.LogError(ex,
                        "[Login-sync] Company {CompanyId}: unexpected error.",
                        companyIdForSync);
                }
            });

            return Ok(new LoginResponse
            {
                Token = token,
                EmployeeId = employee.EmployeeId,
                CompanyId = routing.CompanyId,
                Role = employee.Role,
                FullName = employee.FullName,
                BranchId = employee.BranchId,
                ExpiresAt = expiresAt,
            });
        }
    }
}