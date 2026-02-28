namespace Kromer.Models.WebSocket.Requests;

public class KristWsSubscribeRequest : KristWsRequest
{
    public string Event { get; set; }
}