using System.Text.Json.Serialization;

namespace Kromer.Models.Api.Internal;

public class AddressCreationResponse
{
    public required string Address { get; set; }

    // Dima you ask too much of sov
    [JsonPropertyName("privatekey")]
    public required string PrivateKey { get; set; }
}