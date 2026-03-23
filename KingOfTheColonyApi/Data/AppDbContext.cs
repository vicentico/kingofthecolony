using KingOfTheColonyApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KingOfTheColonyApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<GameRoom> GameRooms => Set<GameRoom>();
    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
    public DbSet<Spectator> Spectators => Set<Spectator>();
    public DbSet<MatchHistory> MatchHistories => Set<MatchHistory>();
    public DbSet<MatchSession> MatchSessions => Set<MatchSession>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.GoogleId).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<GameRoom>(e =>
        {
            e.HasOne(r => r.KingUser).WithMany().HasForeignKey(r => r.KingUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.ChallengerUser).WithMany().HasForeignKey(r => r.ChallengerUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QueueEntry>(e =>
        {
            e.HasIndex(q => new { q.GameRoomId, q.UserId }).IsUnique();
            e.HasOne(q => q.GameRoom).WithMany(r => r.Queue).HasForeignKey(q => q.GameRoomId);
            e.HasOne(q => q.User).WithMany().HasForeignKey(q => q.UserId);
        });

        modelBuilder.Entity<Spectator>(e =>
        {
            e.HasIndex(s => new { s.GameRoomId, s.UserId }).IsUnique();
            e.HasOne(s => s.GameRoom).WithMany(r => r.Spectators).HasForeignKey(s => s.GameRoomId);
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId);
        });

        modelBuilder.Entity<MatchHistory>(e =>
        {
            e.HasOne(m => m.Winner).WithMany().HasForeignKey(m => m.WinnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Loser).WithMany().HasForeignKey(m => m.LoserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MatchSession>(e =>
        {
            e.HasIndex(m => new { m.GameRoomId, m.Status });
            e.HasOne(m => m.GameRoom).WithMany(r => r.MatchSessions).HasForeignKey(m => m.GameRoomId);
            e.HasOne(m => m.KingUser).WithMany().HasForeignKey(m => m.KingUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.ChallengerUser).WithMany().HasForeignKey(m => m.ChallengerUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.WinnerUser).WithMany().HasForeignKey(m => m.WinnerUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.LoserUser).WithMany().HasForeignKey(m => m.LoserUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.CreatedByUser).WithMany().HasForeignKey(m => m.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.ReportedByUser).WithMany().HasForeignKey(m => m.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CreditTransaction>(e =>
        {
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
        });
    }
}
