using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Sync status and manual trigger. The background worker does the real
    /// work every N seconds; these endpoints let the client (a) poll the
    /// current state for the topbar indicator, and (b) force an immediate
    /// pass when the user clicks "Sync now".
    ///
    /// Status is read from the in-memory SyncStatusStore — no DB query, no
    /// cloud round-trip. The store is updated by the worker after each tick.
    /// If the API just booted and no tick has run yet, the store returns
    /// default values (LastSyncedAt = null, PendingCount = 0).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/tenant/{companyId:int}/sync")]
    public class SyncController : ControllerBase
    {
        private readonly SyncStatusStore _statusStore;
        private readonly SyncService _syncService;

        public SyncController(
            SyncStatusStore statusStore,
            SyncService syncService)
        {
            _statusStore = statusStore;
            _syncService = syncService;
        }

        // GET api/tenant/{companyId}/sync/status
        //
        // Returns the last known sync state for this company, plus the master
        // status. Shape is what the topbar indicator polls every 5 seconds.
        [HttpGet("status")]
        public ActionResult<SyncStatusResponse> GetStatus(int companyId)
        {
            var company = _statusStore.ForCompany(companyId);
            var master = _statusStore.Master;

            return Ok(new SyncStatusResponse
            {
                CompanyId = companyId,
                IsOnline = company.IsOnline && master.IsOnline,
                PendingCount = company.PendingCount,
                LastSyncedAt = company.LastSyncedAt,
                LastError = company.LastError ?? master.LastError,
                CloudHost = company.CloudHost,
            });
        }

        // POST api/tenant/{companyId}/sync/run
        //
        // Triggers an immediate sync pass for this company (tenant only —
        // master sync happens on the worker's next tick). Returns 200 with
        // no body regardless of whether the push succeeded; the caller can
        // poll /status to see the result.
        [HttpPost("run")]
        public async Task<IActionResult> RunNow(int companyId, CancellationToken ct)
        {
            // Don't await the actual sync? No — await it. The user clicked
            // "Sync now" and expects the call to have done something. If the
            // cloud is slow, this call is slow. The client shows a spinner
            // during the call, then refreshes the indicator from /status.
            _ = await _syncService.RunTenantAsync(companyId, ct);
            return Ok();
        }
    }
}