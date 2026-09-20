using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Point-adjustment endpoints. Business rules (role checks, self-approval
    /// prevention, balance update, ledger write) live here on the server — the
    /// client cannot bypass them.
    /// </summary>
    [ApiController]
    [Route("api/tenant/{companyId:int}/point-adjustments")]
    public class PointAdjustmentController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public PointAdjustmentController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        // GET api/tenant/{companyId}/point-adjustments/pending?customerId=...
        // If customerId is provided, returns only that customer's pending requests.
        // Otherwise returns every pending request.
        [HttpGet("pending")]
        public async Task<ActionResult<List<PointAdjustmentResponseDto>>> GetPending(
            int companyId,
            [FromQuery] int? customerId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var query = tenant.PointAdjustmentRequests
                .AsNoTracking()
                .Where(r => r.Status == "Pending");

            if (customerId.HasValue)
                query = query.Where(r => r.CustomerId == customerId.Value);

            var results = await query
                .OrderBy(r => r.RequestedAt)
                .Select(r => new PointAdjustmentResponseDto
                {
                    PointAdjustmentRequestId = r.PointAdjustmentRequestId,
                    CustomerId = r.CustomerId,
                    PointsChange = r.PointsChange,
                    Reason = r.Reason,
                    RequestedBy = r.RequestedBy,
                    RequestedAt = r.RequestedAt,
                    Status = r.Status,
                    ApprovedBy = r.ApprovedBy,
                    DecidedAt = r.DecidedAt,
                    DecisionNotes = r.DecisionNotes
                })
                .ToListAsync();

            return Ok(results);
        }

        // POST api/tenant/{companyId}/point-adjustments
        // Creates a Pending request. Role check: requester must be Manager+.
        [HttpPost]
        public async Task<ActionResult<PointAdjustmentResponseDto>> RequestAdjustment(
            int companyId,
            CreatePointAdjustmentDto request)
        {
            if (request.PointsChange == 0)
                return BadRequest(new { message = "Points change cannot be zero." });
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { message = "A reason is required." });
            if (request.RequestedBy <= 0)
                return BadRequest(new { message = "Requesting employee ID is required." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Role check on the server
            var requester = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.RequestedBy);

            if (requester is null)
                return NotFound(new { message = "Requesting employee not found." });

            // Only Baristas submit point-adjustment requests. Managers decide them;
            // Admins have no role in this chain at all.
            if (!requester.IsActive)
                return StatusCode(403, new { message = "Inactive employee cannot request adjustments." });

            if (!requester.Role.Equals("Barista", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "Only baristas can submit point adjustment requests." });

            var customer = await tenant.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);

            if (customer is null)
                return NotFound(new { message = "Customer not found." });

            var entity = new PointAdjustmentRequest
            {
                CustomerId = request.CustomerId,
                PointsChange = request.PointsChange,
                Reason = request.Reason.Trim(),
                RequestedBy = request.RequestedBy,
                RequestedAt = DateTime.UtcNow,
                Status = "Pending",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            tenant.PointAdjustmentRequests.Add(entity);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(MapToDto(entity));
        }

        // POST api/tenant/{companyId}/point-adjustments/{id}/approve
        [HttpPost("{id:int}/approve")]
        public async Task<ActionResult<PointAdjustmentResponseDto>> Approve(
            int companyId,
            int id,
            DecidePointAdjustmentDto request)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var approver = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.DecidedBy);

            if (approver is null)
                return NotFound(new { message = "Approving employee not found." });
            if (!IsManagerOnly(approver.Role))
                return StatusCode(403, new { message = "Only managers can approve point adjustment requests." });

            var entity = await tenant.PointAdjustmentRequests
                .FirstOrDefaultAsync(r => r.PointAdjustmentRequestId == id);

            if (entity is null)
                return NotFound(new { message = "Request not found." });
            if (!entity.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = $"This request has already been {entity.Status}." });
            if (entity.RequestedBy == request.DecidedBy)
                return StatusCode(403, new { message = "An employee cannot approve their own adjustment request." });

            var customer = await tenant.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == entity.CustomerId);

            if (customer is null)
                return NotFound(new { message = "Customer not found." });

            var newBalance = customer.PointsBalance + entity.PointsChange;
            if (newBalance < 0)
                return BadRequest(new { message = "Adjustment would result in a negative points balance." });

            // Apply the change. NOTE: deliberately NOT touching customer.LastVisitDate —
            // that field is "when the customer last made a transaction," and a points
            // adjustment (an admin bookkeeping action) is not a visit. Writing it here
            // would corrupt the 14-day activity threshold used by the Status line and by
            // the "We Miss You" promo eligibility rule.
            customer.PointsBalance = newBalance;
            customer.UpdatedAt = DateTime.UtcNow;

            // Write the ledger row
            tenant.PointLedgers.Add(new PointLedger
            {
                CustomerId = customer.CustomerId,
                PointsChange = entity.PointsChange,
                Type = "Adjustment",
                Reason = entity.Reason,
                PerformedBy = request.DecidedBy,
                Timestamp = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

            // Mark the request decided
            entity.Status = "Approved";
            entity.ApprovedBy = request.DecidedBy;
            entity.DecidedAt = DateTime.UtcNow;
            entity.DecisionNotes = request.Notes?.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(MapToDto(entity));
        }

        // POST api/tenant/{companyId}/point-adjustments/{id}/reject
        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<PointAdjustmentResponseDto>> Reject(
            int companyId,
            int id,
            DecidePointAdjustmentDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Notes))
                return BadRequest(new { message = "A rejection reason is required." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var approver = await tenant.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.DecidedBy);

            if (approver is null)
                return NotFound(new { message = "Approving employee not found." });
            if (!IsManagerOnly(approver.Role))
                return StatusCode(403, new { message = "Only managers can reject point adjustment requests." });

            var entity = await tenant.PointAdjustmentRequests
                .FirstOrDefaultAsync(r => r.PointAdjustmentRequestId == id);

            if (entity is null)
                return NotFound(new { message = "Request not found." });
            if (!entity.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = $"This request has already been {entity.Status}." });

            entity.Status = "Rejected";
            entity.ApprovedBy = request.DecidedBy;
            entity.DecidedAt = DateTime.UtcNow;
            entity.DecisionNotes = request.Notes.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            return Ok(MapToDto(entity));
        }

        // ---- Helpers ----

        // "Manager only" means exactly the Manager role. Admin and SuperAdmin are NOT
        // approvers in the point-adjustment chain — Barista submits, Manager decides.
        private static bool IsManagerOnly(string role) =>
            role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        private static PointAdjustmentResponseDto MapToDto(PointAdjustmentRequest r) => new()
        {
            PointAdjustmentRequestId = r.PointAdjustmentRequestId,
            CustomerId = r.CustomerId,
            PointsChange = r.PointsChange,
            Reason = r.Reason,
            RequestedBy = r.RequestedBy,
            RequestedAt = r.RequestedAt,
            Status = r.Status,
            ApprovedBy = r.ApprovedBy,
            DecidedAt = r.DecidedAt,
            DecisionNotes = r.DecisionNotes,
        };
    }
}