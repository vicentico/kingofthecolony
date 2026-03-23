using System.Security.Claims;
using KingOfTheColonyApi.Models.Dto;
using KingOfTheColonyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KingOfTheColonyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CreditsController : ControllerBase
{
    private readonly CreditService _credits;

    public CreditsController(CreditService credits) => _credits = credits;

    [HttpPost("add")]
    public async Task<IActionResult> AddCredits([FromBody] AddCreditsRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (request.Amount <= 0)
            return BadRequest(new { message = "La cantidad debe ser mayor a 0." });

        var (success, newBalance) = await _credits.AddCreditsAsync(userId.Value, request.Amount);
        if (!success) return BadRequest(new { message = "No se pudieron agregar los créditos." });

        return Ok(new { credits = newBalance });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var history = await _credits.GetHistoryAsync(userId.Value);
        return Ok(history);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? int.Parse(claim) : null;
    }
}
