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
    private static readonly HashSet<OrderStatus> TerminalStatuses = new() { OrderStatus.Delivered, OrderStatus.Cancelled };

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
            .Select(order => ToSummary(order))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

        if (order is null)
            return NotFound();

        return ToDetail(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetailResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"Customer {request.CustomerId} does not exist."
            });
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var missingProductIds = productIds.Except(products.Keys).ToList();
        if (missingProductIds.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"Product(s) not found: {string.Join(", ", missingProductIds)}"
            });
        }

        var inactiveProducts = products.Values.Where(p => !p.IsActive).Select(p => p.Sku).ToList();
        if (inactiveProducts.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"Product(s) are no longer active: {string.Join(", ", inactiveProducts)}"
            });
        }

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(cancellationToken),
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = string.IsNullOrWhiteSpace(request.ShippingAddress) ? customer.Address : request.ShippingAddress.Trim(),
            Notes = request.Notes,
            CustomerId = customer.Id
        };

        foreach (var line in request.Items)
        {
            var product = products[line.ProductId];
            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = product.UnitPrice
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(order).Reference(o => o.Customer).LoadAsync(cancellationToken);
        foreach (var item in order.OrderItems)
        {
            item.Product = products[item.ProductId];
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToDetail(order));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderSummaryResponse>> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
            return NotFound();

        if (TerminalStatuses.Contains(order.Status))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = $"Order {order.OrderNumber} is already {order.Status} and cannot change status."
            });
        }

        order.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);

        return ToSummary(order);
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var lastOrderNumber = await _context.Orders
            .OrderByDescending(o => o.Id)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1001;
        if (lastOrderNumber is not null
            && lastOrderNumber.StartsWith("ORD-", StringComparison.Ordinal)
            && int.TryParse(lastOrderNumber.AsSpan(4), out var parsed))
        {
            nextSequence = parsed + 1;
        }

        return $"ORD-{nextSequence}";
    }

    private static OrderSummaryResponse ToSummary(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.OrderDate,
        order.Status,
        order.CustomerId,
        order.Customer.Name,
        order.OrderItems.Sum(item => item.Quantity * item.UnitPrice));

    private static OrderDetailResponse ToDetail(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.OrderDate,
        order.Status,
        order.CustomerId,
        order.Customer.Name,
        order.ShippingAddress,
        order.Notes,
        order.OrderItems
            .Select(item => new OrderItemResponse(
                item.ProductId,
                item.Product.Name,
                item.Product.Sku,
                item.Quantity,
                item.UnitPrice,
                item.Quantity * item.UnitPrice))
            .ToList(),
        order.OrderItems.Sum(item => item.Quantity * item.UnitPrice));
}

public class CreateOrderRequest
{
    public int CustomerId { get; set; }

    public string? ShippingAddress { get; set; }

    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}

public record OrderSummaryResponse(
    int Id,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    int CustomerId,
    string CustomerName,
    decimal Total);

public record OrderItemResponse(
    int ProductId,
    string ProductName,
    string ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderDetailResponse(
    int Id,
    string OrderNumber,
    DateTime OrderDate,
    OrderStatus Status,
    int CustomerId,
    string CustomerName,
    string ShippingAddress,
    string? Notes,
    List<OrderItemResponse> Items,
    decimal Total);
