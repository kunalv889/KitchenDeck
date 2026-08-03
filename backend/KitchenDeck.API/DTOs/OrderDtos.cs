using System.ComponentModel.DataAnnotations;
using KitchenDeck.API.Models;

namespace KitchenDeck.API.DTOs;

public record OrderLineInput(
    [Required] string MenuItemId,
    [Range(1, 1000)] int Quantity,
    string? Notes);

public record CreateOrderRequest(
    [Required] string TableId,
    List<OrderLineInput> Items);

public record AddLinesRequest(List<OrderLineInput> Items);

public record UpdateLineRequest(
    [Range(0, 1000)] int Quantity,
    string? Notes);

public record UpdateLineStatusRequest(OrderItemStatus Status);

public record UpdateOrderStatusRequest(OrderStatus Status);
