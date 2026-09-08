using System.ComponentModel.DataAnnotations;
using MediCoreSupply.Api.Data;
using MediCoreSupply.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCoreSupply.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly MediCoreSupplyDbContext _context;

    public WarehousesController(MediCoreSupplyDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<WarehouseResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return await _context.Warehouses
            .AsNoTracking()
            .OrderBy(warehouse => warehouse.Name)
            .Select(warehouse => new WarehouseResponse(warehouse.Id, warehouse.Name, warehouse.Address))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WarehouseResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.Id == id)
            .Select(warehouse => new WarehouseResponse(warehouse.Id, warehouse.Name, warehouse.Address))
            .SingleOrDefaultAsync(cancellationToken);

        if (warehouse is null)
            return NotFound();

        return warehouse;
    }

    [HttpPost]
    public async Task<ActionResult<WarehouseResponse>> Create(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var warehouse = new Warehouse
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim()
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new WarehouseResponse(warehouse.Id, warehouse.Name, warehouse.Address);
        return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, response);
    }
}

public class CreateWarehouseRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(400)]
    public string Address { get; set; } = string.Empty;
}

public record WarehouseResponse(int Id, string Name, string Address);
