using System.ComponentModel.DataAnnotations;
using KitchenDeck.API.Models;

namespace KitchenDeck.API.DTOs;

public record CreateRestaurantRequest(
    [Required] string Name,
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Passcode must be exactly 6 digits.")]
    string? KitchenPasscode);

public record RestaurantDto(
    string Id,
    string Name,
    string OwnerUserId,
    bool IsOwner,
    IReadOnlyList<StaffRole> MyRoles,
    bool HasKitchenPasscode);

public record AddMemberRequest(
    [Required, EmailAddress] string Email,
    List<StaffRole> Roles);

public record UpdateRolesRequest(List<StaffRole> Roles);

public record MemberDto(
    string UserId,
    string DisplayName,
    string Email,
    IReadOnlyList<StaffRole> Roles);

public record SetKitchenPasscodeRequest(
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "Passcode must be exactly 6 digits.")]
    string Passcode);
