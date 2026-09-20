namespace CRM_Morcaffein.api.DTOs;

public class ProductResponseDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? LocalImagePath { get; set; }
    public bool IsAvailable { get; set; }
    public List<ProductSizeResponseDto> Sizes { get; set; } = new();
}