using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class ProductApi
    {
        private readonly HttpClient _http;

        public ProductApi()
        {
            _http = ApiClientFactory.Create();
        }

        // GET api/tenant/{companyId}/products
        public async Task<List<ProductResponseDto>> GetProductsAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/products");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
            return result ?? new List<ProductResponseDto>();
        }

        // GET api/tenant/{companyId}/products/full-catalog
        public async Task<FullCatalogResponseDto> GetFullCatalogAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/products/full-catalog");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<FullCatalogResponseDto>();
            return result ?? new FullCatalogResponseDto();
        }

        // GET api/tenant/{companyId}/products/{id}
        public async Task<ProductResponseDto?> GetProductAsync(int productId)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/products/{productId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        }

        // GET api/tenant/{companyId}/products/categories
        public async Task<List<string>> GetCategoriesAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/products/categories");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<string>>();
            return result ?? new List<string>();
        }

        // GET api/tenant/{companyId}/products/modifiers
        public async Task<List<ProductModifierResponseDto>> GetModifiersAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/products/modifiers");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<ProductModifierResponseDto>>();
            return result ?? new List<ProductModifierResponseDto>();
        }

        private static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body)
                    ? $"API returned {(int)response.StatusCode}."
                    : body);
        }
    }
}