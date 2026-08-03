using System.Security.Claims;
using KitchenDeck.API.DTOs;
using KitchenDeck.API.Models;
using KitchenDeck.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenDeck.API.Controllers;

[ApiController]
[Authorize]
[Route("api/restaurants/{restaurantId}/orders")]
public class OrdersController : ControllerBase
{
    private static readonly StaffRole[] WaitStaff = { StaffRole.Waiter };
    private static readonly StaffRole[] KitchenStaff = { StaffRole.Waiter, StaffRole.Cook };

    private readonly OrderService _orders;
    private readonly RestaurantService _restaurants;

    public OrdersController(OrderService orders, RestaurantService restaurants)
    {
        _orders = orders;
        _restaurants = restaurants;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static IEnumerable<(string, int, string?)> ToLines(IEnumerable<OrderLineInput>? items) =>
        (items ?? Enumerable.Empty<OrderLineInput>())
            .Select(i => (i.MenuItemId, i.Quantity, i.Notes));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> List(string restaurantId, [FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        if (!await _restaurants.IsMemberAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        return Ok(await _orders.ListForRestaurantAsync(restaurantId, activeOnly, ct));
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<Order>> Get(string restaurantId, string orderId, CancellationToken ct)
    {
        if (!await _restaurants.IsMemberAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        var order = await _orders.GetAsync(restaurantId, orderId, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create(string restaurantId, CreateOrderRequest request, CancellationToken ct)
    {
        if (!await _restaurants.HasAnyRoleAsync(restaurantId, CurrentUserId, WaitStaff, ct))
        {
            return Forbid();
        }

        var order = await _orders.CreateAsync(restaurantId, request.TableId, CurrentUserId, ToLines(request.Items), ct);
        return order is null
            ? NotFound(new { message = "Table not found." })
            : Ok(order);
    }

    [HttpPost("{orderId}/items")]
    public async Task<ActionResult<Order>> AddLines(string restaurantId, string orderId, AddLinesRequest request, CancellationToken ct)
    {
        if (!await _restaurants.HasAnyRoleAsync(restaurantId, CurrentUserId, WaitStaff, ct))
        {
            return Forbid();
        }

        var order = await _orders.AddLinesAsync(restaurantId, orderId, ToLines(request.Items), ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("{orderId}/items/{lineId}")]
    public async Task<ActionResult<Order>> UpdateLine(string restaurantId, string orderId, string lineId, UpdateLineRequest request, CancellationToken ct)
    {
        if (!await _restaurants.HasAnyRoleAsync(restaurantId, CurrentUserId, WaitStaff, ct))
        {
            return Forbid();
        }

        var (order, lineFound) = await _orders.UpdateLineAsync(restaurantId, orderId, lineId, request.Quantity, request.Notes, ct);
        if (order is null) return NotFound(new { message = "Order not found." });
        if (!lineFound) return NotFound(new { message = "Order line not found." });
        return Ok(order);
    }

    [HttpDelete("{orderId}/items/{lineId}")]
    public async Task<ActionResult<Order>> RemoveLine(string restaurantId, string orderId, string lineId, CancellationToken ct)
    {
        if (!await _restaurants.HasAnyRoleAsync(restaurantId, CurrentUserId, WaitStaff, ct))
        {
            return Forbid();
        }

        var (order, lineFound) = await _orders.RemoveLineAsync(restaurantId, orderId, lineId, ct);
        if (order is null) return NotFound(new { message = "Order not found." });
        if (!lineFound) return NotFound(new { message = "Order line not found." });
        return Ok(order);
    }

    [HttpPut("{orderId}/items/{lineId}/status")]
    public async Task<ActionResult<Order>> SetLineStatus(string restaurantId, string orderId, string lineId, UpdateLineStatusRequest request, CancellationToken ct)
    {
        // Cooks and waiters can advance individual item statuses.
        if (!await _restaurants.HasAnyRoleAsync(restaurantId, CurrentUserId, KitchenStaff, ct))
        {
            return Forbid();
        }

        var (order, lineFound) = await _orders.SetLineStatusAsync(restaurantId, orderId, lineId, request.Status, ct);
        if (order is null) return NotFound(new { message = "Order not found." });
        if (!lineFound) return NotFound(new { message = "Order line not found." });
        return Ok(order);
    }

    [HttpPut("{orderId}/status")]
    public async Task<ActionResult<Order>> SetStatus(string restaurantId, string orderId, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        if (!await _restaurants.HasAnyRoleAsync(restaurantId, CurrentUserId, KitchenStaff, ct))
        {
            return Forbid();
        }

        var order = await _orders.SetStatusAsync(restaurantId, orderId, request.Status, ct);
        return order is null ? NotFound() : Ok(order);
    }
}
