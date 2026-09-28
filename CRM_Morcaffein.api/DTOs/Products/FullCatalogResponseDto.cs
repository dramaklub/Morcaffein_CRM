namespace CRM_Morcaffein.api.DTOs;

public class FullCatalogResponseDto
{
    public List<ProductResponseDto> Products { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<ProductModifierResponseDto> Modifiers { get; set; } = new();
}