namespace Kromer.Models.WebSocket.Events;

public interface IKristEvent
{
    public string Type { get; }
    public string Event { get; }
}