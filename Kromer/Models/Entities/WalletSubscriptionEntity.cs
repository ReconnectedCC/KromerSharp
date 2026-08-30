namespace Kromer.Models.Entities;

public class WalletSubscriptionEntity
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public SubscriptionContractEntity Contract { get; set; } = null!;

    public string WalletAddress { get; set; } = null!;

    public DateTime NextPayment { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public string? CancellationReason { get; set; }

    public bool CanUnsubscribe { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}
