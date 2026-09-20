using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/products")]
    public class ProductController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public ProductController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        // GET api/tenant/{companyId}/products
        // Returns all active, available products with their sizes nested.
        [HttpGet]
        public async Task<ActionResult<List<ProductResponseDto>>> GetProducts(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var products = await tenant.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsAvailable)
                .OrderBy(p => p.ProductId)
                .Select(p => new ProductResponseDto
                {
                    ProductId = p.ProductId,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    ProductCategory = p.ProductCategory,
                    BasePrice = p.BasePrice,
                    ImageUrl = p.ImageUrl,
                    LocalImagePath = p.LocalImagePath,
                    IsAvailable = p.IsAvailable,
                    Sizes = p.ProductSizes
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Price)
                        .Select(s => new ProductSizeResponseDto
                        {
                            ProductSizeId = s.ProductSizeId,
                            SizeName = s.SizeName,
                            Price = s.Price
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET api/tenant/{companyId}/products/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(int companyId, int id)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var product = await tenant.Products
                .AsNoTracking()
                .Where(p => p.ProductId == id)
                .Select(p => new ProductResponseDto
                {
                    ProductId = p.ProductId,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    ProductCategory = p.ProductCategory,
                    BasePrice = p.BasePrice,
                    ImageUrl = p.ImageUrl,
                    LocalImagePath = p.LocalImagePath,
                    IsAvailable = p.IsAvailable,
                    Sizes = p.ProductSizes
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Price)
                        .Select(s => new ProductSizeResponseDto
                        {
                            ProductSizeId = s.ProductSizeId,
                            SizeName = s.SizeName,
                            Price = s.Price
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (product is null)
                return NotFound();

            return Ok(product);
        }

        // GET api/tenant/{companyId}/products/categories
        // Returns distinct categories as a plain list of strings.
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var categories = await tenant.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsAvailable)
                .Select(p => p.ProductCategory)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        // GET api/tenant/{companyId}/products/modifiers
        // Returns all active product modifiers (the add-ons checklist).
        [HttpGet("modifiers")]
        public async Task<ActionResult<List<ProductModifierResponseDto>>> GetModifiers(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var modifiers = await tenant.ProductModifiers
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.ProductModifierId)
                .Select(m => new ProductModifierResponseDto
                {
                    ProductModifierId = m.ProductModifierId,
                    Name = m.Name,
                    Category = m.Category,
                    Price = m.Price
                })
                .ToListAsync();

            return Ok(modifiers);
        }

        // GET api/tenant/{companyId}/products/full-catalog
        // Returns products (with sizes), categories, and modifiers in ONE response, so the
        // client needs a single HTTP call instead of three. Each API call here involves a
        // tenant DB round-trip, so combining them is a meaningful speed-up.
        [HttpGet("full-catalog")]
        public async Task<ActionResult<FullCatalogResponseDto>> GetFullCatalog(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var products = await tenant.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsAvailable)
                .OrderBy(p => p.ProductId)
                .Select(p => new ProductResponseDto
                {
                    ProductId = p.ProductId,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    ProductCategory = p.ProductCategory,
                    BasePrice = p.BasePrice,
                    ImageUrl = p.ImageUrl,
                    LocalImagePath = p.LocalImagePath,
                    IsAvailable = p.IsAvailable,
                    Sizes = p.ProductSizes
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Price)
                        .Select(s => new ProductSizeResponseDto
                        {
                            ProductSizeId = s.ProductSizeId,
                            SizeName = s.SizeName,
                            Price = s.Price
                        })
                        .ToList()
                })
                .ToListAsync();

            var categories = products
                .Select(p => p.ProductCategory)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var modifiers = await tenant.ProductModifiers
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.ProductModifierId)
                .Select(m => new ProductModifierResponseDto
                {
                    ProductModifierId = m.ProductModifierId,
                    Name = m.Name,
                    Category = m.Category,
                    Price = m.Price
                })
                .ToListAsync();

            return Ok(new FullCatalogResponseDto
            {
                Products = products,
                Categories = categories,
                Modifiers = modifiers
            });
        }
    }
}