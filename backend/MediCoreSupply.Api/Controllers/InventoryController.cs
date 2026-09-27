using System.ComponentModel.DataAnnotations;
using MediCoreSupply.Api.Data;
using MediCoreSupply.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MediCoreSupply.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly MediCoreSupplyDbContext _context;

    public InventoryController(MediCoreSupplyDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryItemResponse>>> GetAll(
        [FromQuery] int? warehouseId,
        [FromQuery] int? productId,
        CancellationToken cancellationToken)
    {
        var query = _context.InventoryItems
            .AsNoTracking()
            .Include(item => item.Product)
            .Include(item => item.Warehouse)
            .AsQueryable();

        if (warehouseId is not null)
            query = query.Where(item => item.WarehouseId == warehouseId);

        if (productId is not null)
            query = query.Where(item => item.ProductId == productId);

        return await query
            .OrderBy(item => item.Warehouse.Name)
            .ThenBy(item => item.Product.Name)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryItemResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .AsNoTracking()
            .Include(item => item.Product)
            .Include(item => item.Warehouse)
            .Where(item => item.Id == id)
            .Select(item => ToResponse(item))
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
            return NotFound();

        return item;
    }

    [HttpPost]
    public async Task<ActionResult<InventoryItemResponse>> Create(CreateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"Product {request.ProductId} does not exist."
            });
        }

        var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"Warehouse {request.WarehouseId} does not exist."
            });
        }

        var item = new InventoryItem
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            QuantityOnHand = request.QuantityOnHand,
            ReorderLevel = request.ReorderLevel,
            ReorderQuantity = request.ReorderQuantity
        };

        _context.InventoryItems.Add(item);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "An inventory record for this product at this warehouse already exists."
            });
        }

        await _context.Entry(item).Reference(i => i.Product).LoadAsync(cancellationToken);
        await _context.Entry(item).Reference(i => i.Warehouse).LoadAsync(cancellationToken);

        var response = ToResponse(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, response);
    }

    // Only the stock levels can change; a different product or warehouse is a different inventory record.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<InventoryItemResponse>> Update(int id, UpdateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item is null)
            return NotFound();

        item.QuantityOnHand = request.QuantityOnHand;
        item.ReorderLevel = request.ReorderLevel;
        item.ReorderQuantity = request.ReorderQuantity;

        await _context.SaveChangesAsync(cancellationToken);

        return ToResponse(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems.SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item is null)
            return NotFound();

        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static InventoryItemResponse ToResponse(InventoryItem item) => new(
        item.Id,
        item.ProductId,
        item.Product.Name,
        item.WarehouseId,
        item.Warehouse.Name,
        item.QuantityOnHand,
        item.ReorderLevel,
        item.ReorderQuantity);
}

public class CreateInventoryItemRequest
{
    public int ProductId { get; set; }

    public int WarehouseId { get; set; }

    [Range(0, int.MaxValue)]
    public int QuantityOnHand { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderQuantity { get; set; }
}

public class UpdateInventoryItemRequest
{
    [Range(0, int.MaxValue)]
    public int QuantityOnHand { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderQuantity { get; set; }
}

public record InventoryItemResponse(
    int Id,
    int ProductId,
    string ProductName,
    int WarehouseId,
    string WarehouseName,
    int QuantityOnHand,
    int ReorderLevel,
    int ReorderQuantity);
