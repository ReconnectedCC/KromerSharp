using System.Text.Json.Serialization;

namespace Kromer.Models.WebSocket.Requests;

public class KristWsLoginRequest : KristWsRequest
{
    [JsonPropertyName("privatekey")]
    public string PrivateKey { get; set; }
}