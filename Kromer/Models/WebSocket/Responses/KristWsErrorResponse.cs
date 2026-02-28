using System.Text.Json.Serialization;

namespace Kromer.Models.WebSocket.Responses;

public class KristWsErrorResponse : IKristWsResponse
{
    public int Id { get; set; }
    public new bool Ok { get; set; } = false;
    public string Type => "error";

    public string RespondingToType { get; set; }
    public required string Error { get; set; }
    public required string Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Parameter { get; set; }
}