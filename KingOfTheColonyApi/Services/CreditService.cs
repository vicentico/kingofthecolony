using System.Globalization;
using KingOfTheColonyApi.Data;
using KingOfTheColonyApi.Extensions;
using KingOfTheColonyApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KingOfTheColonyApi.Services;

public class CreditService
{
    private readonly AppDbContext _db;
    private readonly string _writeConnectionString;

    public CreditService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _writeConnectionString = configuration.GetOptionalConnectionString("WriteConnection", "ConnectionStrings__WriteConnection")
            ?? configuration.GetOptionalConnectionString("MigrationConnection", "ConnectionStrings__MigrationConnection")
            ?? configuration.GetRequiredConnectionString("DefaultConnection", "ConnectionStrings__DefaultConnection");
    }

    public async Task<(bool Success, int NewBalance)> AddCreditsAsync(int userId, int amount)
    {
        if (amount <= 0) return (false, 0);

        var connectionBuilder = new NpgsqlConnectionStringBuilder(_writeConnectionString)
        {
            Pooling = false,
            Enlist = false
        };

        await using var connection = new NpgsqlConnection(connectionBuilder.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            WITH updated AS (
                UPDATE ""Users""
                SET ""Credits"" = ""Credits"" + @amount
                WHERE ""Id"" = @userId
                RETURNING ""Id"", ""Credits""
            ), inserted AS (
                INSERT INTO ""CreditTransactions"" (""UserId"", ""Amount"", ""Type"", ""Description"", ""CreatedAt"")
                SELECT ""Id"", @amount, @type, @description, @createdAt
                FROM updated
                RETURNING 1
            )
            SELECT ""Credits"" FROM updated;";
        command.Parameters.AddWithValue("amount", amount);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("type", "Purchase");
        command.Parameters.AddWithValue("description", $"Compra de {amount} crédito(s)");
        command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        var result = await command.ExecuteScalarAsync();
        if (result is null || result == DBNull.Value)
        {
            await transaction.RollbackAsync();
            return (false, 0);
        }

        await transaction.CommitAsync();
        var newBalance = Convert.ToInt32(result, CultureInfo.InvariantCulture);
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
