namespace MediCoreSupply.Api.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    // Snapshot of the product's price at order time, so later price changes
    // don't rewrite the historical value of past orders.
    public decimal UnitPrice { get; set; }
}
