using System.ComponentModel.DataAnnotations;
using MediCoreSupply.Api.Data;
using MediCoreSupply.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCoreSupply.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly MediCoreSupplyDbContext _context;

    public OrdersController(MediCoreSupplyDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.OrderDate)
            .Select(order => new OrderSummaryResponse(
                order.Id,
                order.OrderNumber,
                order.OrderDate,
                order.Status,
                order.CustomerId,
                order.Customer.Name,
                order.OrderItems.Sum(item => item.Quantity * item.UnitPrice)))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
            .ThenInclude(item => item.Product)
            .Where(order => order.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        if (order is null)
            return NotFound();

        return ToResponse(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "An order must contain at least one item."
            });
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Every order item must have a quantity greater than zero."
            });
        }

        var customer = await _context.Customers.FindAsync(new object?[] { request.CustomerId }, cancellationToken);
        if (customer is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"Customer {request.CustomerId} does not exist."
            });
        }

        var requestedProductIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(product => requestedProductIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var missingProductIds = requestedProductIds.Except(products.Keys).ToList();
        if (missingProductIds.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"Product(s) {string.Join(", ", missingProductIds)} do not exist."
            });
        }

        var order = new Order
        {
            // Placeholder to satisfy the NOT NULL + unique constraint until the real,
            // Id-derived order number can be assigned after the first save.
            OrderNumber = Guid.NewGuid().ToString("N"),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress.Trim(),
            Notes = request.Notes,
            CustomerId = request.CustomerId
        };

        foreach (var requestedItem in request.Items)
        {
            var product = products[requestedItem.ProductId];
            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = requestedItem.Quantity,
                UnitPrice = product.UnitPrice
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        order.OrderNumber = $"ORD-{1000 + order.Id}";
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var item in order.OrderItems)
        {
            item.Product = products[item.ProductId];
        }
        order.Customer = customer;

        var response = ToResponse(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.OrderDate,
        order.Status,
        order.ShippingAddress,
        order.Notes,
        order.CustomerId,
        order.Customer.Name,
        order.OrderItems.Select(item => new OrderItemResponse(
            item.Id,
            item.ProductId,
            item.Product.Name,
            item.Quantity,
            item.UnitPrice,
            item.Quantity * item.UnitPrice)).ToList());
}

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(400)]
    public string ShippingAddress { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public record OrderItemResponse(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderSummaryResponse(
    int Id,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    int CustomerId,
    string CustomerName,
    decimal Total);

public record OrderResponse(
    int Id,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    string ShippingAddress,
    string? Notes,
    int CustomerId,
    string CustomerName,
    List<OrderItemResponse> Items);
