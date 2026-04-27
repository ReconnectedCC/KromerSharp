using System.Text.Json.Serialization;

namespace Kromer.Models.Api.V1.Subscriptions;

public class CreateSubscriptionRequest
{
    [JsonPropertyName("privatekey")]
    public required string PrivateKey { get; set; }

    public required string Name { get; set; }

    public decimal Price { get; set; }

    public int Period { get; set; }

    public required string Description { get; set; }
}
