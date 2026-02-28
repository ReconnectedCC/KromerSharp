using Humanizer;

namespace Kromer.Models.WebSocket.Responses;

public class KristWsSubscriptionLevelResponse : IKristWsResponse
{
    public string? RespondingToType { get; set; }
    public bool Ok { get; set; }
    public int Id { get; set; }

    public List<string> SubscriptionLevel { get; set; } = [];
}