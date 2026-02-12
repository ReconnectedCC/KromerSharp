namespace Kromer.Models.Entities;

public partial class NameEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Owner { get; set; } = null!;

    public string OriginalOwner { get; set; } = null!;

    public DateTime TimeRegistered { get; set; }

    public DateTime? LastTransfered { get; set; }

    public DateTime? LastUpdated { get; set; }

    public decimal Unpaid { get; set; }

    public string? Metadata { get; set; }
}
