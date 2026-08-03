using System.Security.Claims;
using KitchenDeck.API.DTOs;
using KitchenDeck.API.Models;
using KitchenDeck.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenDeck.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly RestaurantService _restaurants;
    private readonly UserService _users;

    public RestaurantsController(RestaurantService restaurants, UserService users)
    {
        _restaurants = restaurants;
        _users = users;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    public async Task<ActionResult<RestaurantDto>> Create(CreateRestaurantRequest request, CancellationToken ct)
    {
        var restaurant = await _restaurants.CreateAsync(CurrentUserId, request.Name, request.KitchenPasscode, ct);
        return Ok(new RestaurantDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.OwnerUserId,
            IsOwner: true,
            MyRoles: new[] { StaffRole.Admin },
            HasKitchenPasscode: restaurant.KitchenPasscodeHash is not null));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RestaurantDto>>> ListMine(CancellationToken ct)
    {
        var restaurants = await _restaurants.ListForUserAsync(CurrentUserId, ct);
        var dtos = new List<RestaurantDto>();
        foreach (var r in restaurants)
        {
            var roles = await _restaurants.GetRolesAsync(r.Id, CurrentUserId, ct);
            dtos.Add(new RestaurantDto(
                r.Id, r.Name, r.OwnerUserId,
                IsOwner: r.OwnerUserId == CurrentUserId,
                MyRoles: roles,
                HasKitchenPasscode: r.KitchenPasscodeHash is not null));
        }

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RestaurantDto>> Get(string id, CancellationToken ct)
    {
        var restaurant = await _restaurants.GetByIdAsync(id, ct);
        if (restaurant is null)
        {
            return NotFound();
        }

        var roles = await _restaurants.GetRolesAsync(id, CurrentUserId, ct);
        var isOwner = restaurant.OwnerUserId == CurrentUserId;
        if (!isOwner && roles.Count == 0)
        {
            return Forbid();
        }

        return Ok(new RestaurantDto(
            restaurant.Id, restaurant.Name, restaurant.OwnerUserId,
            isOwner, roles, restaurant.KitchenPasscodeHash is not null));
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<IEnumerable<MemberDto>>> ListMembers(string id, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(id, CurrentUserId, ct))
        {
            return Forbid();
        }

        var membership = await _restaurants.GetMembershipAsync(id, ct);
        var dtos = new List<MemberDto>();
        foreach (var member in membership.Members)
        {
            var user = await _users.GetByIdAsync(member.UserId, ct);
            dtos.Add(new MemberDto(
                member.UserId,
                user?.DisplayName ?? "(unknown)",
                user?.Email ?? "",
                member.Roles));
        }

        return Ok(dtos);
    }

    [HttpPost("{id}/members")]
    public async Task<ActionResult<MemberDto>> AddMember(string id, AddMemberRequest request, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(id, CurrentUserId, ct))
        {
            return Forbid();
        }

        var user = await _users.FindByEmailAsync(request.Email, ct);
        if (user is null)
        {
            return NotFound(new { message = "No user exists with that email. Ask them to register first." });
        }

        await _restaurants.AddOrUpdateMemberAsync(id, user.Id, request.Roles ?? new List<StaffRole>(), ct);
        return Ok(new MemberDto(user.Id, user.DisplayName, user.Email, request.Roles ?? new List<StaffRole>()));
    }

    [HttpPut("{id}/members/{userId}/roles")]
    public async Task<IActionResult> UpdateRoles(string id, string userId, UpdateRolesRequest request, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(id, CurrentUserId, ct))
        {
            return Forbid();
        }

        await _restaurants.AddOrUpdateMemberAsync(id, userId, request.Roles ?? new List<StaffRole>(), ct);
        return NoContent();
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId, CancellationToken ct)
    {
        var restaurant = await _restaurants.GetByIdAsync(id, ct);
        if (restaurant is null)
        {
            return NotFound();
        }

        if (!await _restaurants.IsAdminAsync(id, CurrentUserId, ct))
        {
            return Forbid();
        }

        if (userId == restaurant.OwnerUserId)
        {
            return BadRequest(new { message = "The owner cannot be removed from the restaurant." });
        }

        await _restaurants.RemoveMemberAsync(id, userId, ct);
        return NoContent();
    }

    [HttpPut("{id}/kitchen-passcode")]
    public async Task<IActionResult> SetKitchenPasscode(string id, SetKitchenPasscodeRequest request, CancellationToken ct)
    {
        var restaurant = await _restaurants.GetByIdAsync(id, ct);
        if (restaurant is null)
        {
            return NotFound();
        }

        if (!await _restaurants.IsAdminAsync(id, CurrentUserId, ct))
        {
            return Forbid();
        }

        var (hash, salt) = PasswordHasher.Hash(request.Passcode);
        restaurant.KitchenPasscodeHash = hash;
        restaurant.KitchenPasscodeSalt = salt;
        await _restaurants.SaveAsync(restaurant, ct);
        return NoContent();
    }
}
