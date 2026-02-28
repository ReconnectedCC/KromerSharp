namespace Kromer.Models.WebSocket;

public class KristKeepAlivePacket : KristWsPacket
{
    public new string Type => "keepalive";
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
}