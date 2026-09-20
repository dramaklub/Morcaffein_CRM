namespace CRM_Morcaffein.domain.entities;

public class ProductModifier : BaseEntity
{
    public int ProductModifierId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Extra Shot", "Oat Milk"
    public string Category { get; set; } = string.Empty; // e.g., "Espresso", "Milk", "Syrups"
    public decimal Price { get; set; } // e.g., 30.00
}
