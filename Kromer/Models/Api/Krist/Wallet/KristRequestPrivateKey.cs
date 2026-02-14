using System.Text.Json.Serialization;

namespace Kromer.Models.Api.Krist.Wallet;

public class KristRequestPrivateKey
{
    [JsonPropertyName("privatekey")] 
    public required string PrivateKey { get; set; }
}