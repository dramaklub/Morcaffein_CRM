using CRM_Morcaffein.infrastructure.Data;
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

        // POST api/tenant/{companyId}/dev/seed
        //
        // One-time demo seeder. Adds 3 Baristas + 50 Customers + 200 Orders + 15
        // Campaigns + supporting feedback / ledger / adjustment rows to the tenant
        // DB. ADDITIVE — running twice doubles the seeded data. Remove this
        // endpoint before shipping the API.
        [HttpPost("seed")]
        public async Task<ActionResult> SeedData(
            int companyId,
            [FromQuery] int customers = 50,
            [FromQuery] int orders = 200)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);
            await DatabaseSeeder.SeedAsync(tenant, customers, orders, companyId: companyId);
            return Ok(new { message = "Seeded.", companyId, customers, orders });
        }
    }
}