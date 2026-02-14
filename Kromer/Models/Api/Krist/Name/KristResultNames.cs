using System.Text.Json.Serialization;
using Kromer.Models.Dto;

namespace Kromer.Models.Api.Krist.Name;

public class KristResultNames : KristResultList
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<NameDto> Names { get; set; }
}