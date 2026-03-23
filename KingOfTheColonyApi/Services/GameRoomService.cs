using KingOfTheColonyApi.Data;
using KingOfTheColonyApi.Models;
using KingOfTheColonyApi.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace KingOfTheColonyApi.Services;

public class GameRoomService
{
    private readonly AppDbContext _db;

    public GameRoomService(AppDbContext db) => _db = db;

    public async Task<GameRoom> CreateRoomAsync(string name, int kingUserId, string hostIp)
    {
        var room = new GameRoom
        {
            Name = name,
            KingUserId = kingUserId,
            HostIp = hostIp,
            Status = "WaitingChallenger"
        };
        _db.GameRooms.Add(room);
        await _db.SaveChangesAsync();
        return room;
    }

    public async Task<RoomStateDto?> GetRoomStateAsync(int roomId)
    {
        var room = await _db.GameRooms
            .Include(r => r.KingUser)
            .Include(r => r.ChallengerUser)
            .Include(r => r.Queue).ThenInclude(q => q.User)
            .Include(r => r.Spectators)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room is null) return null;

        return MapToDto(room);
    }

    public async Task<List<RoomStateDto>> ListActiveRoomsAsync()
    {
        var rooms = await _db.GameRooms
            .Where(r => r.Status != "Idle")
            .Include(r => r.KingUser)
            .Include(r => r.ChallengerUser)
            .Include(r => r.Queue).ThenInclude(q => q.User)
            .Include(r => r.Spectators)
            .ToListAsync();

        return rooms.Select(MapToDto).ToList();
    }

    public async Task<(bool Success, string Message)> JoinAsSpectatorAsync(int roomId, int userId)
    {
        var room = await _db.GameRooms
            .Include(r => r.Spectators)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room is null) return (false, "Sala no encontrada.");

        if (room.Spectators.Any(s => s.UserId == userId))
            return (true, "Ya eres espectador de esta sala.");

        _db.Spectators.Add(new Spectator { GameRoomId = roomId, UserId = userId });
        await _db.SaveChangesAsync();
        return (true, "Te has unido como espectador.");
    }

    public async Task<(bool Success, string Message)> JoinQueueAsync(int roomId, int userId)
    {
        var room = await _db.GameRooms
            .Include(r => r.Queue)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room is null) return (false, "Sala no encontrada.");

        // Check if already in queue, is king, or is challenger
        if (room.KingUserId == userId) return (false, "Ya eres el Rey de esta sala.");
        if (room.ChallengerUserId == userId) return (false, "Ya eres el retador actual.");
        if (room.Queue.Any(q => q.UserId == userId)) return (false, "Ya estás en la cola.");

        // Check credits
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return (false, "Usuario no encontrado.");
        if (user.Credits < 1) return (false, "Créditos insuficientes. Agrega créditos para jugar.");

        // Deduct credit
        user.Credits -= 1;
        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Amount = -1,
            Type = "GameEntry",
            Description = $"Entrada a cola sala #{roomId}"
        });

        var maxPosition = room.Queue.Count > 0 ? room.Queue.Max(q => q.Position) : 0;
        _db.QueueEntries.Add(new QueueEntry
        {
            GameRoomId = roomId,
            UserId = userId,
            Position = maxPosition + 1
        });

        // Remove from spectators if present
        var spectator = await _db.Spectators
            .FirstOrDefaultAsync(s => s.GameRoomId == roomId && s.UserId == userId);
        if (spectator is not null) _db.Spectators.Remove(spectator);

        await _db.SaveChangesAsync();
        return (true, $"Te has unido a la cola en posición #{maxPosition + 1}.");
    }

    public async Task<(bool Success, string Message, int? NextChallengerId)> ReportMatchResultAsync(
        int roomId, int winnerId, int loserId)
    {
        var room = await _db.GameRooms
            .Include(r => r.Queue).ThenInclude(q => q.User)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room is null) return (false, "Sala no encontrada.", null);

        // Record match
        _db.MatchHistories.Add(new MatchHistory
        {
            GameRoomId = roomId,
            WinnerId = winnerId,
            LoserId = loserId
        });

        // Update stats
        var winner = await _db.Users.FindAsync(winnerId);
        var loser = await _db.Users.FindAsync(loserId);

        if (winner is not null)
        {
            winner.Wins++;
            winner.CurrentStreak++;
            if (winner.CurrentStreak > winner.BestStreak)
                winner.BestStreak = winner.CurrentStreak;
        }

        if (loser is not null)
        {
            loser.Losses++;
            loser.CurrentStreak = 0;
        }

        // King stays, loser goes to end of queue (or out)
        // The winner becomes/stays as king
        room.KingUserId = winnerId;

        // Pop next challenger from queue
        var next = room.Queue.OrderBy(q => q.Position).FirstOrDefault();
        int? nextChallengerId = null;

        if (next is not null)
        {
            room.ChallengerUserId = next.UserId;
            room.Status = "Playing";
            nextChallengerId = next.UserId;
            _db.QueueEntries.Remove(next);

            // Re-index positions
            var remaining = room.Queue.Where(q => q.Id != next.Id).OrderBy(q => q.Position).ToList();
            for (int i = 0; i < remaining.Count; i++)
                remaining[i].Position = i + 1;
        }
        else
        {
            room.ChallengerUserId = null;
            room.Status = "WaitingChallenger";
        }

        await _db.SaveChangesAsync();
        return (true, "Resultado registrado.", nextChallengerId);
    }

    private static RoomStateDto MapToDto(GameRoom room)
    {
        return new RoomStateDto(
            RoomId: room.Id,
            Status: room.Status,
            King: room.KingUser is not null ? ToSummary(room.KingUser) : null,
            Challenger: room.ChallengerUser is not null ? ToSummary(room.ChallengerUser) : null,
            Queue: room.Queue.OrderBy(q => q.Position).Select(q => new QueueEntryDto(
                q.Position, ToSummary(q.User), q.JoinedAt)).ToList(),
            SpectatorCount: room.Spectators.Count);
    }

    private static UserSummary ToSummary(User u) =>
        new(u.Id, u.DisplayName, u.AvatarUrl, u.Wins, u.Losses);
}
