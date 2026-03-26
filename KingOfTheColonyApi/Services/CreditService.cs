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

        using var transaction = _db.Database.BeginTransaction();

        var updatedRows = _db.Database.ExecuteSqlInterpolated($@"
            UPDATE ""Users""
            SET ""Credits"" = ""Credits"" + {amount}
            WHERE ""Id"" = {userId}");

        if (updatedRows == 0)
        {
            transaction.Rollback();
            return (false, 0);
        }

        _db.Database.ExecuteSqlInterpolated($@"
            INSERT INTO ""CreditTransactions"" (""UserId"", ""Amount"", ""Type"", ""Description"", ""CreatedAt"")
            VALUES ({userId}, {amount}, {"Purchase"}, {$"Compra de {amount} crédito(s)"}, {DateTime.UtcNow})");

        var newBalance = _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Credits)
            .Single();

        transaction.Commit();
        return (true, newBalance);
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
