using Kromer.Models.Api.Krist.Transaction;

namespace Kromer.Models.WebSocket.Responses;

public class KristWsTransactionResponse : KristResultTransaction, IKristWsResponse
{
    public string? RespondingToType { get; set; }
    public int Id { get; set; }
}