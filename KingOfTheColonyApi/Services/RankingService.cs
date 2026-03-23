using KingOfTheColonyApi.Data;
using KingOfTheColonyApi.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace KingOfTheColonyApi.Services;

public class RankingService
{
    private readonly AppDbContext _db;

    public RankingService(AppDbContext db) => _db = db;

    public async Task<List<RankingEntry>> GetRankingAsync(int page = 1, int pageSize = 20)
    {
        var users = await _db.Users
            .Where(u => u.Wins + u.Losses > 0)
            .OrderByDescending(u => u.Wins * 3 + u.BestStreak * 2 - u.Losses)
            .ThenByDescending(u => u.Wins)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return users.Select((u, index) =>
        {
            var total = u.Wins + u.Losses;
            var winRate = total > 0 ? (double)u.Wins / total : 0;
            var score = u.Wins * 3 + u.BestStreak * 2 - u.Losses;

            return new RankingEntry(
                Position: (page - 1) * pageSize + index + 1,
                UserId: u.Id,
                DisplayName: u.DisplayName,
                AvatarUrl: u.AvatarUrl,
                Wins: u.Wins,
                Losses: u.Losses,
                WinRate: Math.Round(winRate, 4),
                BestStreak: u.BestStreak,
                CurrentStreak: u.CurrentStreak,
                Score: score);
        }).ToList();
    }
}
