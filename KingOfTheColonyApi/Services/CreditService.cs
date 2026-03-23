using KingOfTheColonyApi.Data;
using KingOfTheColonyApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KingOfTheColonyApi.Services;

public class CreditService
{
    private readonly AppDbContext _db;

    public CreditService(AppDbContext db) => _db = db;

    public async Task<(bool Success, int NewBalance)> AddCreditsAsync(int userId, int amount)
    {
        if (amount <= 0) return (false, 0);

        var user = await _db.Users.FindAsync(userId);
        if (user is null) return (false, 0);

        user.Credits += amount;

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Amount = amount,
            Type = "Purchase",
            Description = $"Compra de {amount} crédito(s)"
        });

        await _db.SaveChangesAsync();
        return (true, user.Credits);
    }

    public async Task<List<CreditTransaction>> GetHistoryAsync(int userId, int limit = 20)
    {
        return await _db.CreditTransactions
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}
