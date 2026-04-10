namespace Kromer.Models.Entities;

public class PlayerEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public List<int>? OwnedWallets { get; set; }
}
