using System.ComponentModel.DataAnnotations;
using MediCoreSupply.Api.Data;
using MediCoreSupply.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCoreSupply.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly MediCoreSupplyDbContext _context;

    public CustomersController(MediCoreSupplyDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .Select(customer => ToResponse(customer))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == id)
            .Select(customer => ToResponse(customer))
            .SingleOrDefaultAsync(cancellationToken);

        if (customer is null)
            return NotFound();

        return customer;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            ContactName = request.ContactName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address.Trim()
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        var response = ToResponse(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Update(int id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
            return NotFound();

        customer.Name = request.Name.Trim();
        customer.Type = request.Type;
        customer.ContactName = request.ContactName;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Address = request.Address.Trim();
        customer.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return ToResponse(customer);
    }

    // Customers with orders are kept for order history; deactivate them instead.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
            return NotFound();

        if (await _context.Orders.AnyAsync(o => o.CustomerId == id, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "This customer has orders and cannot be deleted.",
                Detail = "Set isActive to false to deactivate the customer instead."
            });
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static CustomerResponse ToResponse(Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Type,
        customer.ContactName,
        customer.Email,
        customer.Phone,
        customer.Address,
        customer.IsActive);
}

public class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public CustomerType Type { get; set; }

    public string? ContactName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    [Required]
    [MaxLength(400)]
    public string Address { get; set; } = string.Empty;
}

public class UpdateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public CustomerType Type { get; set; }

    public string? ContactName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    [Required]
    [MaxLength(400)]
    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public record CustomerResponse(
    int Id,
    string Name,
    CustomerType Type,
    string? ContactName,
    string? Email,
    string? Phone,
    string Address,
    bool IsActive);
