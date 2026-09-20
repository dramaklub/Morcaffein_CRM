using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/feedback")]
    public class FeedbackController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public FeedbackController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        // GET api/tenant/{companyId}/feedback?search=...
        // Returns all tickets (newest first), optionally filtered by customer name,
        // category, status, or exact ticket ID.
        // GET api/tenant/{companyId}/feedback?search=...&page=1&pageSize=25
        [HttpGet]
        public async Task<ActionResult<PagedResponse<FeedbackResponseDto>>> GetTickets(
            int companyId,
            [FromQuery] string? search,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 25;
            if (pageSize > 200) pageSize = 200;

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var query = tenant.Feedbacks
            .AsNoTracking()
            .Where(f => f.IsActive);

            // Status filter — exact match, case-insensitive. Null/empty/whitespace means
            // "no status filter" (all statuses returned). The view supplies "Pending"
            // by default; "All Statuses" maps to null.
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusTerm = status.Trim().ToLower();
                query = query.Where(f => f.Status.ToLower() == statusTerm);
            }

            // Search — matches customer name, category, ticket ID. Note: Status is NO
            // LONGER matched here — status filtering is the dropdown's job now, so the
            // search box has a single responsibility.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var asInt = int.TryParse(term, out var parsed) ? parsed : (int?)null;

                query = query.Where(f =>
                    (f.Customer != null && (f.Customer.FirstName + " " + f.Customer.LastName).Contains(term)) ||
                    f.Category.Contains(term) ||
                    (asInt.HasValue && f.TicketId == asInt.Value));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(f => f.DateLogged)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FeedbackResponseDto
                {
                    TicketId = f.TicketId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null
                        ? (f.Customer.FirstName + " " + f.Customer.LastName).Trim()
                        : "—",
                    LoggedBy = f.LoggedBy,
                    LoggedByName = f.LoggedByEmployee != null
                        ? (f.LoggedByEmployee.FirstName + " " + f.LoggedByEmployee.LastName).Trim()
                        : "—",
                    Rating = f.Rating,
                    Category = f.Category,
                    StaffNotes = f.StaffNotes,
                    Status = f.Status,
                    DateLogged = f.DateLogged,
                    DecidedBy = f.DecidedBy,
                    DecidedAt = f.DecidedAt,
                    DecisionNotes = f.DecisionNotes
                })
                .ToListAsync();

            return Ok(new PagedResponse<FeedbackResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // GET api/tenant/{companyId}/feedback/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<FeedbackResponseDto>> GetTicket(int companyId, int id)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var ticket = await tenant.Feedbacks
                .AsNoTracking()
                .Where(f => f.TicketId == id)
                .Select(f => new FeedbackResponseDto
                {
                    TicketId = f.TicketId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null
                        ? (f.Customer.FirstName + " " + f.Customer.LastName).Trim()
                        : "—",
                    LoggedBy = f.LoggedBy,
                    LoggedByName = f.LoggedByEmployee != null
                        ? (f.LoggedByEmployee.FirstName + " " + f.LoggedByEmployee.LastName).Trim()
                        : "—",
                    Rating = f.Rating,
                    Category = f.Category,
                    StaffNotes = f.StaffNotes,
                    Status = f.Status,
                    DateLogged = f.DateLogged,
                    DecidedBy = f.DecidedBy,
                    DecidedAt = f.DecidedAt,
                    DecisionNotes = f.DecisionNotes
                })
                .FirstOrDefaultAsync();

            if (ticket is null)
                return NotFound();

            return Ok(ticket);
        }

        // POST api/tenant/{companyId}/feedback
        [HttpPost]
        public async Task<ActionResult<FeedbackResponseDto>> LogFeedback(
            int companyId,
            LogFeedbackRequest request)
        {
            // ---- Server-side validation ----
            if (request.CustomerId <= 0)
                return BadRequest(new { message = "Customer is required." });
            if (request.LoggedBy <= 0)
                return BadRequest(new { message = "Logged-by employee is required." });
            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5 stars." });
            if (string.IsNullOrWhiteSpace(request.Category))
                return BadRequest(new { message = "Category is required." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Verify the customer and employee exist (FK safety + clear error)
            var customerExists = await tenant.Customers.AnyAsync(c => c.CustomerId == request.CustomerId);
            if (!customerExists)
                return BadRequest(new { message = "Customer not found." });

            var logger = await tenant.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == request.LoggedBy);

            if (logger is null)
                return BadRequest(new { message = "Employee not found." });

            // Only Baristas log feedback. Managers decide on it; Admins have no role here.
            if (!logger.Role.Equals("Barista", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only baristas can log feedback tickets." });

            var feedback = new Feedback
            {
                CustomerId = request.CustomerId,
                LoggedBy = request.LoggedBy,
                Rating = request.Rating,
                Category = request.Category.Trim(),
                StaffNotes = request.StaffNotes?.Trim() ?? string.Empty,
                Status = "Pending",
                DateLogged = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            tenant.Feedbacks.Add(feedback);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // Reload with the joined names for the response
            var result = await tenant.Feedbacks
                .AsNoTracking()
                .Where(f => f.TicketId == feedback.TicketId)
                .Select(f => new FeedbackResponseDto
                {
                    TicketId = f.TicketId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null
                        ? (f.Customer.FirstName + " " + f.Customer.LastName).Trim()
                        : "—",
                    LoggedBy = f.LoggedBy,
                    LoggedByName = f.LoggedByEmployee != null
                        ? (f.LoggedByEmployee.FirstName + " " + f.LoggedByEmployee.LastName).Trim()
                        : "—",
                    Rating = f.Rating,
                    Category = f.Category,
                    StaffNotes = f.StaffNotes,
                    Status = f.Status,
                    DateLogged = f.DateLogged,
                    DecidedBy = f.DecidedBy,
                    DecidedAt = f.DecidedAt,
                    DecisionNotes = f.DecisionNotes
                })
                .FirstAsync();

            return Ok(result);
        }

        // POST api/tenant/{companyId}/feedback/{id}/cancel
        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<FeedbackResponseDto>> CancelFeedback(
            int companyId,
            int id,
            [FromBody] CancelFeedbackRequest request)
        {
            if (request.CancelledBy <= 0)
                return BadRequest(new { message = "Cancelling employee ID is required." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var feedback = await tenant.Feedbacks
                .FirstOrDefaultAsync(f => f.TicketId == id);

            if (feedback is null)
                return NotFound(new { message = "Ticket not found." });

            // ---- Business rules (moved here from the entity) ----
            if (feedback.LoggedBy != request.CancelledBy)
                return StatusCode(403, new { message = "Only the employee who logged this ticket can cancel it." });

            if (!feedback.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = $"Only pending tickets can be cancelled (current status: {feedback.Status})." });

            // ---- Apply the cancellation ----
            feedback.Status = "Cancelled";
            feedback.IsActive = false;
            feedback.DecidedBy = request.CancelledBy;
            feedback.DecidedAt = DateTime.UtcNow;
            feedback.DecisionNotes = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
            feedback.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // Return updated ticket
            var result = await tenant.Feedbacks
                .AsNoTracking()
                .Where(f => f.TicketId == feedback.TicketId)
                .Select(f => new FeedbackResponseDto
                {
                    TicketId = f.TicketId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null
                        ? (f.Customer.FirstName + " " + f.Customer.LastName).Trim()
                        : "—",
                    LoggedBy = f.LoggedBy,
                    LoggedByName = f.LoggedByEmployee != null
                        ? (f.LoggedByEmployee.FirstName + " " + f.LoggedByEmployee.LastName).Trim()
                        : "—",
                    Rating = f.Rating,
                    Category = f.Category,
                    StaffNotes = f.StaffNotes,
                    Status = f.Status,
                    DateLogged = f.DateLogged,
                    DecidedBy = f.DecidedBy,
                    DecidedAt = f.DecidedAt,
                    DecisionNotes = f.DecisionNotes
                })
                .FirstAsync();

            return Ok(result);
        }

        // POST api/tenant/{companyId}/feedback/{id}/resolve
        //
        // Single decision endpoint. BonusPoints == 0 means "resolved, no gesture"
        // (the dismiss case). BonusPoints > 0 also writes a PointLedger row and bumps
        // the customer's PointsBalance. Manager-only. Only Pending tickets can be
        // resolved. Notes are optional and land in DecisionNotes only.
        [HttpPost("{id:int}/resolve")]
        public async Task<ActionResult<FeedbackResponseDto>> ResolveFeedback(
            int companyId,
            int id,
            ResolveFeedbackRequest request)
        {
            if (request.DecidedBy <= 0)
                return BadRequest(new { message = "Deciding employee ID is required." });

            if (request.BonusPoints < 0)
                return BadRequest(new { message = "Bonus points cannot be negative." });

            if (request.BonusPoints > 50)
                return BadRequest(new { message = "Bonus points cannot exceed 50." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var decider = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.DecidedBy);

            if (decider is null)
                return NotFound(new { message = "Deciding employee not found." });

            if (!decider.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only managers can resolve feedback tickets." });

            var feedback = await tenant.Feedbacks
                .FirstOrDefaultAsync(f => f.TicketId == id);

            if (feedback is null)
                return NotFound(new { message = "Ticket not found." });

            if (!feedback.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = $"Only pending tickets can be resolved (current status: {feedback.Status})." });

            // Apply the decision
            feedback.Status = "Resolved";
            feedback.DecidedBy = request.DecidedBy;
            feedback.DecidedAt = DateTime.UtcNow;
            feedback.DecisionNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            feedback.UpdatedAt = DateTime.UtcNow;

            // Bonus points, if any
            if (request.BonusPoints > 0)
            {
                var customer = await tenant.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == feedback.CustomerId);

                if (customer is null)
                    return NotFound(new { message = "Customer not found." });

                customer.PointsBalance += request.BonusPoints;
                customer.UpdatedAt = DateTime.UtcNow;

                // NOTE: deliberately NOT touching customer.LastVisitDate. A feedback
                // bonus is bookkeeping, not a visit — writing it there would corrupt
                // the 14-day activity threshold used by the Status line and the
                // "We Miss You" promo eligibility rule.

                tenant.PointLedgers.Add(new PointLedger
                {
                    CustomerId = customer.CustomerId,
                    PointsChange = request.BonusPoints,
                    Type = "FeedbackBonus",
                    Reason = $"Feedback ticket #{feedback.TicketId} — {feedback.Category}",
                    PerformedBy = request.DecidedBy,
                    Timestamp = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // Return updated ticket
            var result = await tenant.Feedbacks
                .AsNoTracking()
                .Where(f => f.TicketId == feedback.TicketId)
                .Select(f => new FeedbackResponseDto
                {
                    TicketId = f.TicketId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null
                        ? (f.Customer.FirstName + " " + f.Customer.LastName).Trim()
                        : "—",
                    LoggedBy = f.LoggedBy,
                    LoggedByName = f.LoggedByEmployee != null
                        ? (f.LoggedByEmployee.FirstName + " " + f.LoggedByEmployee.LastName).Trim()
                        : "—",
                    Rating = f.Rating,
                    Category = f.Category,
                    StaffNotes = f.StaffNotes,
                    Status = f.Status,
                    DateLogged = f.DateLogged,
                    DecidedBy = f.DecidedBy,
                    DecidedAt = f.DecidedAt,
                    DecisionNotes = f.DecisionNotes
                })
                .FirstAsync();

            return Ok(result);
        }
    }
}
    