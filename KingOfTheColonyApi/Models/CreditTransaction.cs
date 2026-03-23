namespace KingOfTheColonyApi.Models;

public class CreditTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Amount (positive = add, negative = spend)</summary>
    public int Amount { get; set; }

    /// <summary>Purchase, GameEntry, Refund, Admin</summary>
    public string Type { get; set; } = "";

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
