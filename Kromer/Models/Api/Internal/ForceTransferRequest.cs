using System.Text.Json.Serialization;

namespace Kromer.Models.Api.Internal;

public class ForceTransferRequest
{
    public required string From { get; set; }

    public required string To { get; set; }

    public required decimal Amount { get; set; }

    [JsonPropertyName("metadata")]
    public string? MetaData { get; set; }
}
