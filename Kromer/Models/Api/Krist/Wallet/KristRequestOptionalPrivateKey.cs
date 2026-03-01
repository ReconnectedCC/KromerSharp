using System.Text.Json.Serialization;

namespace Kromer.Models.Api.Krist.Wallet;

public class KristRequestOptionalPrivateKey
{
    [JsonPropertyName("privatekey")]
    public string? PrivateKey { get; set; }
}