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

public record CustomerResponse(
    int Id,
    string Name,
    CustomerType Type,
    string? ContactName,
    string? Email,
    string? Phone,
    string Address,
    bool IsActive);
