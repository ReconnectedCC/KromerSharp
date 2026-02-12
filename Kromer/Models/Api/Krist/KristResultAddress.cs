using System.Text.Json.Serialization;
using Kromer.Models.Dto;

namespace Kromer.Models.Api.Krist;

public class KristResultAddress : KristResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AddressDto Address { get; set; }
}