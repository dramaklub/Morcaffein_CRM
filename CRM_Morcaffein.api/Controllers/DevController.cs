using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Development-only utility endpoints. NOT for production use. This controller
    /// should be deleted once the one-time migrations are done.
    /// </summary>
    [ApiController]
    [Route("api/tenant/{companyId:int}/dev")]
    public class DevController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public DevController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        // POST api/tenant/{companyId}/dev/rehash-credentials
        //
        // One-time migration: reads every credential row whose PasswordHash does NOT
        // already look like a BCrypt hash, hashes its current value with BCrypt, and
        // saves. Safe to run multiple times — rows that are already hashed are skipped.
        [HttpPost("rehash-credentials")]
        public async Task<ActionResult> RehashCredentials(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var credentials = await tenant.Credentials.ToListAsync();

            int updated = 0;
            int skipped = 0;

            foreach (var cred in credentials)
            {
                // BCrypt hashes start with $2a$, $2b$, or $2y$ and are 60 characters long.
                // Anything else is presumed to be plaintext.
                var looksHashed = cred.PasswordHash.StartsWith("$2") && cred.PasswordHash.Length == 60;

                if (looksHashed)
                {
                    skipped++;
                    continue;
                }

                // Hash the current plaintext value and overwrite.
                cred.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cred.PasswordHash);
                cred.UpdatedAt = DateTime.UtcNow;
                updated++;
            }

            if (updated > 0)
                await tenant.SaveChangesAsync();

            return Ok(new
            {
                message = "Rehash complete.",
                updated,
                skipped,
                total = credentials.Count,
            });
        }
    }
}