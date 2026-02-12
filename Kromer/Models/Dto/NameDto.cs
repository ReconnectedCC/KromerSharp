using Kromer.Models.Entities;

namespace Kromer.Models.Dto;

public class NameDto
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public string? OriginalOwner { get; set; }
    public DateTime Registered { get; set; }
    public DateTime? Updated { get; set; }
    public DateTime? Transferred { get; set; }
    public string? A { get; set; }
    public decimal Unpaid { get; set; } = 0;

    public static NameDto FromEntity(NameEntity name)
    {
        return new NameDto
        {
            Name = name.Name,
            Owner = name.Owner,
            OriginalOwner = name.OriginalOwner,
            Registered = name.TimeRegistered,
            Updated = name.LastUpdated,
            Transferred = name.LastTransfered,
            A = name.Metadata,
            Unpaid = name.Unpaid,
        };
    }
}