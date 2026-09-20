using System.Collections.Generic;

namespace CRM_Morcaffein.domain.entities;

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int ProductSizeId { get; set; }
    public string SizeNameSnapshot { get; set; } = string.Empty;
    public decimal BaseSizePrice { get; set; }
    public int Quantity { get; set; }

    // Stored, not computed — TransactionController recalculates and assigns these whenever
    // the line's quantity or modifiers change (UnitPrice = BaseSizePrice + sum of modifier
    // prices; SubTotal = Quantity * UnitPrice).
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    public virtual Order? Order { get; set; }
    public virtual Product? Product { get; set; }
    public virtual ProductSize? ProductSize { get; set; }
    public virtual ICollection<OrderItemModifier> OrderItemModifiers { get; set; } = new List<OrderItemModifier>();
}
