using System.Security.Claims;
using KingOfTheColonyApi.Hubs;
using KingOfTheColonyApi.Models.Dto;
using KingOfTheColonyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace KingOfTheColonyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly GameRoomService _rooms;
    private readonly IHubContext<GameHub> _hub;

    public RoomsController(GameRoomService rooms, IHubContext<GameHub> hub)
    {
        _rooms = rooms;
        _hub = hub;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ListRooms()
    {
        var rooms = await _rooms.ListActiveRoomsAsync();
        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRoom(int id)
    {
        var room = await _rooms.GetRoomStateAsync(id);
        if (room is null) return NotFound();
        return Ok(room);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromQuery] string name, [FromQuery] string hostIp)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var room = await _rooms.CreateRoomAsync(name, userId.Value, hostIp);
        var state = await _rooms.GetRoomStateAsync(room.Id);

        await _hub.Clients.All.SendAsync("RoomCreated", state);
        return Ok(state);
    }

    [HttpPost("{id:int}/spectate")]
    public async Task<IActionResult> JoinAsSpectator(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, message) = await _rooms.JoinAsSpectatorAsync(id, userId.Value);
        if (!success) return BadRequest(new { message });

        var state = await _rooms.GetRoomStateAsync(id);
        await _hub.Clients.Group($"room-{id}").SendAsync("RoomUpdated", state);
        return Ok(new { message });
    }

    [HttpPost("{id:int}/join-queue")]
    public async Task<IActionResult> JoinQueue(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, message) = await _rooms.JoinQueueAsync(id, userId.Value);
        if (!success) return BadRequest(new { message });

        var state = await _rooms.GetRoomStateAsync(id);
        await _hub.Clients.Group($"room-{id}").SendAsync("RoomUpdated", state);
        return Ok(new { message });
    }

    [HttpPost("{id:int}/report-result")]
    public async Task<IActionResult> ReportResult(int id, [FromBody] ReportMatchRequest request)
    {
        var (success, message, nextChallengerId) =
            await _rooms.ReportMatchResultAsync(id, request.WinnerId, request.LoserId);

        if (!success) return BadRequest(new { message });

        var state = await _rooms.GetRoomStateAsync(id);
        await _hub.Clients.Group($"room-{id}").SendAsync("RoomUpdated", state);

        if (nextChallengerId.HasValue)
        {
            await _hub.Clients.Group($"room-{id}")
                .SendAsync("YourTurn", nextChallengerId.Value);
        }

        return Ok(new { message, nextChallengerId });
    }

    [HttpPost("{id:int}/matches")]
    public async Task<IActionResult> CreateMatchSession(int id, [FromBody] CreateMatchSessionRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, message, session) = await _rooms.CreateMatchSessionAsync(id, userId.Value, request);
        if (!success || session is null) return BadRequest(new { message });

        await _hub.Clients.Group($"room-{id}").SendAsync("MatchCreated", session);
        return Ok(session);
    }

    [HttpPost("{id:int}/matches/{matchSessionId:guid}/started")]
    public async Task<IActionResult> MarkMatchStarted(int id, Guid matchSessionId, [FromBody] StartMatchSessionRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, message, session) = await _rooms.MarkMatchSessionStartedAsync(id, matchSessionId, userId.Value, request);
        if (!success || session is null) return BadRequest(new { message });

        await _hub.Clients.Group($"room-{id}").SendAsync("MatchStarted", session);
        return Ok(session);
    }

    [HttpPost("{id:int}/matches/{matchSessionId:guid}/complete")]
    public async Task<IActionResult> CompleteMatchSession(int id, Guid matchSessionId, [FromBody] CompleteMatchSessionRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, message, result) = await _rooms.CompleteMatchSessionAsync(id, matchSessionId, userId.Value, request);
        if (!success || result is null) return BadRequest(new { message });

        await _hub.Clients.Group($"room-{id}").SendAsync("RoomUpdated", result.RoomState);
        await _hub.Clients.Group($"room-{id}").SendAsync("MatchCompleted", result);

        if (result.NextChallengerId.HasValue)
        {
            await _hub.Clients.Group($"room-{id}")
                .SendAsync("YourTurn", result.NextChallengerId.Value);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/matches/{matchSessionId:guid}/cancel")]
    public async Task<IActionResult> CancelMatchSession(int id, Guid matchSessionId, [FromBody] CancelMatchSessionRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, message, session) = await _rooms.CancelMatchSessionAsync(id, matchSessionId, userId.Value, request);
        if (!success || session is null) return BadRequest(new { message });

        var roomState = await _rooms.GetRoomStateAsync(id);
        if (roomState is not null)
            await _hub.Clients.Group($"room-{id}").SendAsync("RoomUpdated", roomState);

        await _hub.Clients.Group($"room-{id}").SendAsync("MatchCancelled", session);
        return Ok(new { message, session });
    }

    [HttpPost("{id:int}/matches/{matchSessionId:guid}/review")]
    public async Task<IActionResult> MarkMatchForReview(int id, Guid matchSessionId, [FromBody] ReviewMatchSessionRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, message, session) = await _rooms.MarkMatchSessionPendingReviewAsync(id, matchSessionId, userId.Value, request);
        if (!success || session is null) return BadRequest(new { message });

        var roomState = await _rooms.GetRoomStateAsync(id);
        if (roomState is not null)
            await _hub.Clients.Group($"room-{id}").SendAsync("RoomUpdated", roomState);

        await _hub.Clients.Group($"room-{id}").SendAsync("MatchAwaitingResult", session);
        return Ok(new { message, session });
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? int.Parse(claim) : null;
    }
}
