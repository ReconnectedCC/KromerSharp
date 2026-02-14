using System.Text.Json.Serialization;
using Kromer.Models.Dto;

namespace Kromer.Models.Api.Krist.Address;

public class KristResultAddresses : KristResultList
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<AddressDto> Addresses { get; set; }
}