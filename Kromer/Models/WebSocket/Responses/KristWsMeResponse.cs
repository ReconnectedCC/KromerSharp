using Kromer.Models.Dto;

namespace Kromer.Models.WebSocket.Responses;

public class KristWsMeResponse : IKristWsResponse
{
    public string? RespondingToType { get; set; }
    public bool Ok { get; set; }
    public int Id { get; set; }

    public AddressDto? Address { get; set; }

    public bool IsGuest { get; set; }
}