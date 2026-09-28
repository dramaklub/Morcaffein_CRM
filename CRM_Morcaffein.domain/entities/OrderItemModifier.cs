namespace CRM_Morcaffein.domain.entities;

public class OrderItemModifier
{
    public int OrderItemModifierId { get; set; }
    public int OrderItemId { get; set; }
    public int ProductModifierId { get; set; }
    public string ProductModifierNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsSynced { get; set; } = false;

    public virtual OrderItem? OrderItem { get; set; }
    public virtual ProductModifier? ProductModifier { get; set; }
}
