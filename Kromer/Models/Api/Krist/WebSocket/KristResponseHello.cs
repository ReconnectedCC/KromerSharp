using Kromer.Models.Api.Krist.Misc;

namespace Kromer.Models.Api.Krist.WebSocket;

public class KristResponseHello : KristMotdResponse
{
    public string Type { get; set; } = "hello";
}