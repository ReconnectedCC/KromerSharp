namespace Kromer.Models.WebSocket.Events;

public interface IKristEvent
{
    public string Type => "event";
    public string Event { get; }
}