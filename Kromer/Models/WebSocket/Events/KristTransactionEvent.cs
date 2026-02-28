using Kromer.Models.Dto;

namespace Kromer.Models.WebSocket.Events;

public class KristTransactionEvent : IKristEvent
{
    public string Event => "transaction";
    public TransactionDto Transaction { get; set; }
}