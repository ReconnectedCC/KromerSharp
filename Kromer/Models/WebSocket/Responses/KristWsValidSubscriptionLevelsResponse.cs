using Humanizer;

namespace Kromer.Models.WebSocket.Responses;

public class KristWsValidSubscriptionLevelsResponse : IKristWsResponse
{
    public string? RespondingToType { get; set; }
    public bool Ok { get; set; }
    public int Id { get; set; }
    
    public List<string> ValidSubscriptionLevels => Enum.GetNames<SubscriptionLevel>()
        .Select(x => x.Camelize())
        .ToList();
}