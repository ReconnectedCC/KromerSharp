using System.Text.Json.Serialization;

namespace Kromer.Models.WebSocket.Requests;

public class KristWsTransactionRequest : KristWsRequest
{
    [JsonPropertyName("privatekey")]
    public string? PrivateKey { get; set; }

    public string To { get; set; }

    public decimal Amount { get; set; }

    [JsonPropertyName("metadata")]
    public string? MetaData { get; set; }
}