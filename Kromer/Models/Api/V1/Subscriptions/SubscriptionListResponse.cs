using Kromer.Models.Dto;

namespace Kromer.Models.Api.V1.Subscriptions;

public class SubscriptionListResponse
{
    public int Count { get; set; }

    public int Total { get; set; }

    public IEnumerable<SubscriptionDto> Subscriptions { get; set; } = [];
}
