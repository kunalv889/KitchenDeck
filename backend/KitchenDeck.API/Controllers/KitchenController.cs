using System.Security.Claims;
using KitchenDeck.API.DTOs;
using KitchenDeck.API.Models;
using KitchenDeck.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenDeck.API.Controllers;

/// <summary>
/// Public kitchen-window access. A display is unlocked with the restaurant's 6-digit
/// passcode, which returns a scoped, short-lived kitchen token. That token grants
/// read-only access to the restaurant's active orders (and nothing else).
/// </summary>
[ApiController]
[Route("api/restaurants/{restaurantId}/kitchen")]
public class KitchenController : ControllerBase
{
    private const int KitchenTokenMinutes = 720; // 12 hours

    private readonly RestaurantService _restaurants;
    private readonly OrderService _orders;
    private readonly ITokenService _tokens;

    public KitchenController(RestaurantService restaurants, OrderService orders, ITokenService tokens)
    {
        _restaurants = restaurants;
        _orders = orders;
        _tokens = tokens;
    }

    [HttpPost("access")]
    [AllowAnonymous]
    public async Task<ActionResult<KitchenAccessResponse>> Access(string restaurantId, KitchenAccessRequest request, CancellationToken ct)
    {
        var restaurant = await _restaurants.GetByIdAsync(restaurantId, ct);
        if (restaurant is null)
        {
            return NotFound(new { message = "Restaurant not found." });
        }

        if (restaurant.KitchenPasscodeHash is null)
        {
            return BadRequest(new { message = "No kitchen passcode has been set for this restaurant." });
        }

        if (!_restaurants.VerifyKitchenPasscode(restaurant, request.Passcode ?? string.Empty))
        {
            return Unauthorized(new { message = "Invalid passcode." });
        }

        var token = _tokens.CreateKitchenToken(restaurantId, restaurant.Name, KitchenTokenMinutes);
        return Ok(new KitchenAccessResponse(token, restaurantId, restaurant.Name, KitchenTokenMinutes));
    }

    [HttpGet("orders")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Order>>> Orders(string restaurantId, CancellationToken ct)
    {
        // Access is allowed either with a kitchen token scoped to this restaurant,
        // or by an authenticated member of the restaurant.
        var kitchenScope = User.FindFirstValue("kitchen_restaurant");
        if (kitchenScope != restaurantId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null || !await _restaurants.IsMemberAsync(restaurantId, userId, ct))
            {
                return Forbid();
            }
        }

        return Ok(await _orders.ListForRestaurantAsync(restaurantId, activeOnly: true, ct));
    }
}
