using System.Collections.ObjectModel;

namespace Kromer.Models.Dto;

public class PlayerDto
{
    public Guid Uuid { get; set; }
    
    public string Username { get; set; }
    
    public IEnumerable<string> Wallets { get; set; } = [];
}