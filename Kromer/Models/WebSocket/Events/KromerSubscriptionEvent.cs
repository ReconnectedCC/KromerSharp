namespace Kromer.Models.WebSocket.Events;

public class KromerSubscriptionEvent : IKristEvent
{
    public string Type => "event";

    public string Event => "subscription";

    public string Action { get; set; } = null!;

    public int ContractId { get; set; }

    public int? SubscriptionId { get; set; }

    public string? OwnerAddress { get; set; }

    public string? SubscriberAddress { get; set; }

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime? NextPayment { get; set; }
}
