namespace CRM_Morcaffein.api.DTOs;

public class ProductModifierResponseDto
{
    public int ProductModifierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
}