namespace Kromer.Models.Api.Internal;

public class PlayerRequest
{
    public Guid Uuid { get; set; }
    public required string Name { get; set; }
}