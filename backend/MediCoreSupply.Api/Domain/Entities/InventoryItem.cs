namespace MediCoreSupply.Api.Domain.Entities;

public class InventoryItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
}
