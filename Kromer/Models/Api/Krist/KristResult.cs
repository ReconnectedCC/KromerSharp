using System.Text.Json.Serialization;

namespace Kromer.Models.Api.Krist;

public class KristResult
{
    public bool Ok { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}