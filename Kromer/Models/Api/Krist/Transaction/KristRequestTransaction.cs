using System.Text.Json.Serialization;

namespace Kromer.Models.Api.Krist.Transaction;

public class KristRequestTransaction
{
    [JsonPropertyName("privatekey")] 
    public required string PrivateKey { get; set; }
    
    public required string To { get; set; }
    
    public required decimal Amount { get; set; }
    
    [JsonPropertyName("metadata")]
    public string? MetaData { get; set; }
}