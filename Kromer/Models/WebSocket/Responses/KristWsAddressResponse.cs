using Kromer.Models.Api.Krist.Address;

namespace Kromer.Models.WebSocket.Responses;

public class KristWsAddressResponse : KristResultAddress, IKristWsResponse
{
    public string? RespondingToType { get; set; }
    public int Id { get; set; }
}