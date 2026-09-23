namespace Kromer.Models.Dto;

public class WalletSubscriptionDto
{
    public string Address { get; set; } = null!;

    public DateTime NextPayment { get; set; }

    public bool Unsubscribable { get; set; }
}
