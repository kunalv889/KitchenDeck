using System.ComponentModel.DataAnnotations;

namespace KitchenDeck.API.DTOs;

public record MenuItemRequest(
    [Required] string Name,
    string? Description,
    string? Category,
    [Range(0, 1_000_000)] decimal Price,
    bool IsAvailable = true);

public record TableRequest(
    [Range(1, 100_000)] int Number,
    string? Label,
    [Range(0, 100)] int Seats);
