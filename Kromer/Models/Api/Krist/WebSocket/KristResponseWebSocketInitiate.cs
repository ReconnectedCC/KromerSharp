namespace Kromer.Models.Api.Krist.WebSocket;

public class KristResponseWebSocketInitiate : KristResult
{
    public Uri Url { get; set; }
    public int Expires { get; set; } = 30;
}