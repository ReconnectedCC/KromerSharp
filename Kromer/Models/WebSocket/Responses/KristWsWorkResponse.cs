namespace Kromer.Models.WebSocket.Responses;

public class KristWsWorkResponse : IKristWsResponse
{
    public string RespondingToType { get; set; }
    public bool Ok { get; set; }
    public int Id { get; set; }
    public int Work { get; set; } = 500;
}