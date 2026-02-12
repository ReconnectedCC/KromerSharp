namespace Kromer.Models.Entities;

public partial class WalletEntity
{
    public int Id { get; set; }

    public string Address { get; set; } = null!;

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool? Locked { get; set; }

    public decimal TotalIn { get; set; }

    public decimal TotalOut { get; set; }

    public string? PrivateKey { get; set; }
}
