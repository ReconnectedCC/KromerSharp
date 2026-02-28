namespace Kromer.Models.WebSocket.Packets;

public class KristKeepAlivePacket : IKristWsPacket
{
    public string Type => "keepalive";
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
}