using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.api.Services;
using CRM_Morcaffein.infrastructure.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Authentication for SuperAdmin accounts. Separate from the tenant
    /// AuthController because SuperAdmins have no tenant context — no companyId
    /// in the URL, no ITenantDbContextFactory, no employee lookup. Only the
    /// master DB is queried.
    /// </summary>
    [ApiController]
    [Route("api/superadmin/auth")]
    public class SuperAdminAuthController : ControllerBase
    {
        private readonly MasterCrmDbContext _master;
        private readonly JwtTokenService _jwt;

        public SuperAdminAuthController(MasterCrmDbContext master, JwtTokenService jwt)
        {
            _master = master;
            _jwt = jwt;
        }

        // POST api/superadmin/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<SuperAdminLoginResponse>> Login(SuperAdminLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            var admin = await _master.SuperAdmins
                .FirstOrDefaultAsync(s => s.Username == request.Username);

            const string invalidMessage = "Invalid username or password.";

            if (admin is null)
                return Unauthorized(new { message = invalidMessage });

            if (!admin.IsActive)
                return Unauthorized(new { message = "This SuperAdmin account is inactive." });

            bool passwordOk;
            try
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash);
            }
            catch
            {
                passwordOk = false;
            }

            if (!passwordOk)
                return Unauthorized(new { message = invalidMessage });

            admin.LastLoginDate = DateTime.UtcNow;
            admin.UpdatedAt = DateTime.UtcNow;
            await _master.SaveChangesAsync();

            var token = _jwt.CreateSuperAdminToken(admin);
            var expiresAt = _jwt.GetExpiryUtc();

            return Ok(new SuperAdminLoginResponse
            {
                Token = token,
                SuperAdminId = admin.SuperAdminId,
                Username = admin.Username,
                FullName = admin.FullName,
                ExpiresAt = expiresAt,
            });
        }
    }
}