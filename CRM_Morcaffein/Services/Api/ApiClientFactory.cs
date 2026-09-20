using System.Net.Http.Headers;

namespace CRM_Morcaffein.winforms.Services.Api
{
    /// <summary>
    /// Central place for building HttpClients used by the Api* classes. Reads
    /// ApiConfig.BaseUrl for the base address and attaches the current auth token
    /// (if any) as a bearer header.
    ///
    /// Each call creates a fresh HttpClient. That's not ideal (sockets should be
    /// pooled), but it's what the existing Api* classes already do — this just
    /// centralizes the setup so adding the token is one change instead of many.
    /// </summary>
    public static class ApiClientFactory
    {
        public static HttpClient Create()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl),
                Timeout = TimeSpan.FromSeconds(5),
            };

            if (!string.IsNullOrWhiteSpace(ApiConfig.AuthToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", ApiConfig.AuthToken);
            }

            return client;
        }
    }
}