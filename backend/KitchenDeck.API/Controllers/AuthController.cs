using System.Security.Claims;
using KitchenDeck.API.DTOs;
using KitchenDeck.API.Models;
using KitchenDeck.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenDeck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _users;
    private readonly ITokenService _tokens;
    private readonly RestaurantService _restaurants;

    public AuthController(UserService users, ITokenService tokens, RestaurantService restaurants)
    {
        _users = users;
        _tokens = tokens;
        _restaurants = restaurants;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var existing = await _users.FindByEmailAsync(request.Email, ct);
        if (existing is not null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var (hash, salt) = PasswordHasher.Hash(request.Password);
        var user = new User
        {
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt
        };

        await _users.SaveAsync(user, ct);
        return Ok(BuildResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(request.Email, ct);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(BuildResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserDto(user.Id, user.Email, user.DisplayName));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return NotFound();

        user.DisplayName = request.DisplayName.Trim();
        await _users.SaveAsync(user, ct);
        return Ok(new UserDto(user.Id, user.Email, user.DisplayName));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return NotFound();

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            return BadRequest(new { message = "Current password is incorrect." });
        }

        var (hash, salt) = PasswordHasher.Hash(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _users.SaveAsync(user, ct);
        return Ok();
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        // Delete restaurants owned by this user.
        var owned = await _restaurants.ListForUserAsync(userId, ct);
        foreach (var r in owned)
        {
            if (r.OwnerUserId == userId)
            {
                await _restaurants.DeleteAsync(r.Id, ct);
            }
        }

        // Delete the user record.
        await _users.DeleteAsync(userId, ct);
        return NoContent();
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = _tokens.CreateToken(user);
        return new AuthResponse(token, new UserDto(user.Id, user.Email, user.DisplayName));
    }
}
