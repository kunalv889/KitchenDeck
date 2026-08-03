using System.ComponentModel.DataAnnotations;

namespace KitchenDeck.API.DTOs;

public record KitchenAccessRequest(
    [Required] string Passcode);

public record KitchenAccessResponse(
    string Token,
    string RestaurantId,
    string RestaurantName,
    int ExpiresInMinutes);
