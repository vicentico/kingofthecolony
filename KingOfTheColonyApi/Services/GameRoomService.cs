using System.Text.Json;
using KingOfTheColonyApi.Data;
using KingOfTheColonyApi.Models;
using KingOfTheColonyApi.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace KingOfTheColonyApi.Services;

public class GameRoomService
{
    private static readonly string[] ActiveMatchStatuses = ["Created", "Running", "AwaitingResult", "PendingReview"];
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

        if (room.ChallengerUserId is null)
        {
            room.ChallengerUserId = userId;
            room.Status = "ReadyToPlay";
        }
        else
        {
            var maxPosition = room.Queue.Count > 0 ? room.Queue.Max(q => q.Position) : 0;
            _db.QueueEntries.Add(new QueueEntry
            {
                GameRoomId = roomId,
                UserId = userId,
                Position = maxPosition + 1
            });
        }

        // Remove from spectators if present
        var spectator = await _db.Spectators
            .FirstOrDefaultAsync(s => s.GameRoomId == roomId && s.UserId == userId);
        if (spectator is not null) _db.Spectators.Remove(spectator);

        await _db.SaveChangesAsync();
        return room.ChallengerUserId == userId
            ? (true, "Eres el retador actual. La sala ya puede iniciar la partida.")
            : (true, $"Te has unido a la cola en posición #{room.Queue.Count + 1}.");
    }

    public async Task<(bool Success, string Message, MatchSessionDto? Session)> CreateMatchSessionAsync(
        int roomId,
        int createdByUserId,
        CreateMatchSessionRequest request)
    {
        var room = await _db.GameRooms
            .Include(r => r.KingUser)
            .Include(r => r.ChallengerUser)
            .Include(r => r.MatchSessions)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room is null) return (false, "Sala no encontrada.", null);
        if (room.KingUserId is null || room.ChallengerUserId is null || room.KingUser is null || room.ChallengerUser is null)
            return (false, "La sala no tiene rey y retador listos para iniciar la partida.", null);

        if (room.KingUserId != request.KingUserId || room.ChallengerUserId != request.ChallengerUserId)
            return (false, "El estado de la sala cambió. Actualiza e intenta de nuevo.", null);

        if (createdByUserId != room.KingUserId && createdByUserId != room.ChallengerUserId)
            return (false, "Solo el rey o el retador actual pueden iniciar una sesión de partida.", null);

        if (room.MatchSessions.Any(m => ActiveMatchStatuses.Contains(m.Status)))
            return (false, "Ya existe una sesión de partida activa para esta sala.", null);

        var session = new MatchSession
        {
            GameRoomId = roomId,
            KingUserId = room.KingUserId.Value,
            ChallengerUserId = room.ChallengerUserId.Value,
            GameRom = string.IsNullOrWhiteSpace(request.GameRom) ? room.GameRom : request.GameRom,
            LaunchSource = string.IsNullOrWhiteSpace(request.LaunchSource) ? "LauncherWpf" : request.LaunchSource,
            ClientInstanceId = request.ClientInstanceId,
            CreatedByUserId = createdByUserId,
            Status = "Created"
        };

        _db.MatchSessions.Add(session);
        await _db.SaveChangesAsync();

        return (true, "Sesión de partida creada.", ToMatchSessionDto(session, room.KingUser, room.ChallengerUser));
    }

    public async Task<(bool Success, string Message, MatchSessionDto? Session)> MarkMatchSessionStartedAsync(
        int roomId,
        Guid matchSessionId,
        int reporterUserId,
        StartMatchSessionRequest request)
    {
        var session = await _db.MatchSessions
            .Include(m => m.GameRoom)
            .Include(m => m.KingUser)
            .Include(m => m.ChallengerUser)
            .FirstOrDefaultAsync(m => m.GameRoomId == roomId && m.Id == matchSessionId);

        if (session is null) return (false, "Sesión de partida no encontrada.", null);
        if (reporterUserId != session.KingUserId && reporterUserId != session.ChallengerUserId)
            return (false, "Solo un jugador activo puede marcar la sesión como iniciada.", null);
        if (session.Status is "Completed" or "Cancelled")
            return (false, "La sesión ya está cerrada.", null);

        session.EmulatorProcessId = request.EmulatorProcessId;
        session.StartedAtUtc = request.StartedAtUtc ?? DateTime.UtcNow;
        session.Status = "Running";
        session.GameRoom.Status = "Playing";

        await _db.SaveChangesAsync();
        return (true, "Sesión marcada como iniciada.", ToMatchSessionDto(session, session.KingUser, session.ChallengerUser));
    }

    public async Task<(bool Success, string Message, MatchSessionCompletionResponse? Result)> CompleteMatchSessionAsync(
        int roomId,
        Guid matchSessionId,
        int reporterUserId,
        CompleteMatchSessionRequest request)
    {
        var session = await _db.MatchSessions
            .Include(m => m.GameRoom)
                .ThenInclude(r => r.Queue)
                    .ThenInclude(q => q.User)
            .Include(m => m.GameRoom)
                .ThenInclude(r => r.KingUser)
            .Include(m => m.GameRoom)
                .ThenInclude(r => r.ChallengerUser)
            .Include(m => m.GameRoom)
                .ThenInclude(r => r.Spectators)
            .Include(m => m.KingUser)
            .Include(m => m.ChallengerUser)
            .FirstOrDefaultAsync(m => m.GameRoomId == roomId && m.Id == matchSessionId);

        if (session is null) return (false, "Sesión de partida no encontrada.", null);
        if (session.Status == "Completed")
        {
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) && session.LastIdempotencyKey == request.IdempotencyKey)
            {
                var existingState = await GetRoomStateAsync(roomId);
                if (existingState is null) return (false, "Sala no encontrada.", null);

                return (true, "Resultado ya registrado previamente.", new MatchSessionCompletionResponse(
                    session.Id,
                    session.Status,
                    "Resultado ya registrado previamente.",
                    session.GameRoom.ChallengerUserId,
                    session.GameRoom.ChallengerUser?.DisplayName,
                    existingState));
            }

            return (false, "La sesión ya fue cerrada.", null);
        }

        if (session.Status is "Cancelled" or "PendingReview")
            return (false, "La sesión no admite más resultados porque ya fue cerrada en otro estado.", null);

        if (reporterUserId != session.KingUserId && reporterUserId != session.ChallengerUserId)
            return (false, "Solo un jugador activo puede reportar el resultado.", null);

        var validParticipants = new[] { session.KingUserId, session.ChallengerUserId };
        if (!validParticipants.Contains(request.WinnerUserId) || !validParticipants.Contains(request.LoserUserId))
            return (false, "El ganador y el perdedor deben coincidir con los jugadores de la sesión.", null);

        if (request.WinnerUserId == request.LoserUserId)
            return (false, "Ganador y perdedor no pueden ser el mismo jugador.", null);

        session.WinnerUserId = request.WinnerUserId;
        session.LoserUserId = request.LoserUserId;
        session.ResultSource = string.IsNullOrWhiteSpace(request.ResultSource) ? "ManualSelection" : request.ResultSource;
        session.EvidencePayload = request.Evidence?.GetRawText();
        session.ReportedByUserId = reporterUserId;
        session.EndedAtUtc = request.EndedAtUtc ?? DateTime.UtcNow;
        session.ReportedAtUtc = DateTime.UtcNow;
        session.LastIdempotencyKey = request.IdempotencyKey;
        session.Status = "Completed";

        var (nextChallengerId, nextChallengerDisplayName) = await ApplyMatchResultToRoomAsync(
            session.GameRoom,
            request.WinnerUserId,
            request.LoserUserId);

        await _db.SaveChangesAsync();

        var roomState = MapToDto(session.GameRoom);
        return (true, "Resultado registrado.", new MatchSessionCompletionResponse(
            session.Id,
            session.Status,
            "Resultado registrado.",
            nextChallengerId,
            nextChallengerDisplayName,
            roomState));
    }

    public async Task<(bool Success, string Message, MatchSessionDto? Session)> CancelMatchSessionAsync(
        int roomId,
        Guid matchSessionId,
        int reporterUserId,
        CancelMatchSessionRequest request)
    {
        var session = await _db.MatchSessions
            .Include(m => m.GameRoom)
            .Include(m => m.KingUser)
            .Include(m => m.ChallengerUser)
            .FirstOrDefaultAsync(m => m.GameRoomId == roomId && m.Id == matchSessionId);

        if (session is null) return (false, "Sesión de partida no encontrada.", null);
        if (session.Status is "Completed" or "Cancelled")
            return (false, "La sesión ya fue cerrada.", null);
        if (reporterUserId != session.KingUserId && reporterUserId != session.ChallengerUserId)
            return (false, "Solo un jugador activo puede cancelar la sesión.", null);

        session.Status = "Cancelled";
        session.ReportedByUserId = reporterUserId;
        session.ResultReason = request.Reason;
        session.EndedAtUtc = request.EndedAtUtc ?? DateTime.UtcNow;
        session.ReportedAtUtc = DateTime.UtcNow;
        session.GameRoom.Status = session.GameRoom.ChallengerUserId is null ? "WaitingChallenger" : "ReadyToPlay";

        await _db.SaveChangesAsync();
        return (true, "La partida fue cancelada y no afecta el ranking.", ToMatchSessionDto(session, session.KingUser, session.ChallengerUser));
    }

    public async Task<(bool Success, string Message, MatchSessionDto? Session)> MarkMatchSessionPendingReviewAsync(
        int roomId,
        Guid matchSessionId,
        int reporterUserId,
        ReviewMatchSessionRequest request)
    {
        var session = await _db.MatchSessions
            .Include(m => m.GameRoom)
            .Include(m => m.KingUser)
            .Include(m => m.ChallengerUser)
            .FirstOrDefaultAsync(m => m.GameRoomId == roomId && m.Id == matchSessionId);

        if (session is null) return (false, "Sesión de partida no encontrada.", null);
        if (session.Status is "Completed" or "Cancelled")
            return (false, "La sesión ya fue cerrada.", null);
        if (reporterUserId != session.KingUserId && reporterUserId != session.ChallengerUserId)
            return (false, "Solo un jugador activo puede enviar la sesión a revisión.", null);

        session.Status = "PendingReview";
        session.ReportedByUserId = reporterUserId;
        session.ResultReason = request.Reason;
        session.EndedAtUtc = request.EndedAtUtc ?? DateTime.UtcNow;
        session.ReportedAtUtc = DateTime.UtcNow;
        session.GameRoom.Status = "ResultReview";

        await _db.SaveChangesAsync();
        return (true, "La partida quedó pendiente de revisión manual.", ToMatchSessionDto(session, session.KingUser, session.ChallengerUser));
    }

    public async Task<(bool Success, string Message, int? NextChallengerId)> ReportMatchResultAsync(
        int roomId, int winnerId, int loserId)
    {
        var room = await _db.GameRooms
            .Include(r => r.Queue).ThenInclude(q => q.User)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room is null) return (false, "Sala no encontrada.", null);

        var (nextChallengerId, _) = await ApplyMatchResultToRoomAsync(room, winnerId, loserId);

        await _db.SaveChangesAsync();
        return (true, "Resultado registrado.", nextChallengerId);
    }

    private async Task<(int? NextChallengerId, string? NextChallengerDisplayName)> ApplyMatchResultToRoomAsync(
        GameRoom room,
        int winnerId,
        int loserId)
    {
        _db.MatchHistories.Add(new MatchHistory
        {
            GameRoomId = room.Id,
            WinnerId = winnerId,
            LoserId = loserId
        });

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

        room.KingUserId = winnerId;
        room.KingUser = winner;

        var next = room.Queue.OrderBy(q => q.Position).FirstOrDefault();
        int? nextChallengerId = null;
        string? nextChallengerDisplayName = null;

        if (next is not null)
        {
            room.ChallengerUserId = next.UserId;
            room.ChallengerUser = next.User;
            room.Status = "ReadyToPlay";
            nextChallengerId = next.UserId;
            nextChallengerDisplayName = next.User.DisplayName;
            _db.QueueEntries.Remove(next);

            var remaining = room.Queue.Where(q => q.Id != next.Id).OrderBy(q => q.Position).ToList();
            for (int i = 0; i < remaining.Count; i++)
                remaining[i].Position = i + 1;
        }
        else
        {
            room.ChallengerUserId = null;
            room.ChallengerUser = null;
            room.Status = "WaitingChallenger";
        }

        return (nextChallengerId, nextChallengerDisplayName);
    }

    private static MatchSessionDto ToMatchSessionDto(MatchSession session, User king, User challenger)
    {
        return new MatchSessionDto(
            session.Id,
            session.GameRoomId,
            session.Status,
            session.GameRom,
            ToSummary(king),
            ToSummary(challenger),
            session.CreatedAtUtc,
            session.StartedAtUtc,
            session.EndedAtUtc);
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
        new(u.Id, u.DisplayName, u.AvatarUrl, u.Credits, u.Wins, u.Losses);
}
