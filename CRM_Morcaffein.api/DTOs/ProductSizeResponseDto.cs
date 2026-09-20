namespace CRM_Morcaffein.api.DTOs;

public class ProductSizeResponseDto
{
    public int ProductSizeId { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}