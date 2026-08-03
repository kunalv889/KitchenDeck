using System.Security.Claims;
using KitchenDeck.API.DTOs;
using KitchenDeck.API.Models;
using KitchenDeck.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenDeck.API.Controllers;

[ApiController]
[Authorize]
[Route("api/restaurants/{restaurantId}/menu")]
public class MenuController : ControllerBase
{
    private readonly MenuService _menu;
    private readonly RestaurantService _restaurants;

    public MenuController(MenuService menu, RestaurantService restaurants)
    {
        _menu = menu;
        _restaurants = restaurants;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItem>>> List(string restaurantId, CancellationToken ct)
    {
        if (!await _restaurants.IsMemberAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        return Ok(await _menu.ListAsync(restaurantId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<MenuItem>> Create(string restaurantId, MenuItemRequest request, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        var item = await _menu.AddAsync(restaurantId, new MenuItem
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Category = request.Category,
            Price = request.Price,
            IsAvailable = request.IsAvailable
        }, ct);

        return Ok(item);
    }

    [HttpPut("{itemId}")]
    public async Task<ActionResult<MenuItem>> Update(string restaurantId, string itemId, MenuItemRequest request, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        var updated = await _menu.UpdateAsync(restaurantId, itemId, new MenuItem
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Category = request.Category,
            Price = request.Price,
            IsAvailable = request.IsAvailable
        }, ct);

        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> Delete(string restaurantId, string itemId, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        return await _menu.DeleteAsync(restaurantId, itemId, ct) ? NoContent() : NotFound();
    }
}
