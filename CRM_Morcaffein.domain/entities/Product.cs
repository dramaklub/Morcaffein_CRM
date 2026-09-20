using System.Collections.Generic;

namespace CRM_Morcaffein.domain.entities;

public class Product : BaseEntity
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }

    public string? ImageUrl { get; set; }
    public string? LocalImagePath { get; set; }

    public bool IsAvailable { get; set; } = true;

    public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
