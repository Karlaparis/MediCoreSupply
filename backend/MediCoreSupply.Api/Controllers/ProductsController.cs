using System.ComponentModel.DataAnnotations;
using MediCoreSupply.Api.Data;
using MediCoreSupply.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MediCoreSupply.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly MediCoreSupplyDbContext _context;

    public ProductsController(MediCoreSupplyDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.Name)
            .Select(product => ToResponse(product))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.Id == id)
            .Select(product => ToResponse(product))
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
            return NotFound();

        return product;
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"Category {request.CategoryId} does not exist."
            });
        }

        var product = new Product
        {
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            UnitOfMeasure = request.UnitOfMeasure.Trim(),
            UnitPrice = request.UnitPrice,
            RequiresPrescription = request.RequiresPrescription,
            CategoryId = request.CategoryId
        };

        _context.Products.Add(product);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "A product with this SKU already exists."
            });
        }

        await _context.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);

        var response = ToResponse(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.UnitOfMeasure,
        product.UnitPrice,
        product.RequiresPrescription,
        product.IsActive,
        product.CategoryId,
        product.Category.Name);
}

public class CreateProductRequest
{
    [Required]
    [MaxLength(64)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(32)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public bool RequiresPrescription { get; set; }

    public int CategoryId { get; set; }
}

public record ProductResponse(
    int Id,
    string Sku,
    string Name,
    string? Description,
    string UnitOfMeasure,
    decimal UnitPrice,
    bool RequiresPrescription,
    bool IsActive,
    int CategoryId,
    string CategoryName);
