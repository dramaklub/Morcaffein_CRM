using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// Session-scoped cache for active campaigns. Fetched once from the API on
    /// first use, then reused for the rest of the app session. Campaigns rarely
    /// change; refetching on every customer search is wasteful and slow.
    ///
    /// Call Reset() to force a refetch (e.g. after a manager creates a new campaign).
    /// </summary>
    public static class CampaignCache
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static List<CampaignResponseDto>? _campaigns;

        public static async Task<List<CampaignResponseDto>> GetActiveCampaignsAsync()
        {
            if (_campaigns is not null) return _campaigns;

            await _lock.WaitAsync();
            try
            {
                if (_campaigns is null)
                {
                    var api = new CampaignApi();
                    _campaigns = await api.GetActiveCampaignsAsync();
                }
                return _campaigns;
            }
            finally { _lock.Release(); }
        }

        public static void Reset() => _campaigns = null;
    }
}