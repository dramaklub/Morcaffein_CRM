using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tenant/{companyId:int}/customers")]
    public class CustomerController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public CustomerController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        // GET api/tenant/{companyId}/customers?search=...
        // Returns all customers, or filters by name/phone/ID if search is provided.
        // GET api/tenant/{companyId}/customers?search=...&page=1&pageSize=25
        [HttpGet]
        public async Task<ActionResult<PagedResponse<CustomerResponseDto>>> GetCustomers(
             int companyId,
             [FromQuery] string? search,
             [FromQuery] string? activeFilter = null,
             [FromQuery] int page = 1,
             [FromQuery] int pageSize = 25)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 25;
            if (pageSize > 200) pageSize = 200;

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // activeFilter:
            //   null / "" / "active"    -> only IsActive = true (the default)
            //   "inactive"              -> only IsActive = false
            //   "all"                   -> no IsActive filter
            // Anything else falls back to the default (active-only).
            var filter = (activeFilter ?? string.Empty).Trim().ToLowerInvariant();

            var query = tenant.Customers.AsNoTracking();

            query = filter switch
            {
                "inactive" => query.Where(c => !c.IsActive),
                "all" => query,
                _ => query.Where(c => c.IsActive),
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c =>
                    (c.FirstName + " " + c.LastName).Contains(term) ||
                    c.ContactNumber.Contains(term) ||
                    c.CustomerId.ToString() == term);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.CustomerId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerResponseDto
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    FullName = (c.FirstName + " " + c.LastName).Trim(),
                    ContactNumber = c.ContactNumber,
                    Email = c.Email,
                    Birthday = c.Birthday,
                    LastVisitDate = c.LastVisitDate,
                    PointsBalance = c.PointsBalance,
                    Tier = c.PointsBalance >= 500 ? "Gold"
                         : c.PointsBalance >= 200 ? "Silver"
                         : "Regular",
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(new PagedResponse<CustomerResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // GET api/tenant/{companyId}/customers/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerResponseDto>> GetCustomer(int companyId, int id)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var customer = await tenant.Customers
                .AsNoTracking()
                .Where(c => c.CustomerId == id && c.IsActive)
                .Select(c => new CustomerResponseDto
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    FullName = (c.FirstName + " " + c.LastName).Trim(),
                    ContactNumber = c.ContactNumber,
                    Email = c.Email,
                    Birthday = c.Birthday,
                    LastVisitDate = c.LastVisitDate,
                    PointsBalance = c.PointsBalance,
                    Tier = c.PointsBalance >= 500 ? "Gold"
                         : c.PointsBalance >= 200 ? "Silver"
                         : "Regular",
                    IsActive = c.IsActive
                })
                .FirstOrDefaultAsync();

            if (customer is null)
                return NotFound();

            return Ok(customer);
        }

        // GET api/tenant/{companyId}/customers/find?query=...
        // Used by the POS customer search (ID or phone). Returns a single match or 404.
        [HttpGet("find")]
        public async Task<ActionResult<CustomerResponseDto>> FindCustomer(
            int companyId,
            [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Query is required." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var term = query.Trim();

            Customer? match = null;

            // Try ID first
            if (int.TryParse(term, out var id))
            {
                match = await tenant.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CustomerId == id && c.IsActive);
            }

            // Then phone (digits only)
            if (match is null)
            {
                var digits = new string(term.Where(char.IsDigit).ToArray());
                if (digits.Length > 0)
                {
                    match = await tenant.Customers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.ContactNumber.Contains(digits) && c.IsActive);
                }
            }

            if (match is null)
                return NotFound();

            return Ok(new CustomerResponseDto
            {
                CustomerId = match.CustomerId,
                FirstName = match.FirstName,
                LastName = match.LastName,
                FullName = (match.FirstName + " " + match.LastName).Trim(),
                ContactNumber = match.ContactNumber,
                Email = match.Email,
                Birthday = match.Birthday,
                LastVisitDate = match.LastVisitDate,
                PointsBalance = match.PointsBalance,
                Tier = match.PointsBalance >= 500 ? "Gold"
                     : match.PointsBalance >= 200 ? "Silver"
                     : "Regular",
                IsActive = match.IsActive
            });
        }

        // POST api/tenant/{companyId}/customers
        [HttpPost]
        public async Task<ActionResult<CustomerResponseDto>> CreateCustomer(
            int companyId,
            CreateCustomerDto request)
        {
            // ---- Server-side validation ----
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

            // Only Baristas register new customers. Managers act on existing ones and
            // don't originate records. Read the caller from the JWT claim, look up
            // their role in the tenant DB.
            var callerEmployeeIdClaim = User.FindFirst("employeeId")?.Value;
            if (!int.TryParse(callerEmployeeIdClaim, out var callerEmployeeId))
                return StatusCode(403, new { message = "Missing or invalid employee claim." });

            var caller = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == callerEmployeeId);

            if (caller is null)
                return StatusCode(403, new { message = "Caller not found." });

            if (!caller.Role.Equals("Barista", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only baristas can register new customers." });

            var customer = new Customer
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                ContactNumber = cleanedPhone,
                Email = cleanedEmail,
                Birthday = request.Birthday,
                LastVisitDate = DateTime.UtcNow,
                PointsBalance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            tenant.Customers.Add(customer);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new CustomerResponseDto
            {
                CustomerId = customer.CustomerId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                FullName = (customer.FirstName + " " + customer.LastName).Trim(),
                ContactNumber = customer.ContactNumber,
                Email = customer.Email,
                Birthday = customer.Birthday,
                LastVisitDate = customer.LastVisitDate,
                PointsBalance = customer.PointsBalance,
                Tier = "Regular",
                IsActive = customer.IsActive
            });
        }

        // PUT api/tenant/{companyId}/customers/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CustomerResponseDto>> UpdateCustomer(
            int companyId,
            int id,
            UpdateCustomerDto request)
        {
            // ---- Server-side validation ----
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

            var customer = await tenant.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer is null)
                return NotFound();

            customer.FirstName = request.FirstName.Trim();
            customer.LastName = request.LastName.Trim();
            customer.ContactNumber = cleanedPhone;
            customer.Email = cleanedEmail;
            customer.Birthday = request.Birthday;
            customer.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(new CustomerResponseDto
            {
                CustomerId = customer.CustomerId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                FullName = (customer.FirstName + " " + customer.LastName).Trim(),
                ContactNumber = customer.ContactNumber,
                Email = customer.Email,
                Birthday = customer.Birthday,
                LastVisitDate = customer.LastVisitDate,
                PointsBalance = customer.PointsBalance,
                Tier = customer.PointsBalance >= 500 ? "Gold"
                     : customer.PointsBalance >= 200 ? "Silver"
                     : "Regular",
                IsActive = customer.IsActive
            });
        }

        // DELETE api/tenant/{companyId}/customers/{id}
        // Soft delete — sets IsActive = false, does not remove the row. Preserves order history.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int companyId, int id)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Only Managers deactivate customers. Read the caller from the JWT claim.
            var callerEmployeeIdClaim = User.FindFirst("employeeId")?.Value;
            if (!int.TryParse(callerEmployeeIdClaim, out var callerEmployeeId))
                return StatusCode(403, new { message = "Missing or invalid employee claim." });

            var caller = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == callerEmployeeId);

            if (caller is null)
                return StatusCode(403, new { message = "Caller not found." });

            if (!caller.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only managers can deactivate customers." });

            var customer = await tenant.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer is null)
                return NotFound();

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;

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

        // POST api/tenant/{companyId}/customers/{id}/reactivate
        // Reverses a soft delete — sets IsActive = true. All history preserved.
        [HttpPost("{id:int}/reactivate")]
        public async Task<IActionResult> ReactivateCustomer(int companyId, int id)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Only Managers reactivate customers.
            var callerEmployeeIdClaim = User.FindFirst("employeeId")?.Value;
            if (!int.TryParse(callerEmployeeIdClaim, out var callerEmployeeId))
                return StatusCode(403, new { message = "Missing or invalid employee claim." });

            var caller = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == callerEmployeeId);

            if (caller is null)
                return StatusCode(403, new { message = "Caller not found." });

            if (!caller.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only managers can reactivate customers." });

            var customer = await tenant.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer is null)
                return NotFound();

            customer.IsActive = true;
            customer.UpdatedAt = DateTime.UtcNow;

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

        // GET api/tenant/{companyId}/customers/{id}/ledger
        [HttpGet("{id:int}/ledger")]
        public async Task<ActionResult<List<PointLedgerResponseDto>>> GetLedger(int companyId, int id)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var entries = await tenant.PointLedgers
                .AsNoTracking()
                .Where(l => l.CustomerId == id)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new PointLedgerResponseDto
                {
                    PointLedgerId = l.PointLedgerId,
                    CustomerId = l.CustomerId,
                    PointsChange = l.PointsChange,
                    Type = l.Type,
                    Reason = l.Reason,
                    OrderId = l.OrderId,
                    PerformedBy = l.PerformedBy,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(entries);
        }

        // GET api/tenant/{companyId}/customers/{id}/orders
        [HttpGet("{id:int}/orders")]
        public async Task<ActionResult<List<OrderHistoryDto>>> GetOrderHistory(int companyId, int id)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var orders = await tenant.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderHistoryDto
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}