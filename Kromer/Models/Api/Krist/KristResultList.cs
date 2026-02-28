using System.Text.Json.Serialization;

namespace Kromer.Models.Api.Krist;

public class KristResultList : KristResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Count { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Total { get; set; }
}