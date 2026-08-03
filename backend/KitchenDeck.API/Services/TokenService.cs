using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KitchenDeck.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace KitchenDeck.API.Services;

public class JwtOptions
{
    public string Issuer { get; set; } = "KitchenDeck";
    public string Audience { get; set; } = "KitchenDeck";
    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}

public interface ITokenService
{
    string CreateToken(User user);
    string CreateKitchenToken(string restaurantId, string restaurantName, int expiryMinutes);
}

/// <summary>
/// Issues signed JWT bearer tokens for authenticated users.
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(JwtOptions options)
    {
        _options = options;
    }

    public string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id),
            new("displayName", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateKitchenToken(string restaurantId, string restaurantName, int expiryMinutes)
    {
        var claims = new List<Claim>
        {
            new("kitchen_restaurant", restaurantId),
            new("displayName", $"Kitchen · {restaurantName}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}