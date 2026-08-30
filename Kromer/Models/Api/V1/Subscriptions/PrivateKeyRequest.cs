using System.Text.Json.Serialization;

namespace Kromer.Models.Api.V1.Subscriptions;

public class PrivateKeyRequest
{
    [JsonPropertyName("privatekey")]
    public required string PrivateKey { get; set; }
}
