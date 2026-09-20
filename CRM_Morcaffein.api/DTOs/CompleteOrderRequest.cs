namespace CRM_Morcaffein.api.DTOs
{
    public class CompleteOrderRequest
    {
        public int CustomerId { get; set; }
        public int CreatedBy { get; set; }
        public bool HasPwdSeniorDiscount { get; set; }
        public int? CampaignId { get; set; }
        public List<OrderLineDto> Lines { get; set; } = new();
    }

    public class OrderLineDto
    {
        public int ProductId { get; set; }
        public int ProductSizeId { get; set; }
        public int Quantity { get; set; }
        public List<int> ModifierIds { get; set; } = new();
    }
}