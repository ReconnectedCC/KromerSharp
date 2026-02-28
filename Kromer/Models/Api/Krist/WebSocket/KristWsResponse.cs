using Kromer.Models.WebSocket;

namespace Kromer.Models.Api.Krist.WebSocket;

public class KristWsResponse : KristWsPacket
{
    public bool Ok { get; set; }
}