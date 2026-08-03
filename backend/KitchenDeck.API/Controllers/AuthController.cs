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

    public AuthController(UserService users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
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

    private AuthResponse BuildResponse(User user)
    {
        var token = _tokens.CreateToken(user);
        return new AuthResponse(token, new UserDto(user.Id, user.Email, user.DisplayName));
    }
}
