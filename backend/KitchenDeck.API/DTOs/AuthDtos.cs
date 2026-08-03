using System.ComponentModel.DataAnnotations;

namespace KitchenDeck.API.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string DisplayName);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(string Token, UserDto User);

public record UserDto(string Id, string Email, string DisplayName);
