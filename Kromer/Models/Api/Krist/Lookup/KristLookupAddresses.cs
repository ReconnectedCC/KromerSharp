using System.Text.Json.Serialization;
using Kromer.Models.Dto;

namespace Kromer.Models.Api.Krist.Lookup;

public class KristLookupAddresses : KristResult
{
    public int Found { get; set; } = 0;

    [JsonPropertyName("notFound")]
    public int NotFound { get; set; } = 0;
    
    public Dictionary<string, AddressDto> Addresses { get; set; } = new();
}