using Kromer.Models.Api.Krist.Misc;

namespace Kromer.Models.WebSocket.Packets;

public class KristHelloPacket : KristMotdResponse, IKristWsPacket
{
    public string Type => "hello";
}