using System.Security.Claims;
using KitchenDeck.API.DTOs;
using KitchenDeck.API.Models;
using KitchenDeck.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KitchenDeck.API.Controllers;

[ApiController]
[Authorize]
[Route("api/restaurants/{restaurantId}/tables")]
public class TablesController : ControllerBase
{
    private readonly TableService _tables;
    private readonly RestaurantService _restaurants;

    public TablesController(TableService tables, RestaurantService restaurants)
    {
        _tables = tables;
        _restaurants = restaurants;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiningTable>>> List(string restaurantId, CancellationToken ct)
    {
        if (!await _restaurants.IsMemberAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        return Ok(await _tables.ListAsync(restaurantId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<DiningTable>> Create(string restaurantId, TableRequest request, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        var table = await _tables.AddAsync(restaurantId, new DiningTable
        {
            Number = request.Number,
            Label = request.Label,
            Seats = request.Seats
        }, ct);

        return table is null
            ? Conflict(new { message = $"Table number {request.Number} already exists." })
            : Ok(table);
    }

    [HttpPut("{tableId}")]
    public async Task<ActionResult<DiningTable>> Update(string restaurantId, string tableId, TableRequest request, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        var (table, numberConflict) = await _tables.UpdateAsync(restaurantId, tableId, new DiningTable
        {
            Number = request.Number,
            Label = request.Label,
            Seats = request.Seats
        }, ct);

        if (numberConflict)
        {
            return Conflict(new { message = $"Table number {request.Number} already exists." });
        }

        return table is null ? NotFound() : Ok(table);
    }

    [HttpDelete("{tableId}")]
    public async Task<IActionResult> Delete(string restaurantId, string tableId, CancellationToken ct)
    {
        if (!await _restaurants.IsAdminAsync(restaurantId, CurrentUserId, ct))
        {
            return Forbid();
        }

        return await _tables.DeleteAsync(restaurantId, tableId, ct) ? NoContent() : NotFound();
    }
}
