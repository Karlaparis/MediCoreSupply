using System.ComponentModel.DataAnnotations;
using MediCoreSupply.Api.Data;
using MediCoreSupply.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MediCoreSupply.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly MediCoreSupplyDbContext _context;

    public CategoriesController(MediCoreSupplyDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .SingleOrDefaultAsync(cancellationToken);

        if (category is null)
            return NotFound();

        return category;
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description
        };

        _context.Categories.Add(category);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "A category with this name already exists."
            });
        }

        var response = new CategoryResponse(category.Id, category.Name, category.Description);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Update(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
            return NotFound();

        category.Name = request.Name.Trim();
        category.Description = request.Description;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "A category with this name already exists."
            });
        }

        return new CategoryResponse(category.Id, category.Name, category.Description);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
            return NotFound();

        if (await _context.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "This category still has products and cannot be deleted.",
                Detail = "Move or delete its products first."
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class UpdateCategoryRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public record CategoryResponse(int Id, string Name, string? Description);
