using Kromer.Models.Dto;

namespace Kromer.Models.WebSocket.Events;

public class KristNameEvent : IKristEvent
{
    public string Type => "event";
    public string Event => "name";
    public NameDto Name { get; set; }
}