namespace CRM_Morcaffein.domain.entities;

public class ProductSize : BaseEntity
{
    public int ProductSizeId { get; set; }
    public int ProductId { get; set; }
    public string SizeName { get; set; } = string.Empty; // e.g., "Regular (16oz)", "Large (22oz)"
    public decimal Price { get; set; }

    public virtual Product? Product { get; set; }
}
