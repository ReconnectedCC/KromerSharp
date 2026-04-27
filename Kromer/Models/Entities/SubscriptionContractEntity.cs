namespace Kromer.Models.Entities;

public class SubscriptionContractEntity
{
    public int Id { get; set; }

    public string Receiver { get; set; } = null!;

    public string BaseName { get; set; } = null!;

    public decimal Price { get; set; }

    public int PeriodMinutes { get; set; }

    public string Description { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<WalletSubscriptionEntity> WalletSubscriptions { get; set; } = [];
}
