namespace MediCoreSupply.Api.Domain.Entities;

public class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
