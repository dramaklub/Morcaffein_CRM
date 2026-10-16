using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.api.Services;
using CRM_Morcaffein.infrastructure.data;
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

        // POST api/dev/sync-company-users
        //
        // Populates master.CompanyUsers from every registered tenant's
        // Credentials table. Idempotent — re-running skips users that are
        // already routed. Detects and reports username collisions across
        // tenants without inserting them.
        //
        // Cross-tenant operation — scans ALL tenants, not just one. The
        // leading slash on the route makes this an absolute path that
        // overrides the controller's "api/tenant/{companyId}/dev" prefix,
        // so it's hit at /api/dev/sync-company-users (no companyId).
        //
        // Called once after seeding tenants, and again whenever a new tenant
        // is provisioned. Not needed on a per-employee basis — new employees
        // are handled by CreateEmployee writing the routing row inline
        // (that patch is separate).
        [HttpPost("/api/dev/sync-company-users")]
        public async Task<ActionResult<CompanyUserSyncDtos>> SyncCompanyUsers(
            [FromServices] MasterCrmDbContext master,
            [FromServices] ITenantDbContextFactory tenantFactory)
        {
            var response = new CompanyUserSyncDtos();

            // ---- 1. Read every tenant's Credentials ----

            var registrations = await master.CompanyDatabases
                .AsNoTracking()
                .Where(cd => cd.IsActive)
                .Select(cd => new { cd.CompanyId })
                .Distinct()
                .ToListAsync();

            // group by username → set of CompanyIds it appears in
            var seen = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            foreach (var reg in registrations)
            {
                await using var tenant = await tenantFactory.CreateAsync(reg.CompanyId);

                var usernames = await tenant.Credentials
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .Select(c => c.Username)
                    .ToListAsync();

                foreach (var u in usernames)
                {
                    if (string.IsNullOrWhiteSpace(u)) continue;
                    response.TotalScanned++;

                    if (!seen.TryGetValue(u, out var companies))
                    {
                        companies = new List<int>();
                        seen[u] = companies;
                    }
                    if (!companies.Contains(reg.CompanyId))
                        companies.Add(reg.CompanyId);
                }
            }

            // ---- 2. Flag duplicates ----

            foreach (var kvp in seen.Where(k => k.Value.Count > 1))
            {
                response.Conflicts.Add(new CompanyUserConflict
                {
                    Username = kvp.Key,
                    CompanyIds = kvp.Value,
                });
            }

            // ---- 3. Insert only the unique ones (skip existing) ----

            var existingUsernames = await master.CompanyUsers
                .AsNoTracking()
                .Select(cu => cu.Username)
                .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in seen)
            {
                // Skip conflicts — can't route an ambiguous username
                if (kvp.Value.Count > 1) continue;

                // Skip if already routed
                if (existingUsernames.Contains(kvp.Key))
                {
                    response.Skipped++;
                    continue;
                }

                master.CompanyUsers.Add(new CRM_Morcaffein.domain.entities.CompanyUser
                {
                    CompanyId = kvp.Value[0],
                    Username = kvp.Key,
                    CreatedAt = DateTime.UtcNow,
                });
                response.Inserted++;
            }

            await master.SaveChangesAsync();

            return Ok(response);
        }

        // POST api/tenant/{companyId}/dev/sync-test-connection
        // Temporary — verifies the cloud context factories can open a connection.
        // Delete once the sync worker is wired up and tested end-to-end.
        [HttpPost("sync-test-connection")]
        public async Task<ActionResult> TestCloudConnection(
            int companyId,
            [FromServices] ICloudTenantDbContextFactory cloudTenantFactory,
            [FromServices] ICloudMasterDbContextFactory cloudMasterFactory)
        {
            var results = new List<string>();

            // ---- Tenant cloud ----
            try
            {
                await using var cloudTenant = await cloudTenantFactory.CreateAsync(companyId);
                var orderCount = await cloudTenant.Orders.CountAsync();
                results.Add($"Cloud tenant {companyId}: OK, Orders count = {orderCount}");
            }
            catch (Exception ex)
            {
                results.Add($"Cloud tenant {companyId}: FAILED — {ex.Message}");
            }

            // ---- Master cloud ----
            try
            {
                using var cloudMaster = cloudMasterFactory.Create();
                var companyCount = await cloudMaster.Companies.CountAsync();
                results.Add($"Cloud master: OK, Companies count = {companyCount}");
            }
            catch (Exception ex)
            {
                results.Add($"Cloud master: FAILED — {ex.Message}");
            }

            return Ok(new { results });
        }

        // POST api/tenant/{companyId}/dev/sync-now
        // Temporary — triggers a full master+tenant sync immediately and returns the result.
        // Will be replaced by the real SyncController once the worker is wired up.
        [HttpPost("sync-now")]
        public async Task<ActionResult> SyncNow(
            int companyId,
            [FromServices] SyncService syncService)
        {
            var tenantResult = await syncService.RunTenantAsync(companyId);
            var masterResult = await syncService.RunMasterAsync();
            return Ok(new { tenant = tenantResult, master = masterResult });
        }

        // POST api/tenant/{companyId}/dev/test-email
        //
        // Sends a plain "hello world" test email to the configured
        // MailKit:DemoRecipient. Used to verify SMTP config without
        // touching the campaign approval flow.
        [HttpPost("test-email")]
        public async Task<ActionResult> TestEmail(
            int companyId,
            [FromServices] IEmailService emailService,
            [FromServices] IConfiguration config)
        {
            var recipient = config["MailKit:DemoRecipient"];
            if (string.IsNullOrWhiteSpace(recipient))
                return BadRequest(new { message = "MailKit:DemoRecipient is not configured." });

            var subject = "[Morcaffein] SMTP test";
            var body =
                "This is a test email from the Morcaffein platform.\n\n" +
                "If you're reading this, the MailKit SMTP pipeline is working.\n\n" +
                "— Morcaffein";

            var (success, error) = await emailService.SendAsync(recipient, subject, body);

            if (!success)
                return StatusCode(500, new { message = $"Send failed: {error}" });

            return Ok(new { message = $"Test email sent to {recipient}." });
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

        // POST api/tenant/{companyId}/dev/seed-transactions
        //
        // Re-seeds ONLY transactional data (orders, campaigns, feedback,
        // point ledger, adjustments). Keeps existing Customers, Employees,
        // Branches, and product catalog.
        //
        // Wipes the transactional tables first by default (wipe=true),
        // producing a fresh, deterministic demo state. Pass wipe=false
        // to append instead.
        //
        // Every generated row has IsSynced = false — the sync worker
        // pushes them to cloud on its next tick.
        [HttpPost("seed-transactions")]
        public async Task<ActionResult> SeedTransactions(
            int companyId,
            [FromQuery] int orders = 200,
            [FromQuery] int feedback = 30,
            [FromQuery] int campaigns = 15,
            [FromQuery] int daysBack = 90,
            [FromQuery] bool wipe = true)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);
            await DatabaseSeeder.SeedTransactionsAsync(
                tenant,
                ordersToCreate: orders,
                feedbackToCreate: feedback,
                campaignsToCreate: campaigns,
                daysBack: daysBack,
                wipe: wipe,
                companyId: companyId);

            return Ok(new
            {
                message = "Transactions seeded.",
                companyId,
                orders,
                feedback,
                campaigns,
                daysBack,
                wipe,
            });
        }
    }
}