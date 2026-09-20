using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// Session-scoped cache of the product catalog. Fetched once from the API on first
    /// use, then held in memory for the rest of the app session.
    ///
    /// Uses a single "full-catalog" endpoint instead of three separate calls, so the
    /// client only pays one HTTP round-trip (and one tenant DB lookup) on load.
    /// </summary>
    public static class ProductCatalogCache
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

        private static FullCatalogResponseDto? _catalog;

        public static async Task<FullCatalogResponseDto> GetCatalogAsync()
        {
            if (_catalog is not null) return _catalog;

            await _lock.WaitAsync();
            try
            {
                if (_catalog is null)
                {
                    var api = new ProductApi();
                    _catalog = await api.GetFullCatalogAsync();
                }
                return _catalog;
            }
            finally { _lock.Release(); }
        }

        public static void Reset() => _catalog = null;
    }
}