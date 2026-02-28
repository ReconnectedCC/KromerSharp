using System.Text.Json.Serialization;

namespace Kromer.Models.WebSocket.Responses;

public interface IKristWsResponse
{
    public string Type => "response";
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RespondingToType { get; set; }
    
    public bool Ok { get; set; }
    
    public int Id { get; set; }
}