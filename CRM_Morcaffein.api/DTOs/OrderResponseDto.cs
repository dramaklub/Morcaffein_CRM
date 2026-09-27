namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// Response from POST /orders. Includes enough detail to render a full
/// receipt client-side: line items with their modifiers, the discount
/// breakdown, and the customer's post-sale points balance.
///
/// The "amount paid" and "change" values are NOT here — those come from
/// the cashier's input at checkout, so they're stitched onto the receipt
/// on the client.
/// </summary>
public class OrderResponseDto
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    /// <summary>Branch name at the time of sale, for the receipt header.</summary>
    public string? BranchName { get; set; }

    public decimal SubTotal { get; set; }

    /// <summary>Campaign/promo discount only. PWD/Senior is separate below.</summary>
    public decimal TotalDiscount { get; set; }

    /// <summary>Legally-mandated PWD/Senior discount, shown separately on the receipt.</summary>
    public decimal PwdSeniorDiscountAmount { get; set; }

    /// <summary>What the customer actually paid (after discounts).</summary>
    public decimal TotalAmount { get; set; }

    public int PointsEarned { get; set; }

    /// <summary>Customer's points balance AFTER this order. Shown on the receipt.</summary>
    public int NewPointsBalance { get; set; }

    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public List<OrderReceiptLineDto> Lines { get; set; } = new();
}

/// <summary>
/// One line on the receipt: the product, its modifiers, and its line total.
/// Names are snapshotted at sale time so a later product rename doesn't
/// change historical receipts.
/// </summary>
public class OrderReceiptLineDto
{
    public string ProductName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public int Quantity { get; set; }

    /// <summary>Base + modifiers, per unit.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>UnitPrice * Quantity.</summary>
    public decimal SubTotal { get; set; }

    public List<OrderReceiptModifierDto> Modifiers { get; set; } = new();
}

public class OrderReceiptModifierDto
{
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}