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
    /// Read-only management endpoints for the SuperAdmin console. Writes
    /// (create company, assign plan, record T&C) come later — this pass is
    /// list-only so the shell has something to render.
    ///
    /// [Authorize(Roles = "SuperAdmin")] gates the whole controller — only
    /// tokens issued by SuperAdminAuthController can call these endpoints.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    [Route("api/superadmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly MasterCrmDbContext _master;
        private readonly ITenantDbContextFactory _tenantFactory;

        public SuperAdminController(
            MasterCrmDbContext master,
            ITenantDbContextFactory tenantFactory)
        {
            _master = master;
            _tenantFactory = tenantFactory;
        }

        // GET api/superadmin/companies
        [HttpGet("companies")]
        public async Task<ActionResult<List<SuperAdminCompanyDto>>> GetCompanies()
        {
            var companies = await _master.Companies
                .AsNoTracking()
                .Include(c => c.CurrentPlan)
                .OrderBy(c => c.CompanyId)
                .Select(c => new SuperAdminCompanyDto
                {
                    CompanyId = c.CompanyId,
                    CompanyCode = c.CompanyCode,
                    CompanyName = c.CompanyName,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    CurrentPlanId = c.CurrentPlanId,
                    CurrentPlanName = c.CurrentPlan != null ? c.CurrentPlan.Name : null,
                })
                .ToListAsync();

            return Ok(companies);
        }

        // GET api/superadmin/plans
        [HttpGet("plans")]
        public async Task<ActionResult<List<SuperAdminPlanDto>>> GetPlans()
        {
            var plans = await _master.Plans
                .AsNoTracking()
                .OrderBy(p => p.PlanId)
                .Select(p => new SuperAdminPlanDto
                {
                    PlanId = p.PlanId,
                    Name = p.Name,
                    Description = p.Description,
                    ModulesCsv = p.ModulesCsv,
                    MonthlyPrice = p.MonthlyPrice,
                    IsActive = p.IsActive,
                })
                .ToListAsync();

            return Ok(plans);
        }
        // GET api/superadmin/dashboard
        //
        // One combined payload for the SuperAdmin dashboard: platform summary,
        // companies-per-plan distribution, and the most recent subscription
        // assignments. All three queries run against the master DB only.
        [HttpGet("dashboard")]
        public async Task<ActionResult<SuperAdminDashboardDto>> GetDashboard()
        {
            // ---- 1. Summary numbers ----
            var companies = await _master.Companies
                .AsNoTracking()
                .Include(c => c.CurrentPlan)
                .ToListAsync();

            var totalCompanies = companies.Count;
            var activeSubscriptions = companies.Count(c => c.CurrentPlanId.HasValue);
            var mrr = companies
                .Where(c => c.CurrentPlanId.HasValue && c.CurrentPlan is not null)
                .Sum(c => c.CurrentPlan!.MonthlyPrice);

            // ---- 2. Companies per plan ----
            // Group by the current plan, plus a synthetic "No Plan" bucket
            // for companies with CurrentPlanId == null. Ordered by plan
            // so the chart legend reads consistently (Basic, Standard, Full).
            var perPlan = companies
                .Where(c => c.CurrentPlanId.HasValue && c.CurrentPlan is not null)
                .GroupBy(c => new { c.CurrentPlanId, c.CurrentPlan!.Name })
                .Select(g => new CompaniesPerPlanPoint
                {
                    PlanId = g.Key.CurrentPlanId,
                    PlanName = g.Key.Name,
                    CompanyCount = g.Count(),
                })
                .OrderBy(p => p.PlanId)
                .ToList();

            // No-plan bucket — always included, even if zero, so the chart
            // renders the fourth bar consistently.
            var noPlanCount = companies.Count(c => !c.CurrentPlanId.HasValue);
            perPlan.Add(new CompaniesPerPlanPoint
            {
                PlanId = null,
                PlanName = "No Plan",
                CompanyCount = noPlanCount,
            });

            // ---- 3. Recent subscriptions ----
            var recent = await _master.Subscriptions
                .AsNoTracking()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .OrderByDescending(s => s.StartedAt)
                .Take(10)
                .Select(s => new RecentSubscriptionPoint
                {
                    SubscriptionId = s.SubscriptionId,
                    CompanyName = s.Company != null ? s.Company.CompanyName : "—",
                    PlanName = s.Plan != null ? s.Plan.Name : "—",
                    PlanMonthlyPrice = s.Plan != null ? s.Plan.MonthlyPrice : 0m,
                    StartedAt = s.StartedAt,
                    TermsAcceptedBy = s.TermsAcceptedBy,
                })
                .ToListAsync();

            // ---- Compose ----
            return Ok(new SuperAdminDashboardDto
            {
                Summary = new SuperAdminDashboardSummary
                {
                    TotalCompanies = totalCompanies,
                    ActiveSubscriptions = activeSubscriptions,
                    MonthlyRecurringRevenue = mrr,
                },
                CompaniesPerPlan = perPlan,
                RecentSubscriptions = recent,
            });
        }

        // POST api/superadmin/companies/{id}/assign-plan
        //
        // Assigns a plan to a company. Two side effects in one transaction:
        //   1. Marks any currently-active Subscription for this company as ended
        //      (stamps EndedAt = now).
        //   2. Inserts a new Subscription row with StartedAt = now, EndedAt = null.
        //   3. Updates Company.CurrentPlanId to the new plan.
        //
        // All three happen together — if any fails, the transaction rolls back.
        [HttpPost("companies/{id:int}/assign-plan")]
        public async Task<ActionResult<SubscriptionResponseDto>> AssignPlan(
            int id,
            AssignPlanRequest request)
        {
            if (request.PlanId <= 0)
                return BadRequest(new { message = "A valid plan is required." });

            if (string.IsNullOrWhiteSpace(request.TermsAcceptedBy))
                return BadRequest(new { message = "Who accepted the T&C is required." });

            var company = await _master.Companies
                .FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company is null)
                return NotFound(new { message = $"Company {id} not found." });

            var plan = await _master.Plans
                .FirstOrDefaultAsync(p => p.PlanId == request.PlanId);
            if (plan is null)
                return BadRequest(new { message = $"Plan {request.PlanId} not found." });

            if (!plan.IsActive)
                return BadRequest(new { message = $"Plan \"{plan.Name}\" is inactive and cannot be assigned." });

            var now = DateTime.UtcNow;

            // Close out the currently-active subscription (if any).
            var currentSubscription = await _master.Subscriptions
                .FirstOrDefaultAsync(s => s.CompanyId == id && s.EndedAt == null);
            if (currentSubscription is not null)
            {
                currentSubscription.EndedAt = now;
                currentSubscription.UpdatedAt = now;
            }

            // Open a new subscription.
            var subscription = new CRM_Morcaffein.domain.entities.Subscription
            {
                CompanyId = id,
                PlanId = request.PlanId,
                StartedAt = now,
                EndedAt = null,
                TermsAcceptedBy = request.TermsAcceptedBy.Trim(),
                TermsAcceptedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _master.Subscriptions.Add(subscription);

            // Update the fast-path field on Company.
            company.CurrentPlanId = request.PlanId;

            try
            {
                await _master.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // Reload the navs we need for the response.
            await _master.Entry(subscription).Reference(s => s.Company).LoadAsync();
            await _master.Entry(subscription).Reference(s => s.Plan).LoadAsync();

            return Ok(MapSubscriptionToDto(subscription));
        }

        // GET api/superadmin/companies/{id}/subscriptions
        //
        // Returns the full subscription history for a company, newest first.
        // The current subscription is the row with EndedAt == null.
        [HttpGet("companies/{id:int}/subscriptions")]
        public async Task<ActionResult<List<SubscriptionResponseDto>>> GetSubscriptions(int id)
        {
            var companyExists = await _master.Companies
                .AnyAsync(c => c.CompanyId == id);
            if (!companyExists)
                return NotFound(new { message = $"Company {id} not found." });

            var subscriptions = await _master.Subscriptions
                .AsNoTracking()
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .Where(s => s.CompanyId == id)
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new SubscriptionResponseDto
                {
                    SubscriptionId = s.SubscriptionId,
                    CompanyId = s.CompanyId,
                    CompanyName = s.Company != null ? s.Company.CompanyName : "—",
                    PlanId = s.PlanId,
                    PlanName = s.Plan != null ? s.Plan.Name : "—",
                    PlanModulesCsv = s.Plan != null ? s.Plan.ModulesCsv : string.Empty,
                    PlanMonthlyPrice = s.Plan != null ? s.Plan.MonthlyPrice : 0m,
                    StartedAt = s.StartedAt,
                    EndedAt = s.EndedAt,
                    TermsAcceptedBy = s.TermsAcceptedBy,
                    TermsAcceptedAt = s.TermsAcceptedAt,
                })
                .ToListAsync();

            return Ok(subscriptions);
        }

        // GET api/superadmin/terms
        //
        // Returns the list of T&C documents — one per plan. No body text;
        // the client fetches the full document per-card on click.
        [HttpGet("terms")]
        public async Task<ActionResult<List<TermsSummaryDto>>> GetTermsList()
        {
            var terms = await _master.TermsAndConditions
                .AsNoTracking()
                .Include(t => t.Plan)
                .OrderBy(t => t.PlanId)
                .Select(t => new TermsSummaryDto
                {
                    TermsId = t.TermsId,
                    PlanId = t.PlanId,
                    PlanName = t.Plan != null ? t.Plan.Name : null,
                    Title = t.Title,
                    Version = t.Version,
                    EffectiveFrom = t.EffectiveFrom,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                })
                .ToListAsync();

            return Ok(terms);
        }

        // GET api/superadmin/terms/{id}
        //
        // Returns a full T&C document including the body text.
        [HttpGet("terms/{id:int}")]
        public async Task<ActionResult<TermsDetailDto>> GetTermsDetail(int id)
        {
            var terms = await _master.TermsAndConditions
                .AsNoTracking()
                .Include(t => t.Plan)
                .Where(t => t.TermsId == id)
                .Select(t => new TermsDetailDto
                {
                    TermsId = t.TermsId,
                    PlanId = t.PlanId,
                    PlanName = t.Plan != null ? t.Plan.Name : null,
                    Title = t.Title,
                    Body = t.Body,
                    Version = t.Version,
                    EffectiveFrom = t.EffectiveFrom,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                })
                .FirstOrDefaultAsync();

            if (terms is null)
                return NotFound(new { message = $"Terms document {id} not found." });

            return Ok(terms);
        }

        // =====================================================================
        // COMPANY ADMIN ACCOUNT — the primary login the SuperAdmin provisions.
        // One Admin per company. All three endpoints work cross-DB: they touch
        // the master (CompanyUsers routing row) AND the tenant (Credentials +
        // Employees). The master write is always first, tenant second, with
        // rollback on tenant failure — same pattern as EmployeesController.
        // =====================================================================

        // GET api/superadmin/companies/{id}/admin
        //
        // Returns the company's Admin account. If none exists,
        // { HasAdmin: false } is returned and the client shows a "Create"
        // button instead of an edit form.
        [HttpGet("companies/{id:int}/admin")]
        public async Task<ActionResult<SuperAdminAdminAccountDto>> GetCompanyAdmin(int id)
        {
            var companyExists = await _master.Companies
                .AnyAsync(c => c.CompanyId == id);
            if (!companyExists)
                return NotFound(new { message = $"Company {id} not found." });

            await using var tenant = await _tenantFactory.CreateAsync(id);

            var admin = await tenant.Employees
                .AsNoTracking()
                .Include(e => e.Credential)
                .Where(e => e.Role == "Admin")
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Email,
                    e.ContactNumber,
                    e.IsActive,
                    Username = e.Credential != null ? e.Credential.Username : null,
                    IsLocked = e.Credential != null ? (bool?)e.Credential.IsLocked : null,
                    LastLoginDate = e.Credential != null ? e.Credential.LastLoginDate : null,
                })
                .FirstOrDefaultAsync();

            if (admin is null)
                return Ok(new SuperAdminAdminAccountDto { HasAdmin = false });

            return Ok(new SuperAdminAdminAccountDto
            {
                HasAdmin = true,
                EmployeeId = admin.EmployeeId,
                Username = admin.Username,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                FullName = (admin.FirstName + " " + admin.LastName).Trim(),
                Email = string.IsNullOrWhiteSpace(admin.Email) ? null : admin.Email,
                ContactNumber = admin.ContactNumber,
                IsActive = admin.IsActive,
                IsLocked = admin.IsLocked,
                LastLoginDate = admin.LastLoginDate,
            });
        }

        // POST api/superadmin/companies/{id}/admin
        //
        // Creates the company's first Admin account. Blocked if an Admin
        // already exists — use PUT to edit instead.
        //
        // Cross-DB write order:
        //   1. Insert CompanyUser (master) — reserves the username globally.
        //   2. Insert Employee + Credential (tenant).
        //   3. Roll back step 1 if step 2 fails.
        [HttpPost("companies/{id:int}/admin")]
        public async Task<ActionResult<SuperAdminAdminAccountDto>> CreateCompanyAdmin(
            int id,
            CreateCompanyAdminRequest request)
        {
            // ---- Validation ----
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length < 3)
                return BadRequest(new { message = "Username must be at least 3 characters." });

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
                return BadRequest(new { message = "Password must be at least 4 characters." });

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

            var trimmedUsername = request.Username.Trim();

            var company = await _master.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company is null)
                return NotFound(new { message = $"Company {id} not found." });

            // ---- Uniqueness: master routing table ----
            var takenInMaster = await _master.CompanyUsers
                .AnyAsync(cu => cu.Username == trimmedUsername);
            if (takenInMaster)
                return Conflict(new { message = "That username is already taken (by another tenant)." });

            await using var tenant = await _tenantFactory.CreateAsync(id);

            // ---- Enforce: one Admin per company ----
            var existingAdmin = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Role == "Admin");
            if (existingAdmin is not null)
                return Conflict(new { message = "This company already has an Admin account. Use edit instead." });

            // ---- Uniqueness: tenant credentials ----
            var takenInTenant = await tenant.Credentials
                .AsNoTracking()
                .AnyAsync(c => c.Username == trimmedUsername);
            if (takenInTenant)
                return Conflict(new { message = "That username is already taken." });

            // ---- Step 1: master routing row ----
            var routingRow = new CompanyUser
            {
                CompanyId = id,
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

            // ---- Step 2: tenant employee + credential ----
            var employee = new Employee
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                ContactNumber = cleanedPhone,
                Email = cleanedEmail,
                Role = "Admin",
                BranchId = null,   // Admins are all-branch by convention
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

            tenant.Employees.Add(employee);
            tenant.Credentials.Add(credential);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Roll back the routing row so the username is free again.
                try
                {
                    _master.CompanyUsers.Remove(routingRow);
                    await _master.SaveChangesAsync();
                }
                catch { /* best-effort; sync-company-users can clean up stragglers */ }

                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new SuperAdminAdminAccountDto
            {
                HasAdmin = true,
                EmployeeId = employee.EmployeeId,
                Username = credential.Username,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                FullName = (employee.FirstName + " " + employee.LastName).Trim(),
                Email = string.IsNullOrWhiteSpace(employee.Email) ? null : employee.Email,
                ContactNumber = employee.ContactNumber,
                IsActive = employee.IsActive,
                IsLocked = false,
                LastLoginDate = null,
            });
        }

        // PUT api/superadmin/companies/{id}/admin
        //
        // Updates the Admin's username and/or password. Both fields are
        // optional; blank/null means "leave unchanged."
        //
        // Cross-DB write order (username change):
        //   1. Update CompanyUser.Username (master).
        //   2. Update Credential.Username (tenant).
        //   3. Roll back step 1 if step 2 fails.
        //
        // Password change touches only the tenant Credential row — no
        // master-side change.
        [HttpPut("companies/{id:int}/admin")]
        public async Task<ActionResult<SuperAdminAdminAccountDto>> UpdateCompanyAdmin(
            int id,
            UpdateCompanyAdminRequest request)
        {
            var companyExists = await _master.Companies
                .AnyAsync(c => c.CompanyId == id);
            if (!companyExists)
                return NotFound(new { message = $"Company {id} not found." });

            await using var tenant = await _tenantFactory.CreateAsync(id);

            var admin = await tenant.Employees
                .Include(e => e.Credential)
                .FirstOrDefaultAsync(e => e.Role == "Admin");

            if (admin is null)
                return NotFound(new { message = "This company has no Admin account." });

            if (admin.Credential is null)
                return Conflict(new { message = "This Admin has no login credentials." });

            var requestedUsername = (request.Username ?? "").Trim();
            var requestedPassword = request.NewPassword ?? "";

            var usernameChanging = false;
            string? oldUsername = null;
            CompanyUser? routingRow = null;

            // ---- Username change ----
            if (!string.IsNullOrEmpty(requestedUsername) &&
                !string.Equals(admin.Credential.Username, requestedUsername, StringComparison.Ordinal))
            {
                if (requestedUsername.Length < 3)
                    return BadRequest(new { message = "Username must be at least 3 characters." });

                // Master uniqueness
                var takenInMaster = await _master.CompanyUsers
                    .AnyAsync(cu => cu.Username == requestedUsername);
                if (takenInMaster)
                    return Conflict(new { message = "That username is already taken (by another tenant)." });

                // Tenant uniqueness
                var takenInTenant = await tenant.Credentials
                    .AsNoTracking()
                    .AnyAsync(c => c.Username == requestedUsername && c.EmployeeId != admin.EmployeeId);
                if (takenInTenant)
                    return Conflict(new { message = "That username is already taken." });

                usernameChanging = true;
                oldUsername = admin.Credential.Username;

                routingRow = await _master.CompanyUsers
                    .FirstOrDefaultAsync(cu => cu.CompanyId == id && cu.Username == oldUsername);

                if (routingRow is null)
                {
                    return Conflict(new
                    {
                        message = "This Admin has no login routing entry. Run the sync-company-users endpoint, then retry."
                    });
                }

                // Step 1: master
                routingRow.Username = requestedUsername;
                try
                {
                    await _master.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
                }

                // Step 2 (partial): tenant credential
                admin.Credential.Username = requestedUsername;
                admin.Credential.UpdatedAt = DateTime.UtcNow;
            }

            // ---- Password change ----
            if (!string.IsNullOrWhiteSpace(requestedPassword))
            {
                if (requestedPassword.Length < 4)
                    return BadRequest(new { message = "Password must be at least 4 characters." });

                admin.Credential.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestedPassword);
                admin.Credential.FailedLoginAttempts = 0;
                admin.Credential.IsLocked = false;
                admin.Credential.UpdatedAt = DateTime.UtcNow;
            }

            // ---- Save tenant (may contain username change and/or password change) ----
            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Roll back master username change.
                if (usernameChanging && routingRow is not null && oldUsername is not null)
                {
                    try
                    {
                        routingRow.Username = oldUsername;
                        await _master.SaveChangesAsync();
                    }
                    catch { /* both writes failed; manual reconciliation needed */ }
                }

                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new SuperAdminAdminAccountDto
            {
                HasAdmin = true,
                EmployeeId = admin.EmployeeId,
                Username = admin.Credential.Username,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                FullName = (admin.FirstName + " " + admin.LastName).Trim(),
                Email = string.IsNullOrWhiteSpace(admin.Email) ? null : admin.Email,
                ContactNumber = admin.ContactNumber,
                IsActive = admin.IsActive,
                IsLocked = admin.Credential.IsLocked,
                LastLoginDate = admin.Credential.LastLoginDate,
            });
        }

        // ---- helpers ----

        private static SubscriptionResponseDto MapSubscriptionToDto(
            CRM_Morcaffein.domain.entities.Subscription s) => new()
            {
                SubscriptionId = s.SubscriptionId,
                CompanyId = s.CompanyId,
                CompanyName = s.Company?.CompanyName ?? "—",
                PlanId = s.PlanId,
                PlanName = s.Plan?.Name ?? "—",
                PlanModulesCsv = s.Plan?.ModulesCsv ?? string.Empty,
                PlanMonthlyPrice = s.Plan?.MonthlyPrice ?? 0m,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                TermsAcceptedBy = s.TermsAcceptedBy,
                TermsAcceptedAt = s.TermsAcceptedAt,
            };
    }
}